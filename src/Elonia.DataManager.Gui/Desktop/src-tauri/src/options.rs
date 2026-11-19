use serde::Serialize;
use std::env;
use std::ffi::{OsStr, OsString};
use std::path::{Path, PathBuf};

#[derive(Clone, Default, Serialize)]
pub struct LaunchOptions {
    #[serde(skip_serializing_if = "Option::is_none")]
    pub sheet: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub problem: Option<usize>,
    pub screenshot: bool,
}

pub struct Options {
    pub root: PathBuf,
    pub input: PathBuf,
    pub code: PathBuf,
    pub data: PathBuf,
    pub service_args: Vec<OsString>,
    pub launch: LaunchOptions,
    pub capture_path: Option<PathBuf>,
}

impl Options {
    pub fn from_command_line() -> Result<Self, String> {
        let cwd = env::current_dir().map_err(|error| error.to_string())?;
        let root = match env::var_os("ELONIA_REPO_ROOT") {
            Some(path) if !path.is_empty() => cwd.join(path),
            // Finder에서 저장소 안의 앱을 열면 현재 폴더가 저장소 밖일 수 있다.
            _ => find_repository(&cwd)
                .or_else(|| {
                    env::current_exe()
                        .ok()
                        .and_then(|exe| find_repository(&exe))
                })
                .unwrap_or(cwd),
        };
        Self::parse(root, env::args_os().skip(1))
    }

    fn parse(root: PathBuf, mut args: impl Iterator<Item = OsString>) -> Result<Self, String> {
        let mut options = Self {
            input: root.join("src/Elonia.Data/Sheets"),
            code: root.join("src/Elonia.Data/Generated"),
            data: root.join("src/Elonia.Data/Generated/Bytes"),
            root,
            service_args: Vec::new(),
            launch: LaunchOptions::default(),
            capture_path: None,
        };
        while let Some(key) = args.next() {
            let value = args
                .next()
                .ok_or_else(|| format!("{} 뒤에 값이 필요합니다.", key.to_string_lossy()))?;
            match key.to_str() {
                Some("--input" | "--code" | "--data") => {
                    if value.is_empty() {
                        return Err(format!("{} 경로가 비었습니다.", key.to_string_lossy()));
                    }
                    let path = options.root.join(value);
                    match key.to_str() {
                        Some("--input") => options.input = path,
                        Some("--code") => options.code = path,
                        _ => options.data = path,
                    }
                }
                Some("--namespace" | "--class") => options.service_args.extend([key, value]),
                Some("--sheet") => {
                    options.launch.sheet = Some(
                        value
                            .into_string()
                            .map_err(|_| "시트 이름을 읽을 수 없습니다.")?,
                    );
                }
                Some("--problem") => {
                    let index = value
                        .to_str()
                        .and_then(|s| s.parse::<usize>().ok())
                        .filter(|index| *index > 0)
                        .ok_or("문제 번호는 1부터 시작합니다.")?;
                    options.launch.problem = Some(index);
                }
                Some("--screenshot") if !value.is_empty() => {
                    options.capture_path = Some(options.root.join(value));
                    options.launch.screenshot = true;
                }
                _ => return Err(format!("잘못된 실행 인자입니다. {}", key.to_string_lossy())),
            }
        }
        for (flag, path) in [
            ("--input", &options.input),
            ("--code", &options.code),
            ("--data", &options.data),
        ] {
            options
                .service_args
                .extend([OsString::from(flag), path.as_os_str().to_owned()]);
        }
        Ok(options)
    }
}

fn find_repository(start: &Path) -> Option<PathBuf> {
    start
        .ancestors()
        .find(|dir| dir.join("Elonia.sln").is_file())
        .map(Path::to_path_buf)
}

pub fn is_workbook(name: &OsStr) -> bool {
    let path = Path::new(name);
    path.file_name() == Some(name)
        && !name.to_string_lossy().starts_with("~$")
        && path
            .extension()
            .is_some_and(|ext| ext.eq_ignore_ascii_case("xlsx"))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resolves_workspace_paths_and_keeps_launch_flags_out_of_service_args() {
        let root = env::temp_dir();
        let args = [
            "--input",
            "표",
            "--namespace",
            "Demo",
            "--sheet",
            "Actor",
            "--problem",
            "2",
            "--screenshot",
            "result.png",
        ];
        let options = Options::parse(root.clone(), args.into_iter().map(OsString::from)).unwrap();
        assert_eq!(options.input, root.join("표"));
        assert_eq!(options.capture_path, Some(root.join("result.png")));
        assert_eq!(options.launch.problem, Some(2));
        assert_eq!(options.launch.sheet.as_deref(), Some("Actor"));
        assert_eq!(options.service_args.len(), 8);
        assert_eq!(options.code, root.join("src/Elonia.Data/Generated"));
    }

    #[test]
    fn rejects_invalid_problem_indexes_and_incomplete_options() {
        for args in [
            vec!["--problem", "0"],
            vec!["--problem", "-1"],
            vec!["--input"],
        ] {
            assert!(Options::parse(env::temp_dir(), args.into_iter().map(OsString::from)).is_err());
        }
    }
}
