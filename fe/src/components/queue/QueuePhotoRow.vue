<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from 'vue'

import BaseButton from '@/components/shared/BaseButton.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { PhotoStatus } from '@/config/appConfig'
import type { QueuedPhoto } from '@/types/queue'
import { formatBytes, formatTime } from '@/utils/formatters'

const props = defineProps<{ photo: QueuedPhoto }>()
const emit = defineEmits<{ retry: [photoId: string]; remove: [photoId: string] }>()

const thumbUrl = ref<string | null>(null)

watch(
  () => props.photo.thumb,
  (thumb) => {
    if (thumbUrl.value) URL.revokeObjectURL(thumbUrl.value)
    thumbUrl.value = thumb ? URL.createObjectURL(thumb) : null
  },
  { immediate: true },
)

onUnmounted(() => {
  if (thumbUrl.value) URL.revokeObjectURL(thumbUrl.value)
})

const badge = computed(() => {
  switch (props.photo.status) {
    case PhotoStatus.sent:
      return { tone: 'ok', label: 'Inviata' } as const
    case PhotoStatus.sending:
      return { tone: 'federal', label: 'Invio in corso' } as const
    case PhotoStatus.error:
      return { tone: 'alert', label: 'Errore' } as const
    default:
      return { tone: 'neutral', label: 'In coda' } as const
  }
})

const retryInfo = computed(() =>
  props.photo.status === PhotoStatus.queued && props.photo.nextAttemptAt !== null
    ? `Nuovo tentativo alle ${formatTime(props.photo.nextAttemptAt)} (tentativo ${props.photo.attempts})`
    : null,
)
const canRetry = computed(() => props.photo.status === PhotoStatus.error || props.photo.status === PhotoStatus.queued)
</script>

<template>
  <li class="row">
    <img v-if="thumbUrl" class="row__thumb" :src="thumbUrl" alt="" />
    <div v-else class="row__thumb row__thumb--empty" aria-hidden="true"></div>

    <div class="row__body">
      <div class="row__head">
        <StatusBadge :tone="badge.tone" :label="badge.label" />
        <span class="muted">{{ formatBytes(photo.sizeBytes) }}</span>
      </div>
      <p v-if="photo.lastError" class="row__error">{{ photo.lastError }}</p>
      <p v-if="retryInfo" class="muted">{{ retryInfo }}</p>
      <div class="row__actions">
        <BaseButton v-if="canRetry" @click="emit('retry', photo.photoId)">Riprova</BaseButton>
        <BaseButton variant="danger" @click="emit('remove', photo.photoId)">Elimina</BaseButton>
      </div>
    </div>
  </li>
</template>

<style scoped>
.row {
  display: grid;
  grid-template-columns: var(--thumb-size) 1fr;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-top: 1px solid var(--c-panel-line);
}

.row__thumb {
  width: var(--thumb-size);
  height: var(--thumb-size);
  object-fit: cover;
  border-radius: var(--radius-sm);
  background: var(--c-panel-line);
}

.row__body {
  display: grid;
  gap: var(--space-2);
  align-content: start;
  min-width: 0;
}

.row__head {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.row__error {
  color: var(--c-alert);
  font-size: var(--font-size-small);
  font-weight: 600;
  overflow-wrap: anywhere;
}

.row__actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}
</style>
