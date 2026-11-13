<script setup lang="ts">
import { computed } from 'vue'

import QueuePhotoRow from '@/components/queue/QueuePhotoRow.vue'
import BaseButton from '@/components/shared/BaseButton.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { QueueBatchView } from '@/services/queueService'
import { formatDateTime } from '@/utils/formatters'

const props = defineProps<{ entry: QueueBatchView }>()
const emit = defineEmits<{
  release: [batchId: string]
  removeBatch: [batchId: string]
  retryPhoto: [photoId: string]
  removePhoto: [photoId: string]
}>()

const fieldSummary = computed(() =>
  Object.entries(props.entry.batch.fields)
    .map(([key, value]) => `${key}: ${value}`)
    .join(' - '),
)
</script>

<template>
  <section class="card">
    <header class="card__head">
      <div>
        <h2>{{ entry.batch.scopeName }}</h2>
        <p class="muted">{{ formatDateTime(entry.batch.createdAt) }} - {{ entry.photos.length }} foto</p>
        <p v-if="fieldSummary" class="muted">{{ fieldSummary }}</p>
      </div>
      <StatusBadge v-if="entry.batch.manual" tone="caution" label="Da inviare a mano" />
    </header>

    <ul class="card__photos">
      <QueuePhotoRow
        v-for="photo in entry.photos"
        :key="photo.photoId"
        :photo="photo"
        @retry="emit('retryPhoto', $event)"
        @remove="emit('removePhoto', $event)"
      />
    </ul>

    <footer class="card__actions">
      <BaseButton v-if="entry.batch.manual" variant="primary" @click="emit('release', entry.batch.id)">
        Invia questo lotto
      </BaseButton>
      <BaseButton variant="danger" @click="emit('removeBatch', entry.batch.id)">Elimina lotto</BaseButton>
    </footer>
  </section>
</template>

<style scoped>
.card {
  background: var(--c-surface);
  border: 1px solid var(--c-panel-line);
  border-radius: var(--radius);
  padding: var(--space-4);
  display: grid;
  gap: var(--space-2);
}

.card__head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--space-2);
}

.card__photos {
  list-style: none;
  margin: 0;
  padding: 0;
}

.card__actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}
</style>
