/** Tipi del contratto API v1 (fissato 2026-11-13). Sola descrizione dei dati, nessuna logica. */

export type FieldType = 'text' | 'number' | 'date'

export interface ScopeField {
  key: string
  label: string
  type: FieldType
  required: boolean
  default: string | null
  min: number | null
  maxLength: number | null
}

export interface Scope {
  id: string
  name: string
  icon: string
  photoHints: string[]
  fields: ScopeField[]
}

/** Valori dei campi come inviati nella parte `fields`: stringa o numero JSON. */
export type FieldValues = Record<string, string | number>

/** Corpo di 200/201 della POST /inbox/photos. */
export interface UploadResponse {
  photoId: string
  batchId: string
  scope: string
  duplicate: boolean
}

/** Corpo di un 400 (ValidationProblem): chiavi = nome della parte. */
export interface ValidationProblemBody {
  title?: string
  errors?: Record<string, string[]>
}
