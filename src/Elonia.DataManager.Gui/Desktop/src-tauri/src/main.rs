#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod options;

use base64::{engine::general_purpose::STANDARD, Engine};
use options::{is_workbook, LaunchOptions, Options};
use serde::Deserialize;
use serde_json::Value;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use tauri::{AppHandle, Manager, RunEvent, State};

struct Workspace {
    options: Options,
    operation: Mutex<()>,
    closing: AtomicBool,
}

type DesktopState = Arc<Workspace>;

#[tauri::command]
fn launch_options(state: State<'_, DesktopState>) -> LaunchOptions {
    state.options.launch.clone()
}

#[derive(Clone, Copy, Deserialize)]
#[serde(rename_all = "lowercase")]
enum Operation {
    List,
    Check,
    Build,
}

impl Operation {
    fn name(self) -> &'static str {
        match self {
            Self::List => "list",
            Self::Check => "check",
            Self::Build => "build",
        }
    }
}

fn parse_result(
    operation: Operation,
    code: Option<i32>,
    stdout: &[u8],
    stderr: &[u8],
) -> Result<Value, String> {
    let status = code
        .map(|value| value.to_string())
        .unwrap_or_else(|| "중단".into());
    let failure = |message: &str| {
        format!(
            "{} 명령이 실패했습니다({status}). {message}",
            operation.name()
        )
    };
    let detail = String::from_utf8_lossy(stderr);
    if code == Some(2) && detail.trim().is_empty() {
        let help = String::from_utf8_lossy(stdout);
        return Err(failure(
            help.lines()
                .next()
                .unwrap_or("CLI 실행 인자를 확인해 주세요."),
        ));
    }
    if !matches!(code, Some(0 | 1)) {
        return Err(failure(detail.trim()));
    }
    let value: Value = serde_json::from_slice(stdout).map_err(|error| {
        if detail.trim().is_empty() {
            failure(&format!("JSON 결과를 읽을 수 없습니다. {error}"))
        } else {
            failure(detail.trim())
        }
    })?;
    if !value["spreadsheets"].is_array() || !value["diagnostics"].is_array() {
        return Err(failure(
            "CLI 결과에서 스프레드시트 목록이나 진단 목록을 읽을 수 없습니다.",
        ));
    }
    if code == Some(1)
        && !value["diagnostics"]
            .as_array()
            .is_some_and(|rows| rows.iter().any(|row| row["severity"] == "error"))
    {
        return Err(failure(if detail.trim().is_empty() {
            "오류 진단을 받지 못했습니다."
        } else {
            detail.trim()
        }));
    }
    Ok(value)
}

fn run_service(options: &Options, operation: Operation) -> Result<Value, String> {
    let exe = std::env::current_exe().map_err(|error| error.to_string())?;
    let name = if cfg!(windows) {
        "elonia-data.exe"
    } else {
        "elonia-data"
    };
    let mut command = Command::new(exe.with_file_name(name));
    command
        .current_dir(&options.root)
        .args([operation.name(), "--format", "json"])
        .args(&options.service_args);
    #[cfg(windows)]
    {
        use std::os::windows::process::CommandExt;
        command.creation_flags(0x08000000);
    }
    let output = command
        .output()
        .map_err(|error| format!("데이터 CLI를 실행할 수 없습니다. {error}"))?;
    parse_result(
        operation,
        output.status.code(),
        &output.stdout,
        &output.stderr,
    )
}

#[tauri::command]
async fn workspace(state: State<'_, DesktopState>, operation: Operation) -> Result<Value, String> {
    let state = Arc::clone(&state);
    tauri::async_runtime::spawn_blocking(move || {
        let _guard = state
            .operation
            .lock()
            .map_err(|_| "데이터 작업을 다시 시작해 주세요.")?;
        if state.closing.load(Ordering::Acquire) {
            return Err("데이터매니저를 종료하고 있습니다.".into());
        }
        run_service(&state.options, operation)
    })
    .await
    .map_err(|error| error.to_string())?
}

#[derive(Deserialize)]
#[serde(rename_all = "lowercase")]
enum Folder {
    Input,
    Code,
    Data,
}

#[tauri::command]
async fn open_folder(state: State<'_, DesktopState>, kind: Folder) -> Result<(), String> {
    let path = match kind {
        Folder::Input => &state.options.input,
        Folder::Code => &state.options.code,
        Folder::Data => &state.options.data,
    }
    .clone();
    tauri::async_runtime::spawn_blocking(move || {
        if !path.is_dir() {
            return Err(format!("폴더를 찾을 수 없습니다. {}", path.display()));
        }
        open::that(path).map_err(|error| format!("폴더를 열 수 없습니다. {error}"))
    })
    .await
    .map_err(|error| error.to_string())?
}

