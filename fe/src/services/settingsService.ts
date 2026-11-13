import { SettingKey } from '@/config/appConfig'
import { getDb } from '@/services/db'

/*
 * Impostazioni persistenti in IndexedDB. Il token dell'API sta solo qui (mai nel bundle, mai in env Vite,
 * mai nei log): decisione "schema B" del piano photo-inbox. Il token non e' leggibile da template: si espone
 * solo se esiste (`hasApiToken`).
 */

async function readSetting<T>(key: string): Promise<T | undefined> {
  const db = await getDb()
  return (await db.get('settings', key)) as T | undefined
}

async function writeSetting(key: string, value: unknown): Promise<void> {
  const db = await getDb()
  await db.put('settings', value, key)
}

export async function getApiToken(): Promise<string | null> {
  const token = await readSetting<string>(SettingKey.apiToken)
  return token && token.length > 0 ? token : null
}

export async function hasApiToken(): Promise<boolean> {
  return (await getApiToken()) !== null
}

export async function setApiToken(token: string): Promise<void> {
  await writeSetting(SettingKey.apiToken, token.trim())
}

export async function clearApiToken(): Promise<void> {
  const db = await getDb()
  await db.delete('settings', SettingKey.apiToken)
}

export async function getSendOriginal(): Promise<boolean> {
  return (await readSetting<boolean>(SettingKey.sendOriginal)) === true
}

export async function setSendOriginal(value: boolean): Promise<void> {
  await writeSetting(SettingKey.sendOriginal, value)
}

export async function readCache<T>(key: string): Promise<T | undefined> {
  return readSetting<T>(key)
}

export async function writeCache(key: string, value: unknown): Promise<void> {
  await writeSetting(key, value)
}
