<script setup lang="ts">
import { onMounted, ref } from 'vue'

import BaseButton from '@/components/shared/BaseButton.vue'
import MessageBanner from '@/components/shared/MessageBanner.vue'
import { useCaptureSession } from '@/composables/capture/useCaptureSession'
import { loadScopes } from '@/services/scopeService'
import type { Scope } from '@/types/contract'

const emit = defineEmits<{ goSettings: [] }>()

const { chooseScope } = useCaptureSession()

const scopes = ref<Scope[]>([])
const isLoading = ref(true)
const fromCache = ref(false)
const needsToken = ref(false)
const errorMessage = ref('')

async function load(): Promise<void> {
  isLoading.value = true
  errorMessage.value = ''
  needsToken.value = false
  try {
    const result = await loadScopes()
    scopes.value = result.scopes
    fromCache.value = result.fromCache
  } catch (error) {
    if (error instanceof Error && error.name === 'UnauthorizedError') needsToken.value = true
    else errorMessage.value = 'Impossibile caricare gli ambiti e nessuna copia salvata: collegati al server almeno una volta.'
  } finally {
    isLoading.value = false
  }
}

onMounted(() => void load())
</script>

<template>
  <section class="view">
    <h1>Cosa fotografi?</h1>

    <MessageBanner v-if="needsToken" tone="caution" title="Serve il token">
      <p>Inserisci il token di accesso nelle Impostazioni per vedere gli ambiti.</p>
      <BaseButton variant="primary" @click="emit('goSettings')">Apri Impostazioni</BaseButton>
    </MessageBanner>

    <MessageBanner v-else-if="errorMessage" tone="alert" title="Ambiti non disponibili">
      {{ errorMessage }}
      <BaseButton @click="load">Riprova</BaseButton>
    </MessageBanner>

    <MessageBanner v-if="fromCache" tone="info">
      Elenco salvato sul telefono (server non raggiungibile): puoi scattare e mettere in coda.
    </MessageBanner>

    <p v-if="isLoading" class="muted" role="status">Caricamento ambiti...</p>

    <ul class="list">
      <li v-for="scope in scopes" :key="scope.id">
        <button type="button" class="scope" @click="chooseScope(scope)">
          <span class="scope__name">{{ scope.name }}</span>
          <span v-if="scope.photoHints.length > 0" class="muted">Suggerimento: {{ scope.photoHints.join(', ') }}</span>
        </button>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.view {
  display: grid;
  gap: var(--space-4);
}

.list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: var(--space-3);
}

.scope {
  display: grid;
  gap: var(--space-1);
  width: 100%;
  min-height: var(--hit-target-min);
  padding: var(--space-4);
  text-align: left;
  background: var(--c-surface);
  border: 2px solid var(--c-control-border);
  border-radius: var(--radius);
  cursor: pointer;
}

.scope__name {
  font-size: var(--font-size-title);
  font-weight: 700;
}
</style>
