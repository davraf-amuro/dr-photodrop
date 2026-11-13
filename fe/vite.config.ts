import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'

/** Origine dell'API in sviluppo (container dr-photodrop-api con porta pubblicata). */
const apiTarget = 'http://localhost:5070'

/**
 * Colore della barra del browser e dello splash. Il manifest non puo' usare variabili CSS: valore letterale
 * qui, uguale al `<meta name="theme-color">` di index.html e al fondo delle icone (scripts/generate-icons.mjs).
 */
const themeColor = '#1f2937'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    VitePWA({
      // Service worker scritto a mano (src/sw/sw.ts): precache dell'app shell + Background Sync della coda
      strategies: 'injectManifest',
      srcDir: 'src/sw',
      filename: 'sw.ts',
      registerType: 'autoUpdate',
      injectManifest: {
        globPatterns: ['**/*.{js,css,html,png,svg,webmanifest}'],
      },
      manifest: {
        id: '/',
        name: 'dr-photodrop',
        short_name: 'photodrop',
        description: 'Scatta foto dal telefono e parcheggiale sul PC',
        lang: 'it',
        display: 'standalone',
        orientation: 'portrait',
        start_url: '/',
        scope: '/',
        theme_color: themeColor,
        background_color: themeColor,
        icons: [
          { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
          { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
          { src: 'icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
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
