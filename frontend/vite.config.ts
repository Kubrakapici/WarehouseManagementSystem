import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import path from 'path';

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_PROXY ?? 'https://localhost:7288',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: process.env.VITE_API_PROXY ?? 'https://localhost:7288',
        ws: true,
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