fn workbook_path(input: &Path, name: &str) -> Result<PathBuf, String> {
    if !is_workbook(name.as_ref()) {
        return Err("스프레드시트 이름을 확인해 주세요.".into());
    }
    let directory = input.canonicalize().map_err(|error| error.to_string())?;
    let path = directory
        .join(name)
        .canonicalize()
        .map_err(|error| error.to_string())?;
    if path.parent() != Some(directory.as_path())
        || !path.is_file()
        || !path.file_name().is_some_and(is_workbook)
    {
        return Err("선택한 엑셀 파일을 입력 폴더에서 찾을 수 없습니다.".into());
    }
    Ok(path)
}

#[tauri::command]
async fn open_workbook(state: State<'_, DesktopState>, name: String) -> Result<(), String> {
    let directory = state.options.input.clone();
    tauri::async_runtime::spawn_blocking(move || {
        let path = workbook_path(&directory, &name)?;
        open::that(path).map_err(|error| format!("원본을 열 수 없습니다. {error}"))
    })
    .await
    .map_err(|error| error.to_string())?
}

fn write_png(path: &Path, image: &str) -> Result<(), String> {
    let png = image
        .strip_prefix("data:image/png;base64,")
        .ok_or("PNG 형식의 캡처가 필요합니다.")?;
    let bytes = STANDARD.decode(png).map_err(|error| error.to_string())?;
    if !bytes.starts_with(b"\x89PNG\r\n\x1a\n") {
        return Err("PNG의 머리글이 올바르지 않습니다.".into());
    }
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent).map_err(|error| error.to_string())?;
    }
    fs::write(path, bytes).map_err(|error| error.to_string())
}

#[tauri::command]
fn save_capture(app: AppHandle, state: State<'_, DesktopState>, png: String) -> Result<(), String> {
    let path = state
        .options
        .capture_path
        .as_deref()
        .ok_or("캡처 경로를 지정해 실행해 주세요.")?;
    match write_png(path, &png) {
        Ok(()) => app.exit(0),
        Err(reason) => {
            eprintln!("캡처 저장 실패: {reason}");
            app.exit(1);
            return Err(reason);
        }
    }
    Ok(())
}

#[tauri::command]
fn capture_failed(app: AppHandle, state: State<'_, DesktopState>, message: String) {
    if state.options.launch.screenshot {
        eprintln!("캡처 실패: {message}");
        app.exit(1);
    }
}

fn main() {
    let options = Options::from_command_line().unwrap_or_else(|error| {
        eprintln!("{error}");
        std::process::exit(2)
    });
    tauri::Builder::default()
        .manage(Arc::new(Workspace {
            options,
            operation: Mutex::new(()),
            closing: AtomicBool::new(false),
        }))
        .invoke_handler(tauri::generate_handler![
            launch_options,
            workspace,
            open_folder,
            open_workbook,
            save_capture,
            capture_failed
        ])
        .build(tauri::generate_context!())
        .expect("데이터매니저를 실행할 수 없습니다.")
        .run(|app, event| {
            if matches!(event, RunEvent::Exit) {
                // 생성 중 창을 닫아도 CLI가 파일 반영을 마칠 때까지 호스트를 살려 둔다.
                let state = app.state::<DesktopState>();
                state.closing.store(true, Ordering::Release);
                drop(state.operation.lock());
            }
        });
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn handles_validation_exit_without_masking_cli_failures() {
        let error =
            br#"{"spreadsheets":[],"diagnostics":[{"severity":"error","reason":"bad enum"}]}"#;
        assert_eq!(
            parse_result(Operation::Check, Some(1), error, b"").unwrap()["diagnostics"][0]
                ["reason"],
            "bad enum"
        );
        assert!(parse_result(Operation::Build, Some(2), error, b"bad option").is_err());
        let denied = parse_result(Operation::Build, Some(1), b"", b"access denied").unwrap_err();
        assert!(denied.contains("access denied"));
        assert!(parse_result(Operation::List, Some(0), b"not json", b"").is_err());
        assert!(parse_result(
            Operation::Build,
            Some(1),
            br#"{"spreadsheets":[],"diagnostics":[]}"#,
            b""
        )
        .is_err());
        assert!(parse_result(Operation::List, Some(0), br#"{}"#, b"").is_err());
    }

    #[test]
    fn accepts_only_documented_js_commands_and_folder_identifiers() {
        assert!(serde_json::from_str::<Operation>(r#""--help""#).is_err());
        assert!(serde_json::from_str::<Folder>(r#""../other""#).is_err());
        assert!(!is_workbook("../outside.xlsx".as_ref()));
        assert!(!is_workbook("~$locked.xlsx".as_ref()));
        assert!(is_workbook("장비 표.xlsx".as_ref()));
    }
}
