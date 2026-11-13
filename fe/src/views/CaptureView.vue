<script setup lang="ts">
import { ref } from 'vue'

import PhotoGrid from '@/components/capture/PhotoGrid.vue'
import ScopeFieldInput from '@/components/capture/ScopeFieldInput.vue'
import BaseButton from '@/components/shared/BaseButton.vue'
import MessageBanner from '@/components/shared/MessageBanner.vue'
import { useCaptureSession } from '@/composables/capture/useCaptureSession'
import { CaptureConfig } from '@/config/appConfig'

const emit = defineEmits<{ queued: [] }>()

const { scope, photos, values, errors, isProcessing, hasPhotos, addFiles, previewUrl, removePhoto, reset, submit } =
  useCaptureSession()

const cameraInput = ref<HTMLInputElement | null>(null)
const galleryInput = ref<HTMLInputElement | null>(null)
const isSubmitting = ref(false)
const errorMessage = ref('')

async function onFilesChosen(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    errorMessage.value = ''
    try {
      await addFiles(input.files)
    } catch {
      errorMessage.value = 'Non riesco a elaborare la foto: riprova.'
    }
  }
  // Permette di riscattare lo stesso file
  input.value = ''
}

async function onSubmit(manual: boolean): Promise<void> {
  isSubmitting.value = true
  errorMessage.value = ''
  try {
    if (await submit(manual)) emit('queued')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Salvataggio non riuscito'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <form v-if="scope" class="view" novalidate @submit.prevent="onSubmit(false)">
    <header class="head">
      <h1>{{ scope.name }}</h1>
      <BaseButton :disabled="isSubmitting" @click="reset">Cambia ambito</BaseButton>
    </header>

    <MessageBanner v-if="errorMessage" tone="alert">{{ errorMessage }}</MessageBanner>

    <section class="block" aria-labelledby="photos-title">
      <h2 id="photos-title">Foto ({{ photos.length }})</h2>
      <p v-if="scope.photoHints.length > 0" class="muted">Inquadra: {{ scope.photoHints.join(', ') }}</p>

      <input
        ref="cameraInput"
        class="sr-only"
        type="file"
        :accept="CaptureConfig.accept"
        :capture="CaptureConfig.captureMode"
        multiple
        tabindex="-1"
        aria-hidden="true"
        @change="onFilesChosen"
      />
      <input
        ref="galleryInput"
        class="sr-only"
        type="file"
        :accept="CaptureConfig.accept"
        multiple
        tabindex="-1"
        aria-hidden="true"
        @change="onFilesChosen"
      />

      <BaseButton variant="primary" block :disabled="isProcessing" @click="cameraInput?.click()">
        {{ hasPhotos ? 'Scatta un\'altra foto' : 'Scatta foto' }}
      </BaseButton>
      <BaseButton block :disabled="isProcessing" @click="galleryInput?.click()">Scegli dalla galleria</BaseButton>

      <p v-if="isProcessing" class="muted" role="status">Elaborazione foto...</p>
      <PhotoGrid :photos="photos" :preview-url="previewUrl" @remove="removePhoto" />
    </section>

    <section v-if="scope.fields.length > 0" class="block" aria-labelledby="fields-title">
      <h2 id="fields-title">Dati del lotto</h2>
      <p class="muted">Valgono per tutte le foto di questo invio.</p>
      <ScopeFieldInput
        v-for="field in scope.fields"
        :key="field.key"
        :field="field"
        :model-value="values[field.key] ?? ''"
        :error="errors[field.key]"
        @update:model-value="values[field.key] = $event"
      />
    </section>

    <section class="actions">
      <BaseButton type="submit" variant="primary" block :disabled="!hasPhotos || isProcessing || isSubmitting">
        Invia ora
      </BaseButton>
      <BaseButton block :disabled="!hasPhotos || isProcessing || isSubmitting" @click="onSubmit(true)">
        Metti in coda
      </BaseButton>
    </section>
  </form>
</template>

<style scoped>
.view {
  display: grid;
  gap: var(--space-4);
}

.head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--space-2);
}

.block {
  display: grid;
  gap: var(--space-3);
  background: var(--c-surface);
  border: 1px solid var(--c-panel-line);
  border-radius: var(--radius);
  padding: var(--space-4);
}

.actions {
  display: grid;
  gap: var(--space-2);
}
</style>
