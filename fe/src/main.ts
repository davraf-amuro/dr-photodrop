import { registerSW } from 'virtual:pwa-register'
import { createApp } from 'vue'

import App from './App.vue'
import './assets/styles/main.css'
import { useTheme } from '@/composables/shared/useTheme'
import { requestPersistence } from '@/services/storageService'
import { startSyncScheduler } from '@/services/syncScheduler'

// Aggiornamento automatico del service worker (registerType: autoUpdate)
registerSW({ immediate: true })

useTheme().applyStoredTheme()
createApp(App).mount('#app')

// Chiede al browser di non cancellare la coda sotto pressione di spazio, poi avvia l'invio
void requestPersistence()
startSyncScheduler()
