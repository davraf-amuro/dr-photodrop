import { DbConfig, PhotoStatus, QueueConfig, SyncConfig } from '@/config/appConfig'
import { getDb } from '@/services/db'
import type { FieldValues } from '@/types/contract'
import type { DraftPhoto, PhotoBlobRecord, QueuedBatch, QueuedPhoto } from '@/types/queue'

/*
 * Coda offline su IndexedDB. Modulo sicuro anche nel service worker (Background Sync):
 * niente window/document. I cambiamenti sono notificati con BroadcastChannel a tutte le schede.
 */

const MS_PER_DAY = 86_400_000

/** Coda piena: il lotto non e' stato accodato. */
export class QueueFullError extends Error {
  constructor(limit: number) {
    super(`Coda piena (massimo ${limit} foto): invia o elimina qualcosa prima di scattare ancora`)
    this.name = 'QueueFullError'
  }
}

/** Spazio del browser esaurito durante il salvataggio. */
export class StorageQuotaError extends Error {
  constructor() {
    super('Spazio del telefono esaurito: libera spazio o elimina foto dalla coda')
    this.name = 'StorageQuotaError'
  }
}

export function notifyQueueChanged(): void {
  if (typeof BroadcastChannel === 'undefined') return
  const channel = new BroadcastChannel(SyncConfig.channelName)
  channel.postMessage('changed')
  channel.close()
}

export interface NewBatchInput {
  scopeId: string
  scopeName: string
  fields: FieldValues
  photos: DraftPhoto[]
  /** true = "Metti in coda" (invio solo manuale dalla schermata Coda). */
  manual: boolean
}

/** Conta le foto ancora da gestire (non inviate). */
export async function countPendingPhotos(): Promise<number> {
  const db = await getDb()
  const all = await db.getAll('photos')
  return all.filter((photo) => photo.status !== PhotoStatus.sent).length
}

/** Salva lotto e foto in un'unica transazione. Lancia QueueFullError o StorageQuotaError. */
export async function enqueueBatch(input: NewBatchInput): Promise<QueuedBatch> {
  const pending = await countPendingPhotos()
  if (pending + input.photos.length > QueueConfig.maxPhotosInQueue) {
    throw new QueueFullError(QueueConfig.maxPhotosInQueue)
  }

  const now = Date.now()
  const batch: QueuedBatch = {
    id: crypto.randomUUID(),
    scopeId: input.scopeId,
    scopeName: input.scopeName,
    fields: input.fields,
    createdAt: now,
    manual: input.manual,
  }

  const db = await getDb()
  const tx = db.transaction(
    [DbConfig.stores.batches, DbConfig.stores.photos, DbConfig.stores.blobs],
    'readwrite',
  )
  try {
    await tx.objectStore('batches').put(batch)
    for (const draft of input.photos) {
      const photo: QueuedPhoto = {
        photoId: draft.photoId,
        batchId: batch.id,
        capturedAt: draft.capturedAt,
        createdAt: now,
        sizeBytes: draft.blob.size,
        contentType: draft.blob.type,
        thumb: draft.thumb.size > 0 ? draft.thumb : null,
        status: PhotoStatus.queued,
        attempts: 0,
        nextAttemptAt: null,
        lastError: null,
        sentAt: null,
      }
      await tx.objectStore('photos').put(photo)
      await tx.objectStore('blobs').put({ photoId: draft.photoId, blob: draft.blob })
    }
    await tx.done
  } catch (error) {
    try {
      tx.abort()
    } catch {
      // transazione gia' terminata in errore: niente da annullare
    }
    if (error instanceof DOMException && error.name === 'QuotaExceededError') throw new StorageQuotaError()
    throw error
  }
  notifyQueueChanged()
  return batch
}

export interface QueueBatchView {
  batch: QueuedBatch
  photos: QueuedPhoto[]
}

/** Lotti con le loro foto, dal piu' recente. */
export async function listBatches(): Promise<QueueBatchView[]> {
  const db = await getDb()
  const [batches, photos] = await Promise.all([db.getAll('batches'), db.getAll('photos')])
  return batches
    .sort((a, b) => b.createdAt - a.createdAt)
    .map((batch) => ({
      batch,
      photos: photos.filter((photo) => photo.batchId === batch.id).sort((a, b) => a.createdAt - b.createdAt),
    }))
}

export async function getBatch(batchId: string): Promise<QueuedBatch | undefined> {
  const db = await getDb()
  return db.get('batches', batchId)
}

export async function getPhotoBlob(photoId: string): Promise<Blob | undefined> {
  const db = await getDb()
  const record: PhotoBlobRecord | undefined = await db.get('blobs', photoId)
  return record?.blob
}

export async function getPhoto(photoId: string): Promise<QueuedPhoto | undefined> {
  const db = await getDb()
  return db.get('photos', photoId)
}

