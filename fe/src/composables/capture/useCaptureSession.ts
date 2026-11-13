import { computed, ref } from 'vue'

import { processImage, resolveCapturedAt } from '@/services/imageService'
import { enqueueBatch } from '@/services/queueService'
import { getSendOriginal } from '@/services/settingsService'
import { requestBackgroundSync, runFlush } from '@/services/syncScheduler'
import type { Scope } from '@/types/contract'
import type { DraftPhoto } from '@/types/queue'
import { extensionFor } from '@/utils/fileTypes'
import { buildInitialValues, validateFields, type FormValues } from '@/utils/fieldValidation'

/*
 * Bozza di scatto corrente: ambito scelto, foto e valori dei campi. Singleton di modulo, cosi' cambiare
 * scheda (Coda, Impostazioni) non perde le foto. La bozza vive in memoria: si perde solo chiudendo l'app
 * prima di accodare.
 */

const scope = ref<Scope | null>(null)
const photos = ref<DraftPhoto[]>([])
const values = ref<FormValues>({})
const errors = ref<Record<string, string>>({})
const isProcessing = ref(false)
const previewUrls = new Map<string, string>()

function revokePreview(photoId: string): void {
  const url = previewUrls.get(photoId)
  if (url) URL.revokeObjectURL(url)
  previewUrls.delete(photoId)
}

export function useCaptureSession() {
  function chooseScope(chosen: Scope): void {
    scope.value = chosen
    values.value = buildInitialValues(chosen.fields)
  }

  /** Aggiunge le foto scattate: ridimensiona, genera miniatura e `photoId` (GUID alla cattura). */
  async function addFiles(files: FileList | File[]): Promise<void> {
    isProcessing.value = true
    try {
      const sendOriginal = await getSendOriginal()
      for (const file of Array.from(files)) {
        const processed = await processImage(file, sendOriginal)
        const photoId = crypto.randomUUID()
        photos.value.push({
          photoId,
          blob: processed.blob,
          thumb: processed.thumb,
          capturedAt: resolveCapturedAt(file),
          fileName: `${photoId}.${extensionFor(processed.blob.type)}`,
        })
      }
    } finally {
      isProcessing.value = false
    }
  }

  function previewUrl(photo: DraftPhoto): string {
    let url = previewUrls.get(photo.photoId)
    if (!url) {
      url = URL.createObjectURL(photo.thumb.size > 0 ? photo.thumb : photo.blob)
      previewUrls.set(photo.photoId, url)
    }
    return url
  }

  function removePhoto(photoId: string): void {
    revokePreview(photoId)
    photos.value = photos.value.filter((photo) => photo.photoId !== photoId)
  }

  /** Svuota la bozza (dopo l'accodamento o con "Cambia ambito"). */
  function reset(): void {
    for (const photo of photos.value) revokePreview(photo.photoId)
    photos.value = []
    scope.value = null
    values.value = {}
    errors.value = {}
  }

  const hasPhotos = computed(() => photos.value.length > 0)

  /**
   * Valida i campi e accoda il lotto (stesso `batchId` per tutte le foto).
   * `manual` = "Metti in coda" (parte solo dalla schermata Coda); altrimenti "Invia ora".
   * Ritorna false se la validazione fallisce (errori in `errors`); lancia QueueFullError/StorageQuotaError.
   */
  async function submit(manual: boolean): Promise<boolean> {
    if (!scope.value || photos.value.length === 0) return false
    const result = validateFields(scope.value.fields, values.value)
    errors.value = result.errors
    if (Object.keys(result.errors).length > 0) return false

    await enqueueBatch({
      scopeId: scope.value.id,
      scopeName: scope.value.name,
      fields: result.values,
      photos: photos.value,
      manual,
    })
    reset()
    if (!manual) {
      void requestBackgroundSync()
      void runFlush(false, true)
    }
    return true
  }

  return {
    scope,
    photos,
    values,
    errors,
    isProcessing,
    hasPhotos,
    chooseScope,
    addFiles,
    previewUrl,
    removePhoto,
    reset,
    submit,
  }
}
