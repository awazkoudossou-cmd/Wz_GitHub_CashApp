import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import path from 'node:path';

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      // Pas de cache runtime pour /api/** : on ne veut jamais servir des soldes/opérations
      // périmés hors-ligne. Seule la coquille de l'app (JS/CSS/HTML/icônes) est précachée.
      manifest: {
        name: 'CashApp',
        short_name: 'CashApp',
        description: 'Gestion de caisse',
        theme_color: '#1f4e8f',
        background_color: '#f5f7fa',
        display: 'standalone',
        start_url: '/',
        scope: '/',
        icons: [
          { src: 'pwa-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512.png', sizes: '512x512', type: 'image/png' },
          { src: 'maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' }
        ]
      }
    })
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
        secure: false
      }
    }
  }
});
