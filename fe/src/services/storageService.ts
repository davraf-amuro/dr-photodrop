export interface StorageUsage {
  usage: number
  quota: number
}

/** Chiede al browser di non eliminare i dati della PWA sotto pressione di spazio. Ritorna true se concesso. */
export async function requestPersistence(): Promise<boolean> {
  if (!navigator.storage?.persist) return false
  try {
    return (await navigator.storage.persisted()) || (await navigator.storage.persist())
  } catch {
    return false
  }
}

export async function isPersisted(): Promise<boolean> {
  if (!navigator.storage?.persisted) return false
  try {
    return await navigator.storage.persisted()
  } catch {
    return false
  }
}

/** Occupazione dello storage di origine; null se il browser non la espone. */
export async function estimateStorage(): Promise<StorageUsage | null> {
  if (!navigator.storage?.estimate) return null
  try {
    const { usage, quota } = await navigator.storage.estimate()
    return { usage: usage ?? 0, quota: quota ?? 0 }
  } catch {
    return null
  }
}
