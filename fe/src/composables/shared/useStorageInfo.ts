import { computed, onMounted, ref } from 'vue'

import { QueueConfig } from '@/config/appConfig'
import { estimateStorage, isPersisted, type StorageUsage } from '@/services/storageService'

/** Occupazione dello storage del browser e stato della persistenza. */
export function useStorageInfo() {
  const usage = ref<StorageUsage | null>(null)
  const persisted = ref(false)

  async function refresh(): Promise<void> {
    usage.value = await estimateStorage()
    persisted.value = await isPersisted()
  }

  const ratio = computed(() => (usage.value && usage.value.quota > 0 ? usage.value.usage / usage.value.quota : 0))
  const isHigh = computed(() => ratio.value >= QueueConfig.storageWarnRatio)

  onMounted(() => void refresh())

  return { usage, persisted, ratio, isHigh, refresh }
}
