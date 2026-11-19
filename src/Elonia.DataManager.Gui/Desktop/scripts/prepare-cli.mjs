import { execFileSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readFileSync, renameSync, unlinkSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const desktop = fileURLToPath(new URL('..', import.meta.url));
const project = resolve(desktop, '../../Elonia.DataManager.Cli');
const projectName = 'Elonia.DataManager.Cli';
const host = process.env.TAURI_ENV_TARGET_TRIPLE || execFileSync('rustc', ['-vV'], { encoding: 'utf8' }).match(/^host: (.+)$/m)?.[1].trim();
const platforms = {
  'aarch64-apple-darwin': 'osx-arm64',
  'x86_64-apple-darwin': 'osx-x64',
  'x86_64-pc-windows-msvc': 'win-x64',
  'aarch64-pc-windows-msvc': 'win-arm64',
  'x86_64-unknown-linux-gnu': 'linux-x64',
  'aarch64-unknown-linux-gnu': 'linux-arm64'
};
const rid = platforms[host];
if (!rid) throw new Error('지원하지 않는 빌드 대상입니다. ' + host);

const out = join(project, 'bin', 'desktop', rid);
const suffix = rid.startsWith('win-') ? '.exe' : '';
const binary = join(out, 'elonia-data' + suffix);
const destination = join(desktop, 'src-tauri', 'binaries', 'elonia-data-' + host + suffix);
execFileSync('dotnet', [
  'publish', join(project, projectName + '.csproj'), '-c', 'Release', '-r', rid,
  '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
  '-o', out, '--nologo'
], { stdio: 'inherit' });

mkdirSync(dirname(destination), { recursive: true });
// 내용이 같으면 파일 시각을 보존해 Cargo가 서비스를 다시 복사하는 일을 줄인다.
if (!existsSync(destination) || !readFileSync(destination).equals(readFileSync(binary))) {
  const temporary = destination + '.' + process.pid;
  try {
    copyFileSync(binary, temporary);
    renameSync(temporary, destination);
  } finally {
    if (existsSync(temporary)) unlinkSync(temporary);
  }
}
