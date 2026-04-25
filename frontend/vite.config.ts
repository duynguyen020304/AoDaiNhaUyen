import { defineConfig, type Connect } from 'vite';
import react from '@vitejs/plugin-react';

function serviceWorkerNoStoreHeaders(): Connect.NextHandleFunction {
  return (req, res, next) => {
    if (req.url?.split('?')[0] === '/sw.js') {
      res.setHeader('Cache-Control', 'no-store');
    }

    next();
  };
}

export default defineConfig({
  plugins: [
    react(),
    {
      name: 'service-worker-no-store',
      configureServer(server) {
        server.middlewares.use(serviceWorkerNoStoreHeaders());
      },
      configurePreviewServer(server) {
        server.middlewares.use(serviceWorkerNoStoreHeaders());
      },
    },
  ],
  envPrefix: ['VITE_', 'PUBLIC_'],
  css: {
    postcss: './postcss.config.js',
  },
  build: {
    assetsInlineLimit: 0,
  },
});
