<script setup lang="ts">
import { computed } from 'vue'

import type { ScopeField } from '@/types/contract'

const props = defineProps<{
  field: ScopeField
  modelValue: string
  error?: string
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const inputId = computed(() => `field-${props.field.key}`)
const errorId = computed(() => `${inputId.value}-error`)
const inputType = computed(() => (props.field.type === 'date' ? 'date' : 'text'))
const inputMode = computed(() => (props.field.type === 'number' ? 'decimal' : undefined))

function onInput(event: Event): void {
  emit('update:modelValue', (event.target as HTMLInputElement).value)
}
</script>

<template>
  <div class="field">
    <label :for="inputId" class="field__label">
      {{ field.label }}
      <span v-if="field.required" class="field__required">(obbligatorio)</span>
    </label>
    <input
      :id="inputId"
      class="field-input"
      :type="inputType"
      :inputmode="inputMode"
      :value="modelValue"
      :maxlength="field.maxLength ?? undefined"
      :aria-invalid="error ? 'true' : undefined"
      :aria-describedby="error ? errorId : undefined"
      autocomplete="off"
      @input="onInput"
    />
    <p v-if="error" :id="errorId" class="field__error">Attenzione: {{ error }}</p>
  </div>
</template>

<style scoped>
.field {
  display: grid;
  gap: var(--space-1);
}

.field__label {
  font-weight: 600;
}

.field__required {
  font-weight: 400;
  color: var(--c-ink-muted);
  font-size: var(--font-size-small);
}

.field__error {
  color: var(--c-alert);
  font-size: var(--font-size-small);
  font-weight: 600;
}
</style>
