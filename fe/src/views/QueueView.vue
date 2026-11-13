<script setup lang="ts">
import QueueBatchCard from '@/components/queue/QueueBatchCard.vue'
import BaseButton from '@/components/shared/BaseButton.vue'
import MessageBanner from '@/components/shared/MessageBanner.vue'
import { useQueue } from '@/composables/queue/useQueue'
import { useOnline } from '@/composables/shared/useOnline'
import { useStorageInfo } from '@/composables/shared/useStorageInfo'
import {
  clearSent,
  deleteBatch,
  deletePhoto,
  releaseAllBatches,
  releaseBatch,
  resetPhotoForRetry,
} from '@/services/queueService'
import { runFlush } from '@/services/syncScheduler'
import { formatBytes } from '@/utils/formatters'

const { batches, pendingCount, hasManualBatches, hasSent } = useQueue()
const { isOnline } = useOnline()
const { usage, isHigh, refresh: refreshStorage } = useStorageInfo()

async function sendAll(): Promise<void> {
  await releaseAllBatches()
  await runFlush(true, true)
  await refreshStorage()
}

async function sendBatch(batchId: string): Promise<void> {
  await releaseBatch(batchId)
  await runFlush(false, true)
}

async function retryPhoto(photoId: string): Promise<void> {
  await resetPhotoForRetry(photoId)
  await runFlush(false, true)
}

async function removePhoto(photoId: string): Promise<void> {
  await deletePhoto(photoId)
  await refreshStorage()
}

async function removeBatch(batchId: string): Promise<void> {
  await deleteBatch(batchId)
  await refreshStorage()
}

async function removeSent(): Promise<void> {
  await clearSent()
  await refreshStorage()
}
</script>

<template>
  <section class="view">
    <h1>Coda</h1>

    <MessageBanner v-if="!isOnline" tone="caution" title="Sei offline">
      Le foto restano sul telefono e partono appena torna la rete.
    </MessageBanner>

    <p class="muted" role="status">
      <template v-if="pendingCount > 0">{{ pendingCount }} foto da inviare.</template>
      <template v-else>Niente in attesa.</template>
      <template v-if="usage"> Spazio usato dall'app: {{ formatBytes(usage.usage) }} su {{ formatBytes(usage.quota) }}.</template>
    </p>
    <MessageBanner v-if="isHigh" tone="caution" title="Spazio quasi esaurito">
      Invia o elimina qualche foto dalla coda.
    </MessageBanner>

    <div class="toolbar">
      <BaseButton variant="primary" :disabled="pendingCount === 0 || !isOnline" @click="sendAll">
        {{ hasManualBatches ? 'Invia tutto' : 'Riprova ora' }}
      </BaseButton>
      <BaseButton v-if="hasSent" @click="removeSent">Rimuovi inviate</BaseButton>
    </div>

    <p v-if="batches.length === 0" class="muted">Nessun lotto: scatta qualche foto dalla scheda Scatta.</p>

    <QueueBatchCard
      v-for="entry in batches"
      :key="entry.batch.id"
      :entry="entry"
      @release="sendBatch"
      @remove-batch="removeBatch"
      @retry-photo="retryPhoto"
      @remove-photo="removePhoto"
    />
  </section>
</template>

<style scoped>
.view {
  display: grid;
  gap: var(--space-4);
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}
</style>
