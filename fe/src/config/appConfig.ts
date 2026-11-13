/**
 * Configurazione centralizzata della PWA: ogni limite, soglia o nome con significato sta qui.
 * Nessun segreto (il token dell'API vive in IndexedDB sul telefono, mai nel bundle).
 */

/** API: base URL relativo (in dev lo inoltra il proxy di vite.config.ts, in produzione nginx). */
export const ApiConfig = {
  basePath: '/api/v1',
  scopesPath: '/scopes',
  uploadPath: '/inbox/photos',
  apiKeyHeader: 'X-Api-Key',
  /** Nomi delle parti multipart, fissati dal contratto API. */
  parts: {
    file: 'file',
    scope: 'scope',
    photoId: 'photoId',
    batchId: 'batchId',
    capturedAt: 'capturedAt',
    fields: 'fields',
  },
  /** Timeout di una singola richiesta (rete mobile lenta con foto da ~1 MB). */
  requestTimeoutMs: 60_000,
} as const

/** Esiti HTTP dell'upload secondo il contratto. */
export const HttpStatus = {
  ok: 200,
  created: 201,
  badRequest: 400,
  unauthorized: 401,
  payloadTooLarge: 413,
  serverErrorFrom: 500,
} as const

/** Ridimensionamento lato client. */
export const ImageConfig = {
  maxLongSidePx: 2000,
  jpegQuality: 0.85,
  jpegMimeType: 'image/jpeg',
  thumbnailLongSidePx: 240,
  thumbnailQuality: 0.7,
} as const

/** Scatto: input file con fotocamera posteriore. */
export const CaptureConfig = {
  accept: 'image/*',
  captureMode: 'environment',
  /** capturedAt: si usa la data del file se plausibile, altrimenti l'istante corrente. */
  maxCapturedAtAgeDays: 300,
} as const

/** Coda offline. */
export const QueueConfig = {
  /** Tentativi automatici per foto prima di passare a "errore" (riprova manuale possibile). */
  maxAttempts: 8,
  backoffBaseMs: 5_000,
  backoffFactor: 2,
  backoffMaxMs: 15 * 60_000,
  /** Tetto di foto presenti in coda (rischio quota storage). */
  maxPhotosInQueue: 150,
  /** Le foto inviate restano visibili (senza Blob) per questo numero di giorni. */
  sentRetentionDays: 3,
  /** Soglia oltre la quale l'occupazione storage e' evidenziata come alta. */
  storageWarnRatio: 0.8,
} as const

/** Stati di una foto in coda. */
export const PhotoStatus = {
  queued: 'queued',
  sending: 'sending',
  sent: 'sent',
  error: 'error',
} as const

/** Database locale. */
export const DbConfig = {
  name: 'dr-photodrop',
  version: 1,
  stores: {
    batches: 'batches',
    photos: 'photos',
    blobs: 'blobs',
    settings: 'settings',
  },
  indexes: {
    photosByBatch: 'byBatch',
    photosByStatus: 'byStatus',
  },
} as const

/** Chiavi dello store impostazioni. */
export const SettingKey = {
  apiToken: 'apiToken',
  sendOriginal: 'sendOriginal',
  scopesCache: 'scopesCache',
} as const

/** Sincronizzazione: nomi condivisi tra pagina e service worker. */
export const SyncConfig = {
  backgroundSyncTag: 'photodrop-sync',
  lockName: 'photodrop-sync-lock',
  channelName: 'photodrop-queue',
} as const

/** Preferenze non sensibili in localStorage (ammesso: solo tema). */
export const PreferenceKey = {
  theme: 'photodrop.theme',
} as const

/** Tema: valori dell'attributo data-theme di <html>. */
export const ThemeMode = {
  light: 'light',
  dark: 'dark',
  system: 'system',
} as const
