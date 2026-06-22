import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  base: '/dashboard/',
  plugins: [react()],
  build: {
    outDir: '../Zte.Backend.Api/wwwroot/dashboard',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5145',
    },
  },
});
