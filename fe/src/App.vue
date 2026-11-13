<script setup lang="ts">
import { computed, ref } from 'vue'

import BaseButton from '@/components/shared/BaseButton.vue'
import MessageBanner from '@/components/shared/MessageBanner.vue'
import { useCaptureSession } from '@/composables/capture/useCaptureSession'
import { useQueue } from '@/composables/queue/useQueue'
import { useOnline } from '@/composables/shared/useOnline'
import { authRequired } from '@/services/syncScheduler'
import CaptureView from '@/views/CaptureView.vue'
import QueueView from '@/views/QueueView.vue'
import ScopeView from '@/views/ScopeView.vue'
import SettingsView from '@/views/SettingsView.vue'

type Tab = 'capture' | 'queue' | 'settings'

const tab = ref<Tab>('capture')
const { scope } = useCaptureSession()
const { pendingCount } = useQueue()
const { isOnline } = useOnline()

const tabs = computed<{ id: Tab; label: string }[]>(() => [
  { id: 'capture', label: 'Scatta' },
  { id: 'queue', label: pendingCount.value > 0 ? `Coda (${pendingCount.value})` : 'Coda' },
  { id: 'settings', label: 'Impostazioni' },
])
</script>

<template>
  <div class="app">
    <main class="app__main">
      <MessageBanner v-if="!isOnline" tone="caution">Offline: puoi scattare, le foto vanno in coda.</MessageBanner>
      <MessageBanner v-if="authRequired && tab !== 'settings'" tone="caution" title="Serve il token">
        <p>Il server ha rifiutato l'accesso: le foto restano in coda.</p>
        <BaseButton variant="primary" @click="tab = 'settings'">Inserisci il token</BaseButton>
      </MessageBanner>

      <template v-if="tab === 'capture'">
        <CaptureView v-if="scope" @queued="tab = 'queue'" />
        <ScopeView v-else @go-settings="tab = 'settings'" />
      </template>
      <QueueView v-else-if="tab === 'queue'" />
      <SettingsView v-else />
    </main>

    <nav class="nav" aria-label="Sezioni">
      <button
        v-for="item in tabs"
        :key="item.id"
        type="button"
        class="nav__item"
        :class="{ 'nav__item--active': tab === item.id }"
        :aria-current="tab === item.id ? 'page' : undefined"
        @click="tab = item.id"
      >
        {{ item.label }}
      </button>
    </nav>
  </div>
</template>

<style scoped>
.app {
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
}

.app__main {
  flex: 1;
  display: grid;
  gap: var(--space-4);
  align-content: start;
  width: 100%;
  max-width: 40rem;
  margin: 0 auto;
  padding: var(--space-4) var(--space-4) calc(var(--nav-height) + var(--space-5));
}

/* Navigazione in basso: raggiungibile con il pollice */
.nav {
  position: fixed;
  inset: auto 0 0 0;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  height: var(--nav-height);
  padding-bottom: env(safe-area-inset-bottom);
  background: var(--c-surface);
  border-top: 1px solid var(--c-panel-line);
}

.nav__item {
  min-height: var(--hit-target-min);
  background: transparent;
  border: 0;
  border-top: 4px solid transparent;
  color: var(--c-ink);
  font-weight: 600;
  cursor: pointer;
}

/* Stato attivo: bordo + testo in colore primario + sottolineatura, non solo colore */
.nav__item--active {
  border-top-color: var(--c-federal);
  color: var(--c-federal);
  text-decoration: underline;
}
</style>
