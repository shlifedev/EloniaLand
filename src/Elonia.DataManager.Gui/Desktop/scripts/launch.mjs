import { spawn, spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { delimiter, dirname, join, resolve } from 'node:path';
import { homedir } from 'node:os';
import { fileURLToPath } from 'node:url';

const desktop = fileURLToPath(new URL('..', import.meta.url));
const cli = join(desktop, 'node_modules', '@tauri-apps', 'cli', 'tauri.js');
const stamp = join(desktop, 'node_modules', '.elonia-lock');
const windows = process.platform === 'win32';
process.chdir(desktop);
process.env.ELONIA_REPO_ROOT = resolve(desktop, '../../..');
// Finder에서 연 Rider도 Node, rustup 및 Homebrew의 실행 경로를 찾도록 한다.
process.env.PATH = [
  dirname(process.execPath), process.env.PATH, join(homedir(), '.cargo', 'bin'),
  ...(process.platform === 'darwin' ? ['/opt/homebrew/bin', '/usr/local/bin'] : [])
].filter(Boolean).join(delimiter);

const hash = createHash('sha256')
  .update(readFileSync('package.json')).update(readFileSync('package-lock.json')).digest('hex');
if (!existsSync(cli) || !existsSync(stamp) || readFileSync(stamp, 'utf8') !== hash) {
  const result = windows
    ? spawnSync('cmd.exe', ['/d', '/c', 'npm ci --include=dev --no-audit --no-fund'], { stdio: 'inherit' })
    : spawnSync('npm', ['ci', '--include=dev', '--no-audit', '--no-fund'], { stdio: 'inherit' });
  if (result.error) console.error(result.error.message);
  if (result.status !== 0) process.exit(result.status ?? 1);
  writeFileSync(stamp, hash);
}

const args = process.argv.slice(2);
const tauri = spawn(process.execPath, [cli, 'dev', ...(args.length ? ['--', '--', ...args] : [])], { stdio: 'inherit' });
for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => tauri.kill(signal));
}
tauri.on('error', error => { console.error(error.message); process.exitCode = 1; });
tauri.on('exit', (code, signal) => { process.exitCode = code ?? (signal === 'SIGINT' ? 130 : 1); });
