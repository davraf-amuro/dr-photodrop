import { PhotoStatus, QueueConfig, SyncConfig } from '@/config/appConfig'
import { uploadPhoto } from '@/services/apiClient'
import {
  getBatch,
  getPhotoBlob,
  listPhotosByStatus,
  markPhotoSent,
  updatePhoto,
} from '@/services/queueService'
import { extensionFor } from '@/utils/fileTypes'
import type { FlushSummary, QueuedPhoto } from '@/types/queue'

/*
 * Invio della coda. Modulo sicuro anche nel service worker (Background Sync).
 *
 * Concorrenza: l'invio e' SEQUENZIALE per foto, dichiarato di proposito. Le foto sono indipendenti, ma il
 * collo di bottiglia e' la rete mobile (piu' richieste in parallelo rallenterebbero ciascuna e farebbero
 * scadere i timeout) e l'ordine di scatto del lotto e' utile a chi legge l'inbox. Una sola passata alla
 * volta in tutto il browser (pagine e service worker) grazie a Web Locks.
 */

/** Ritardo del tentativo n (1-based): base * factor^(n-1), con tetto. */
export function backoffDelayMs(attempt: number): number {
  const delay = QueueConfig.backoffBaseMs * QueueConfig.backoffFactor ** (attempt - 1)
  return Math.min(delay, QueueConfig.backoffMaxMs)
}

export interface FlushOptions {
  /** true = invia anche i lotti "Metti in coda" (azione esplicita dell'utente). */
  includeManual: boolean
  /** true = ignora l'attesa del backoff (rete appena tornata, app appena aperta, sync del browser). */
  ignoreBackoff: boolean
}

async function nextEligiblePhoto(now: number, options: FlushOptions): Promise<QueuedPhoto | null> {
  const queued = await listPhotosByStatus(PhotoStatus.queued)
  for (const photo of queued) {
    if (!options.ignoreBackoff && photo.nextAttemptAt !== null && photo.nextAttemptAt > now) continue
    if (!options.includeManual) {
      const batch = await getBatch(photo.batchId)
      if (!batch || batch.manual) continue
    }
    return photo
  }
  return null
}

async function earliestScheduledAttempt(): Promise<number | null> {
  const queued = await listPhotosByStatus(PhotoStatus.queued)
  const times = queued.map((photo) => photo.nextAttemptAt).filter((value): value is number => value !== null)
  return times.length > 0 ? Math.min(...times) : null
}

/** Foto rimaste "sending" per chiusura della scheda a meta' invio: tornano in coda. */
async function recoverInterrupted(): Promise<void> {
  const stuck = await listPhotosByStatus(PhotoStatus.sending)
  for (const photo of stuck) await updatePhoto(photo.photoId, { status: PhotoStatus.queued })
}

async function runPass(options: FlushOptions): Promise<FlushSummary> {
  await recoverInterrupted()
  const summary: FlushSummary = { sent: 0, failed: 0, authRequired: false, nextAttemptAt: null, skipped: false }

  for (;;) {
    const photo = await nextEligiblePhoto(Date.now(), options)
    if (!photo) break

    const [batch, blob] = await Promise.all([getBatch(photo.batchId), getPhotoBlob(photo.photoId)])
    if (!batch || !blob) {
      await updatePhoto(photo.photoId, { status: PhotoStatus.error, lastError: 'Dati della foto non trovati in coda' })
      summary.failed += 1
      continue
    }

    await updatePhoto(photo.photoId, { status: PhotoStatus.sending })
    const outcome = await uploadPhoto({
      photoId: photo.photoId,
      batchId: photo.batchId,
      scopeId: batch.scopeId,
      capturedAt: photo.capturedAt,
      fields: batch.fields,
      blob,
      fileName: `${photo.photoId}.${extensionFor(photo.contentType)}`,
    })

    if (outcome.kind === 'ok') {
      await markPhotoSent(photo.photoId)
      summary.sent += 1
      continue
    }

    if (outcome.kind === 'rejected') {
      // Errore definitivo (400/413): da mostrare, non ritentare
      await updatePhoto(photo.photoId, { status: PhotoStatus.error, lastError: outcome.message })
      summary.failed += 1
      continue
    }

    if (outcome.kind === 'unauthorized') {
      await updatePhoto(photo.photoId, {
        status: PhotoStatus.queued,
        lastError: 'Token mancante o non valido: controllalo nelle Impostazioni',
      })
      summary.authRequired = true
      break
    }

    // Rete o 5xx: backoff esponenziale; si ferma la passata (le altre foto soffrirebbero dello stesso problema)
    const attempts = photo.attempts + 1
    if (attempts >= QueueConfig.maxAttempts) {
      await updatePhoto(photo.photoId, {
        status: PhotoStatus.error,
        attempts,
        nextAttemptAt: null,
        lastError: `${outcome.message}: tentativi esauriti, riprova a mano`,
      })
      summary.failed += 1
      continue
    }
    await updatePhoto(photo.photoId, {
      status: PhotoStatus.queued,
      attempts,
      nextAttemptAt: Date.now() + backoffDelayMs(attempts),
      lastError: outcome.message,
    })
    break
  }

  summary.nextAttemptAt = await earliestScheduledAttempt()
  return summary
}

/** Una passata di invio. Se un'altra e' gia' in corso (altra scheda o service worker) ritorna `skipped`. */
export async function flushQueue(options: FlushOptions = { includeManual: false, ignoreBackoff: false }): Promise<FlushSummary> {
  if (typeof navigator !== 'undefined' && 'locks' in navigator) {
    const result = await navigator.locks.request(SyncConfig.lockName, { ifAvailable: true }, async (lock) => {
      if (!lock) return null
      return runPass(options)
    })
    return result ?? { sent: 0, failed: 0, authRequired: false, nextAttemptAt: null, skipped: true }
  }
  return runPass(options)
}
