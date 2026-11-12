import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'

/** Origine dell'API in sviluppo (container dr-photodrop-api con porta pubblicata). */
const apiTarget = 'http://localhost:5070'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    // Scheletro PWA: manifest e service worker di base. Coda offline e icone vere: piano photo-inbox, fase 3.
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'dr-photodrop',
        short_name: 'photodrop',
        description: 'Scatta foto dal telefono e parcheggiale sul PC',
        lang: 'it',
        display: 'standalone',
        start_url: '/',
        theme_color: '#1f2937',
        background_color: '#1f2937',
        icons: [],
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
})
