import { onMounted, onUnmounted, ref } from 'vue'

/** Stato di rete del browser (aggiornato dagli eventi online/offline). */
export function useOnline() {
  const isOnline = ref(navigator.onLine)
  const update = () => {
    isOnline.value = navigator.onLine
  }

  onMounted(() => {
    window.addEventListener('online', update)
    window.addEventListener('offline', update)
  })
  onUnmounted(() => {
    window.removeEventListener('online', update)
    window.removeEventListener('offline', update)
  })

  return { isOnline }
}
