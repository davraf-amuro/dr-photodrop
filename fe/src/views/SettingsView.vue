<script setup lang="ts">
import { onMounted, ref } from 'vue'

import BaseButton from '@/components/shared/BaseButton.vue'
import MessageBanner from '@/components/shared/MessageBanner.vue'
import { useStorageInfo } from '@/composables/shared/useStorageInfo'
import { useTheme } from '@/composables/shared/useTheme'
import { ImageConfig, ThemeMode } from '@/config/appConfig'
import { fetchScopes } from '@/services/apiClient'
import {
  clearApiToken,
  getSendOriginal,
  hasApiToken,
  setApiToken,
  setSendOriginal,
} from '@/services/settingsService'
import { requestPersistence } from '@/services/storageService'
import { authRequired, runFlush } from '@/services/syncScheduler'
import { formatBytes } from '@/utils/formatters'

const { mode, setTheme } = useTheme()
const { usage, persisted, refresh: refreshStorage } = useStorageInfo()

const tokenSaved = ref(false)
// Il token digitato vive solo nel campo, mai in un log; dopo il salvataggio il campo viene svuotato
const tokenInput = ref('')
const sendOriginal = ref(false)
const message = ref<{ tone: 'ok' | 'alert' | 'info'; text: string } | null>(null)
const isChecking = ref(false)

onMounted(async () => {
  tokenSaved.value = await hasApiToken()
  sendOriginal.value = await getSendOriginal()
})

async function saveToken(): Promise<void> {
  if (tokenInput.value.trim() === '') {
    message.value = { tone: 'alert', text: 'Inserisci il token prima di salvare.' }
    return
  }
  await setApiToken(tokenInput.value)
  tokenInput.value = ''
  tokenSaved.value = true
  authRequired.value = false
  message.value = { tone: 'ok', text: 'Token salvato su questo telefono.' }
  void runFlush(false, true)
}

async function removeToken(): Promise<void> {
  await clearApiToken()
  tokenSaved.value = false
  message.value = { tone: 'info', text: 'Token rimosso.' }
}

async function checkConnection(): Promise<void> {
  isChecking.value = true
  try {
    const scopes = await fetchScopes()
    message.value = { tone: 'ok', text: `Connessione e token validi (${scopes.length} ambiti).` }
  } catch (error) {
    const text =
      error instanceof Error && error.name === 'UnauthorizedError'
        ? 'Token mancante o non valido.'
        : 'Server non raggiungibile.'
    message.value = { tone: 'alert', text }
  } finally {
    isChecking.value = false
  }
}

async function onSendOriginalChange(event: Event): Promise<void> {
  sendOriginal.value = (event.target as HTMLInputElement).checked
  await setSendOriginal(sendOriginal.value)
}

async function askPersistence(): Promise<void> {
  const granted = await requestPersistence()
  message.value = granted
    ? { tone: 'ok', text: 'Archiviazione protetta: il telefono non cancellera\' la coda da solo.' }
    : { tone: 'info', text: 'Il browser non ha concesso la protezione dello spazio (riprova dopo aver installato l\'app).' }
  await refreshStorage()
}

function onThemeChange(event: Event): void {
  setTheme((event.target as HTMLSelectElement).value as (typeof ThemeMode)[keyof typeof ThemeMode])
}
</script>

<template>
  <section class="view">
    <h1>Impostazioni</h1>

    <MessageBanner v-if="authRequired && !message" tone="caution" title="Serve il token">
      Il server ha rifiutato l'accesso: inserisci il token qui sotto.
    </MessageBanner>
    <MessageBanner v-if="message" :tone="message.tone">{{ message.text }}</MessageBanner>

    <form class="block" @submit.prevent="saveToken">
      <h2>Token di accesso</h2>
      <p class="muted">
        {{ tokenSaved ? 'Un token e\' salvato su questo telefono.' : 'Nessun token salvato.' }}
        Si inserisce una sola volta e non viene mai mostrato.
      </p>
      <label for="token" class="label">Nuovo token</label>
      <input
        id="token"
        v-model="tokenInput"
        class="field-input"
        type="password"
        autocomplete="off"
        autocapitalize="off"
        spellcheck="false"
      />
      <div class="row">
        <BaseButton type="submit" variant="primary">Salva token</BaseButton>
        <BaseButton :disabled="isChecking" @click="checkConnection">Verifica connessione</BaseButton>
        <BaseButton v-if="tokenSaved" variant="danger" @click="removeToken">Rimuovi token</BaseButton>
      </div>
    </form>

    <section class="block">
      <h2>Foto</h2>
      <label class="check">
        <input type="checkbox" :checked="sendOriginal" @change="onSendOriginalChange" />
        <span>Invia originale (senza ridimensionare)</span>
      </label>
      <p class="muted">
        Di norma le foto sono ridotte a {{ ImageConfig.maxLongSidePx }} px sul lato lungo: piu' leggere e veloci.
        Attiva l'originale se il testo piccolo delle etichette non si legge.
      </p>
    </section>

    <section class="block">
      <h2>Spazio</h2>
      <p v-if="usage">Usato: {{ formatBytes(usage.usage) }} su {{ formatBytes(usage.quota) }}.</p>
      <p v-else class="muted">Occupazione non disponibile su questo browser.</p>
      <p>Protezione dalla cancellazione: <strong>{{ persisted ? 'attiva' : 'non attiva' }}</strong></p>
      <BaseButton v-if="!persisted" @click="askPersistence">Proteggi la coda</BaseButton>
    </section>

    <section class="block">
      <h2>Aspetto</h2>
      <label for="theme" class="label">Tema</label>
      <select id="theme" class="field-input" :value="mode" @change="onThemeChange">
        <option :value="ThemeMode.system">Come il telefono</option>
        <option :value="ThemeMode.light">Chiaro</option>
        <option :value="ThemeMode.dark">Scuro</option>
      </select>
    </section>
  </section>
</template>

<style scoped>
.view {
  display: grid;
  gap: var(--space-4);
}

.block {
  display: grid;
  gap: var(--space-3);
  background: var(--c-surface);
  border: 1px solid var(--c-panel-line);
  border-radius: var(--radius);
  padding: var(--space-4);
}

.label {
  font-weight: 600;
}

.row {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.check {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  min-height: var(--hit-target-min);
  font-weight: 600;
}

.check input {
  width: 1.5rem;
  height: 1.5rem;
}
</style>
