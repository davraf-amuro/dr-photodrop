const KILOBYTE = 1024
const UNITS = ['B', 'KB', 'MB', 'GB'] as const

/** Dimensione leggibile, es. `1,4 MB`. */
export function formatBytes(bytes: number): string {
  let value = bytes
  let unitIndex = 0
  while (value >= KILOBYTE && unitIndex < UNITS.length - 1) {
    value /= KILOBYTE
    unitIndex += 1
  }
  const digits = unitIndex === 0 ? 0 : 1
  return `${value.toLocaleString('it-IT', { maximumFractionDigits: digits })} ${UNITS[unitIndex]}`
}

/** Data e ora brevi in italiano. */
export function formatDateTime(epochMs: number): string {
  return new Date(epochMs).toLocaleString('it-IT', { dateStyle: 'short', timeStyle: 'short' })
}

/** Solo l'ora, es. `10:15`. */
export function formatTime(epochMs: number): string {
  return new Date(epochMs).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })
}
