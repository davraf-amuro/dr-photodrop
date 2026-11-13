import { ref } from 'vue'

import { SyncConfig } from '@/config/appConfig'
import { purgeOldSent } from '@/services/queueService'
import { flushQueue } from '@/services/syncService'
import type { FlushSummary } from '@/types/queue'

/*
 * Pianificazione dell'invio lato pagina: all'apertura, al ritorno online, quando la scheda torna visibile,
 * alla scadenza del backoff. Background Sync (se disponibile) e' un di piu': il service worker rifa' la
 * stessa passata anche a pagina chiusa.
 */

/** true se l'ultima passata e' stata respinta con 401 o manca il token: la UI chiede di inserirlo. */
export const authRequired = ref(false)

let timer: ReturnType<typeof setTimeout> | null = null
let started = false

function scheduleRetry(at: number | null): void {
  if (timer) clearTimeout(timer)
  timer = null
  if (at === null) return
  timer = setTimeout(() => void runFlush(false, false), Math.max(at - Date.now(), 0))
}

/** Esegue una passata e riprogramma la successiva in base al backoff. */
export async function runFlush(includeManual: boolean, ignoreBackoff: boolean): Promise<FlushSummary | null> {
  if (!navigator.onLine) return null
  const summary = await flushQueue({ includeManual, ignoreBackoff })
  if (!summary.skipped) {
    scheduleRetry(summary.nextAttemptAt)
    authRequired.value = summary.authRequired
  }
  return summary
}

/** Richiede una sincronizzazione in background, se il browser la supporta (feature detection). */
export async function requestBackgroundSync(): Promise<void> {
  if (!('serviceWorker' in navigator)) return
  try {
    const registration = await navigator.serviceWorker.ready
    if ('sync' in registration) {
      await (registration as ServiceWorkerRegistration & { sync: { register(tag: string): Promise<void> } }).sync.register(
        SyncConfig.backgroundSyncTag,
      )
    }
  } catch {
    // Background Sync non disponibile o negato: resta l'invio da pagina
  }
}

/** Avvia i trigger di invio. Idempotente. */
export function startSyncScheduler(): void {
  if (started) return
  started = true
  void purgeOldSent().then(() => runFlush(false, true))
  window.addEventListener('online', () => void runFlush(false, true))
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') void runFlush(false, true)
  })
}
