import { defineConfig } from 'vite';

export default defineConfig({
  clearScreen: false,
  build: { target: ['safari16.4', 'chrome110'] },
  server: { watch: { ignored: ['**/src-tauri/**'] } }
});
