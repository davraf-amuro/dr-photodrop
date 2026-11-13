import { ApiConfig, HttpStatus } from '@/config/appConfig'
import { getApiToken } from '@/services/settingsService'
import type { FieldValues, Scope, UploadResponse, ValidationProblemBody } from '@/types/contract'
import type { UploadOutcome } from '@/types/queue'

/** Il server ha risposto 401 o manca il token locale. */
export class UnauthorizedError extends Error {
  constructor() {
    super('Token mancante o non valido')
    this.name = 'UnauthorizedError'
  }
}

/** Errore di rete o risposta 5xx: si puo' ritentare. */
export class NetworkError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'NetworkError'
  }
}

function apiUrl(path: string): string {
  return `${ApiConfig.basePath}${path}`
}

async function authHeaders(): Promise<Headers | null> {
  const token = await getApiToken()
  if (!token) return null
  const headers = new Headers()
  headers.set(ApiConfig.apiKeyHeader, token)
  return headers
}

async function fetchWithTimeout(url: string, init: RequestInit): Promise<Response> {
  return fetch(url, { ...init, signal: AbortSignal.timeout(ApiConfig.requestTimeoutMs) })
}

/** GET /api/v1/scopes. Lancia UnauthorizedError (401 o token assente) o NetworkError (rete/5xx). */
export async function fetchScopes(): Promise<Scope[]> {
  const headers = await authHeaders()
  if (!headers) throw new UnauthorizedError()

  let response: Response
  try {
    response = await fetchWithTimeout(apiUrl(ApiConfig.scopesPath), { headers })
  } catch {
    throw new NetworkError('Server non raggiungibile')
  }
  if (response.status === HttpStatus.unauthorized) throw new UnauthorizedError()
  if (!response.ok) throw new NetworkError(`Risposta inattesa dal server (${response.status})`)
  return (await response.json()) as Scope[]
}

/** Estrae un messaggio leggibile da un 400 ValidationProblem. */
async function describeBadRequest(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as ValidationProblemBody
    const lines = Object.entries(body.errors ?? {}).map(([part, messages]) => `${part}: ${messages.join(' ')}`)
    if (lines.length > 0) return lines.join(' - ')
    if (body.title) return body.title
  } catch {
    // corpo non JSON: messaggio generico
  }
  return 'Richiesta rifiutata dal server (400)'
}

export interface UploadRequest {
  photoId: string
  batchId: string
  scopeId: string
  capturedAt: string
  fields: FieldValues
  blob: Blob
  fileName: string
}

/**
 * POST /api/v1/inbox/photos, una foto per richiesta.
 * 201 e 200 (duplicate) sono entrambi successo; 400/413 definitivi; 401 chiede il token; rete/5xx ritentabili.
 */
export async function uploadPhoto(request: UploadRequest): Promise<UploadOutcome> {
  const headers = await authHeaders()
  if (!headers) return { kind: 'unauthorized' }

  const form = new FormData()
  form.append(ApiConfig.parts.file, request.blob, request.fileName)
  form.append(ApiConfig.parts.scope, request.scopeId)
  form.append(ApiConfig.parts.photoId, request.photoId)
  form.append(ApiConfig.parts.batchId, request.batchId)
  form.append(ApiConfig.parts.capturedAt, request.capturedAt)
  form.append(ApiConfig.parts.fields, JSON.stringify(request.fields))

  let response: Response
  try {
    response = await fetchWithTimeout(apiUrl(ApiConfig.uploadPath), { method: 'POST', headers, body: form })
  } catch {
    return { kind: 'retry', message: 'Server non raggiungibile' }
  }

  switch (response.status) {
    case HttpStatus.created:
      return { kind: 'ok', duplicate: false }
    case HttpStatus.ok: {
      const body = (await response.json().catch(() => null)) as UploadResponse | null
      return { kind: 'ok', duplicate: body?.duplicate ?? true }
    }
    case HttpStatus.badRequest:
      return { kind: 'rejected', message: await describeBadRequest(response) }
    case HttpStatus.unauthorized:
      return { kind: 'unauthorized' }
    case HttpStatus.payloadTooLarge:
      return { kind: 'rejected', message: 'Foto troppo grande per il server (413)' }
    default:
      return { kind: 'retry', message: `Errore del server (${response.status})` }
  }
}
