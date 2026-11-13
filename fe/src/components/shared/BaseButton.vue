<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'danger'
    type?: 'button' | 'submit'
    disabled?: boolean
    block?: boolean
  }>(),
  { variant: 'secondary', type: 'button', disabled: false, block: false },
)
</script>

<template>
  <button :type="type" :disabled="disabled" class="btn" :class="[`btn--${variant}`, { 'btn--block': block }]">
    <slot />
  </button>
</template>

<style scoped>
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  min-height: var(--hit-target-min);
  padding: var(--space-2) var(--space-4);
  border: 2px solid transparent;
  border-radius: var(--radius-sm);
  font-weight: 600;
  cursor: pointer;
}

.btn--block {
  display: flex;
  width: 100%;
}

.btn--primary {
  background: var(--c-federal);
  color: var(--c-on-federal);
}

.btn--secondary {
  background: var(--c-surface);
  color: var(--c-ink);
  border-color: var(--c-control-border);
}

.btn--danger {
  background: var(--c-alert);
  color: var(--c-on-alert);
}

.btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
</style>
