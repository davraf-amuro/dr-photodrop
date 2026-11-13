import { CaptureConfig, ImageConfig } from '@/config/appConfig'
import { toIsoWithOffset } from '@/utils/dateTime'

const MS_PER_DAY = 86_400_000

/** Foto pronta per l'invio e la sua miniatura. */
export interface ProcessedImage {
  blob: Blob
  thumb: Blob
}

function scaleToLongSide(width: number, height: number, maxLongSide: number): { width: number; height: number } {
  const longSide = Math.max(width, height)
  if (longSide <= maxLongSide) return { width, height }
  const ratio = maxLongSide / longSide
  return { width: Math.round(width * ratio), height: Math.round(height * ratio) }
}

async function encodeJpeg(bitmap: ImageBitmap, maxLongSide: number, quality: number): Promise<Blob> {
  const { width, height } = scaleToLongSide(bitmap.width, bitmap.height, maxLongSide)
  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height
  const context = canvas.getContext('2d')
  if (!context) throw new Error('Canvas 2D non disponibile')
  context.drawImage(bitmap, 0, 0, width, height)
  return new Promise<Blob>((resolve, reject) => {
    canvas.toBlob(
      (result) => (result ? resolve(result) : reject(new Error('Codifica JPEG non riuscita'))),
      ImageConfig.jpegMimeType,
      quality,
    )
  })
}

/**
 * Prepara una foto scattata: ridimensiona (lato lungo max, JPEG) salvo `sendOriginal`, e genera la miniatura.
 * L'orientamento EXIF viene applicato da `createImageBitmap`. Se il file non e' decodificabile dal browser
 * (es. formato insolito) si tiene l'originale e la miniatura resta vuota: la foto parte comunque.
 */
export async function processImage(file: File, sendOriginal: boolean): Promise<ProcessedImage> {
  let bitmap: ImageBitmap
  try {
    bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
  } catch {
    return { blob: file, thumb: new Blob([], { type: ImageConfig.jpegMimeType }) }
  }

  try {
    const thumb = await encodeJpeg(bitmap, ImageConfig.thumbnailLongSidePx, ImageConfig.thumbnailQuality)
    if (sendOriginal) return { blob: file, thumb }

    const needsResize = Math.max(bitmap.width, bitmap.height) > ImageConfig.maxLongSidePx
    // Gia' piccola e JPEG: inutile ricomprimere (si perderebbe qualita' senza guadagno)
    if (!needsResize && file.type === ImageConfig.jpegMimeType) return { blob: file, thumb }

    const blob = await encodeJpeg(bitmap, ImageConfig.maxLongSidePx, ImageConfig.jpegQuality)
    return { blob, thumb }
  } finally {
    bitmap.close()
  }
}

/** capturedAt: data del file se plausibile (non futura, non troppo vecchia), altrimenti ora corrente. */
export function resolveCapturedAt(file: File, now: Date = new Date()): string {
  const nowMs = now.getTime()
  const age = nowMs - file.lastModified
  const plausible = file.lastModified > 0 && age >= 0 && age <= CaptureConfig.maxCapturedAtAgeDays * MS_PER_DAY
  return toIsoWithOffset(plausible ? new Date(file.lastModified) : now)
}
