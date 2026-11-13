function pad(value: number, length = 2): string {
  return String(Math.trunc(Math.abs(value))).padStart(length, '0')
}

/** ISO 8601 con offset locale, es. `2026-11-13T10:15:00+01:00` (richiesto dal contratto per `capturedAt`). */
export function toIsoWithOffset(date: Date): string {
  const offsetMinutes = -date.getTimezoneOffset()
  const sign = offsetMinutes >= 0 ? '+' : '-'
  const offset = `${sign}${pad(offsetMinutes / 60)}:${pad(offsetMinutes % 60)}`
  const datePart = `${pad(date.getFullYear(), 4)}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
  const timePart = `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
  return `${datePart}T${timePart}${offset}`
}
