import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    host: '127.0.0.1',
    hmr: {
      host: '127.0.0.1',
    },
  },
  base: './',  // Relative paths for Electron file:// protocol
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
});
