import { SettingKey } from '@/config/appConfig'
import { fetchScopes } from '@/services/apiClient'
import { readCache, writeCache } from '@/services/settingsService'
import type { Scope } from '@/types/contract'

export interface ScopeLoadResult {
  scopes: Scope[]
  /** true se l'elenco viene dalla cache locale (offline o server non raggiungibile). */
  fromCache: boolean
}

/**
 * Carica gli ambiti dal server e ne tiene copia locale per l'uso offline.
 * Un 401 non ripiega sulla cache: l'errore sale alla vista, che chiede il token.
 */
export async function loadScopes(): Promise<ScopeLoadResult> {
  try {
    const scopes = await fetchScopes()
    await writeCache(SettingKey.scopesCache, scopes)
    return { scopes, fromCache: false }
  } catch (error) {
    if (error instanceof Error && error.name === 'UnauthorizedError') throw error
    const cached = await readCache<Scope[]>(SettingKey.scopesCache)
    if (cached && cached.length > 0) return { scopes: cached, fromCache: true }
    throw error
  }
}
