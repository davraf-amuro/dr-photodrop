import type { PhotoStatus } from '@/config/appConfig'
import type { FieldValues } from '@/types/contract'

export type PhotoStatusValue = (typeof PhotoStatus)[keyof typeof PhotoStatus]

/** Lotto: foto scattate insieme, stesso ambito e stessi campi. */
export interface QueuedBatch {
  id: string
  scopeId: string
  scopeName: string
  fields: FieldValues
  createdAt: number
  /** true = "Metti in coda": non parte da solo, solo con "Invia" dalla schermata Coda. */
  manual: boolean
}

/** Metadati di una foto in coda (il Blob pesante sta nello store `blobs`). */
export interface QueuedPhoto {
  photoId: string
  batchId: string
  capturedAt: string
  createdAt: number
  sizeBytes: number
  contentType: string
  thumb: Blob | null
  status: PhotoStatusValue
  attempts: number
  /** Epoch ms del prossimo tentativo automatico (backoff). */
  nextAttemptAt: number | null
  lastError: string | null
  sentAt: number | null
}

export interface PhotoBlobRecord {
  photoId: string
  blob: Blob
}

/** Foto catturata e non ancora accodata (bozza in memoria). */
export interface DraftPhoto {
  photoId: string
  blob: Blob
  thumb: Blob
  capturedAt: string
  fileName: string
}

/** Esito dell'upload di una foto, tradotto dal contratto. */
export type UploadOutcome =
  | { kind: 'ok'; duplicate: boolean }
  | { kind: 'rejected'; message: string }
  | { kind: 'unauthorized' }
  | { kind: 'retry'; message: string }

/** Riepilogo di una passata di invio. */
export interface FlushSummary {
  sent: number
  failed: number
  authRequired: boolean
  /** Epoch ms del primo prossimo tentativo programmato, se presente. */
  nextAttemptAt: number | null
  /** true se un'altra passata era gia' in corso. */
  skipped: boolean
}
