<script setup lang="ts">
withDefaults(
  defineProps<{
    tone?: 'neutral' | 'ok' | 'alert' | 'caution' | 'federal'
    label: string
  }>(),
  { tone: 'neutral' },
)
</script>

<template>
  <!-- Lo stato e' sempre espresso anche a parole (label), mai solo dal colore -->
  <span class="badge" :class="`badge--${tone}`">{{ label }}</span>
</template>

<style scoped>
.badge {
  display: inline-block;
  padding: 0 var(--space-2);
  border-radius: var(--radius-sm);
  border: 2px solid currentColor;
  font-size: var(--font-size-small);
  font-weight: 700;
  white-space: nowrap;
}

.badge--neutral {
  color: var(--c-neutral);
}

.badge--ok {
  color: var(--c-ok);
}

.badge--alert {
  color: var(--c-alert);
}

.badge--federal {
  color: var(--c-federal);
}

/* caution non si usa mai come colore del testo: fondo + on-caution */
.badge--caution {
  background: var(--c-caution);
  color: var(--c-on-caution);
  border-color: var(--c-caution);
}
</style>
