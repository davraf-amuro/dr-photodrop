/// <reference lib="webworker" />
import { clientsClaim } from 'workbox-core'
import { cleanupOutdatedCaches, createHandlerBoundToURL, precacheAndRoute } from 'workbox-precaching'
import { NavigationRoute, registerRoute } from 'workbox-routing'

import { SyncConfig } from '@/config/appConfig'
import { flushQueue } from '@/services/syncService'

/*
 * Service worker: precache dell'app shell e Background Sync della coda.
 * NESSUNA cache runtime: le risposte di /api/ non si mettono mai in cache (token, foto, ambiti).
 */

/** `__WB_MANIFEST` e' sostituito da vite-plugin-pwa (injectManifest) con l'elenco dei file da precache. */
declare const self: ServiceWorkerGlobalScope & {
  __WB_MANIFEST: Array<string | { url: string; revision?: string | null }>
}

/** Evento Background Sync (non presente nei tipi standard di TypeScript). */
interface SyncEvent extends ExtendableEvent {
  readonly tag: string
}

/** Percorsi che la navigazione offline NON deve riscrivere verso index.html. */
const NAVIGATION_DENYLIST = [/^\/api\//]

// autoUpdate: il nuovo service worker prende subito il controllo
void self.skipWaiting()
clientsClaim()

cleanupOutdatedCaches()
precacheAndRoute(self.__WB_MANIFEST)
registerRoute(new NavigationRoute(createHandlerBoundToURL('/index.html'), { denylist: NAVIGATION_DENYLIST }))

self.addEventListener('sync', (event) => {
  const syncEvent = event as SyncEvent
  if (syncEvent.tag !== SyncConfig.backgroundSyncTag) return
  syncEvent.waitUntil(
    flushQueue({ includeManual: false, ignoreBackoff: true }).then((summary) => {
      // Se resta qualcosa da ritentare, l'errore fa riprogrammare l'evento dal browser
      if (summary.nextAttemptAt !== null) throw new Error('Invio incompleto: nuovo tentativo')
    }),
  )
})
