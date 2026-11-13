import { computed, ref } from 'vue'

import { PhotoStatus, SyncConfig } from '@/config/appConfig'
import { listBatches, type QueueBatchView } from '@/services/queueService'

/*
 * Stato reattivo della coda, condiviso (singleton di modulo) tra badge della navigazione e schermata Coda.
 * Si aggiorna da solo quando cambia lo store, anche per scritture del service worker (BroadcastChannel).
 */

const batches = ref<QueueBatchView[]>([])
let started = false

async function reload(): Promise<void> {
  batches.value = await listBatches()
}

function start(): void {
  if (started) return
  started = true
  void reload()
  if (typeof BroadcastChannel !== 'undefined') {
    const channel = new BroadcastChannel(SyncConfig.channelName)
    channel.onmessage = () => void reload()
  }
}

export function useQueue() {
  start()

  const pendingCount = computed(() =>
    batches.value.reduce(
      (total, entry) => total + entry.photos.filter((photo) => photo.status !== PhotoStatus.sent).length,
      0,
    ),
  )
  const hasManualBatches = computed(() => batches.value.some((entry) => entry.batch.manual))
  const hasSent = computed(() =>
    batches.value.some((entry) => entry.photos.some((photo) => photo.status === PhotoStatus.sent)),
  )

  return { batches, pendingCount, hasManualBatches, hasSent, reload }
}
