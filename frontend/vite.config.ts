import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  envPrefix: ['VITE_', 'PUBLIC_'],
  css: {
    postcss: './postcss.config.js',
  },
  build: {
    assetsInlineLimit: 0,
  },
});
