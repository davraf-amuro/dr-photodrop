<script setup lang="ts">
import type { DraftPhoto } from '@/types/queue'

defineProps<{
  photos: DraftPhoto[]
  previewUrl: (photo: DraftPhoto) => string
}>()

const emit = defineEmits<{ remove: [photoId: string] }>()
</script>

<template>
  <ul class="grid">
    <li v-for="(photo, index) in photos" :key="photo.photoId" class="grid__item">
      <img class="grid__img" :src="previewUrl(photo)" :alt="`Foto ${index + 1}`" />
      <button type="button" class="grid__remove" :aria-label="`Rimuovi foto ${index + 1}`" @click="emit('remove', photo.photoId)">
        Rimuovi
      </button>
    </li>
  </ul>
</template>

<style scoped>
.grid {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(var(--thumb-size), 1fr));
  gap: var(--space-2);
}

.grid__item {
  display: grid;
  gap: var(--space-1);
}

.grid__img {
  width: 100%;
  aspect-ratio: 1;
  object-fit: cover;
  border-radius: var(--radius-sm);
  background: var(--c-panel-line);
}

.grid__remove {
  min-height: var(--hit-target-min);
  background: var(--c-surface);
  border: 2px solid var(--c-alert);
  color: var(--c-alert);
  border-radius: var(--radius-sm);
  font-weight: 600;
  cursor: pointer;
}
</style>