/** Aggiorna i metadati di una foto se esiste ancora (potrebbe essere stata eliminata durante l'invio). */
export async function updatePhoto(photoId: string, patch: Partial<QueuedPhoto>): Promise<void> {
  const db = await getDb()
  const tx = db.transaction(DbConfig.stores.photos, 'readwrite')
  const current = await tx.store.get(photoId)
  if (current) await tx.store.put({ ...current, ...patch })
  await tx.done
  notifyQueueChanged()
}

/** Foto inviata: libera il Blob ma tiene i metadati per la cronologia. */
export async function markPhotoSent(photoId: string): Promise<void> {
  const db = await getDb()
  const tx = db.transaction([DbConfig.stores.photos, DbConfig.stores.blobs], 'readwrite')
  const current = await tx.objectStore('photos').get(photoId)
  if (current) {
    await tx.objectStore('photos').put({
      ...current,
      status: PhotoStatus.sent,
      sentAt: Date.now(),
      lastError: null,
      nextAttemptAt: null,
    })
  }
  await tx.objectStore('blobs').delete(photoId)
  await tx.done
  notifyQueueChanged()
}

/** Foto di un lotto ancora da inviare, in ordine di scatto. */
export async function listPhotosByStatus(status: QueuedPhoto['status']): Promise<QueuedPhoto[]> {
  const db = await getDb()
  const photos = await db.getAllFromIndex('photos', DbConfig.indexes.photosByStatus, status)
  return photos.sort((a, b) => a.createdAt - b.createdAt)
}

async function removeBatchIfEmpty(batchId: string): Promise<void> {
  const db = await getDb()
  const remaining = await db.countFromIndex('photos', DbConfig.indexes.photosByBatch, batchId)
  if (remaining === 0) await db.delete('batches', batchId)
}

export async function deletePhoto(photoId: string): Promise<void> {
  const db = await getDb()
  const photo = await db.get('photos', photoId)
  const tx = db.transaction([DbConfig.stores.photos, DbConfig.stores.blobs], 'readwrite')
  await tx.objectStore('photos').delete(photoId)
  await tx.objectStore('blobs').delete(photoId)
  await tx.done
  if (photo) await removeBatchIfEmpty(photo.batchId)
  notifyQueueChanged()
}

export async function deleteBatch(batchId: string): Promise<void> {
  const db = await getDb()
  const photos = await db.getAllFromIndex('photos', DbConfig.indexes.photosByBatch, batchId)
  const tx = db.transaction(
    [DbConfig.stores.batches, DbConfig.stores.photos, DbConfig.stores.blobs],
    'readwrite',
  )
  for (const photo of photos) {
    await tx.objectStore('photos').delete(photo.photoId)
    await tx.objectStore('blobs').delete(photo.photoId)
  }
  await tx.objectStore('batches').delete(batchId)
  await tx.done
  notifyQueueChanged()
}

/** Rimuove dalla cronologia tutte le foto gia' inviate (e i lotti rimasti vuoti). */
export async function clearSent(): Promise<void> {
  const sent = await listPhotosByStatus(PhotoStatus.sent)
  const batchIds = new Set(sent.map((photo) => photo.batchId))
  const db = await getDb()
  const tx = db.transaction(DbConfig.stores.photos, 'readwrite')
  for (const photo of sent) await tx.store.delete(photo.photoId)
  await tx.done
  for (const batchId of batchIds) await removeBatchIfEmpty(batchId)
  notifyQueueChanged()
}

/** Elimina le inviate piu' vecchie di `sentRetentionDays`. */
export async function purgeOldSent(now: number = Date.now()): Promise<void> {
  const sent = await listPhotosByStatus(PhotoStatus.sent)
  const limit = now - QueueConfig.sentRetentionDays * MS_PER_DAY
  const old = sent.filter((photo) => (photo.sentAt ?? photo.createdAt) < limit)
  for (const photo of old) await deletePhoto(photo.photoId)
}

/** Riprova manuale: azzera tentativi ed errore, e il lotto non e' piu' "solo manuale". */
export async function resetPhotoForRetry(photoId: string): Promise<void> {
  const photo = await getPhoto(photoId)
  if (!photo) return
  const db = await getDb()
  const batch = await db.get('batches', photo.batchId)
  if (batch?.manual) await db.put('batches', { ...batch, manual: false })
  await updatePhoto(photoId, {
    status: PhotoStatus.queued,
    attempts: 0,
    nextAttemptAt: null,
    lastError: null,
  })
}

/** "Invia" su un lotto messo in coda: da quel momento e' idoneo all'invio automatico. */
export async function releaseBatch(batchId: string): Promise<void> {
  const db = await getDb()
  const batch = await db.get('batches', batchId)
  if (batch?.manual) await db.put('batches', { ...batch, manual: false })
  notifyQueueChanged()
}

/** "Invia tutto": rilascia tutti i lotti manuali. */
export async function releaseAllBatches(): Promise<void> {
  const db = await getDb()
  const batches = await db.getAll('batches')
  for (const batch of batches.filter((b) => b.manual)) await db.put('batches', { ...batch, manual: false })
  notifyQueueChanged()
}
