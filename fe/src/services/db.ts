import { openDB, type DBSchema, type IDBPDatabase } from 'idb'

import { DbConfig } from '@/config/appConfig'
import type { PhotoBlobRecord, QueuedBatch, QueuedPhoto } from '@/types/queue'

/** Schema IndexedDB. Modulo sicuro anche nel service worker (nessun riferimento a window/document). */
interface PhotodropDb extends DBSchema {
  batches: {
    key: string
    value: QueuedBatch
  }
  photos: {
    key: string
    value: QueuedPhoto
    indexes: { byBatch: string; byStatus: string }
  }
  blobs: {
    key: string
    value: PhotoBlobRecord
  }
  settings: {
    key: string
    value: unknown
  }
}

let dbPromise: Promise<IDBPDatabase<PhotodropDb>> | null = null

/** Apre (una sola volta) il database, creando gli store alla prima apertura. */
export function getDb(): Promise<IDBPDatabase<PhotodropDb>> {
  dbPromise ??= openDB<PhotodropDb>(DbConfig.name, DbConfig.version, {
    upgrade(db) {
      db.createObjectStore(DbConfig.stores.batches, { keyPath: 'id' })
      const photos = db.createObjectStore(DbConfig.stores.photos, { keyPath: 'photoId' })
      photos.createIndex(DbConfig.indexes.photosByBatch, 'batchId')
      photos.createIndex(DbConfig.indexes.photosByStatus, 'status')
      db.createObjectStore(DbConfig.stores.blobs, { keyPath: 'photoId' })
      db.createObjectStore(DbConfig.stores.settings)
    },
  })
  return dbPromise
}
