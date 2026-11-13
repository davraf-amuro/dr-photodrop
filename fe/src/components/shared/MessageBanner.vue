<script setup lang="ts">
withDefaults(
  defineProps<{
    tone?: 'info' | 'alert' | 'caution' | 'ok'
    title?: string
  }>(),
  { tone: 'info', title: '' },
)
</script>

<template>
  <div class="banner" :class="`banner--${tone}`" :role="tone === 'alert' ? 'alert' : 'status'">
    <strong v-if="title" class="banner__title">{{ title }}</strong>
    <div><slot /></div>
  </div>
</template>

<style scoped>
.banner {
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-sm);
  border: 2px solid var(--c-panel-line);
  background: var(--c-surface);
  display: grid;
  gap: var(--space-1);
}

.banner__title {
  font-weight: 700;
}

.banner--alert {
  background: var(--c-alert);
  color: var(--c-on-alert);
  border-color: var(--c-alert);
}

.banner--caution {
  background: var(--c-caution);
  color: var(--c-on-caution);
  border-color: var(--c-caution);
}

.banner--ok {
  background: var(--c-ok);
  color: var(--c-on-ok);
  border-color: var(--c-ok);
}
</style>
