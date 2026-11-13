import { ref } from 'vue'

import { PreferenceKey, ThemeMode } from '@/config/appConfig'

type ThemeModeValue = (typeof ThemeMode)[keyof typeof ThemeMode]

function readStoredTheme(): ThemeModeValue {
  try {
    const stored = localStorage.getItem(PreferenceKey.theme)
    if (stored === ThemeMode.light || stored === ThemeMode.dark) return stored
  } catch {
    // storage non accessibile: si segue il sistema
  }
  return ThemeMode.system
}

const mode = ref<ThemeModeValue>(readStoredTheme())

function applyTheme(value: ThemeModeValue): void {
  const root = document.documentElement
  if (value === ThemeMode.system) root.removeAttribute('data-theme')
  else root.setAttribute('data-theme', value)
}

/** Tema chiaro/scuro/sistema: preferenza non sensibile, ammessa in localStorage. */
export function useTheme() {
  function setTheme(value: ThemeModeValue): void {
    mode.value = value
    applyTheme(value)
    try {
      if (value === ThemeMode.system) localStorage.removeItem(PreferenceKey.theme)
      else localStorage.setItem(PreferenceKey.theme, value)
    } catch {
      // preferenza non persistita: vale per la sessione
    }
  }

  return { mode, setTheme, applyStoredTheme: () => applyTheme(mode.value) }
}
