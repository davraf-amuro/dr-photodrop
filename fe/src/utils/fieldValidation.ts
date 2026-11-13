import type { FieldValues, ScopeField } from '@/types/contract'

/** Valori dei campi nel form: sempre stringhe (come negli <input>). */
export type FormValues = Record<string, string>

export interface FieldValidationResult {
  /** Messaggio per chiave di campo; vuoto se tutto e' valido. */
  errors: Record<string, string>
  /** Valori pronti per la parte `fields` (number come numero JSON); campi vuoti omessi. */
  values: FieldValues
}

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

/** Valori iniziali del form: `default` dell'ambito; per le date `today` non esiste, resta vuoto. */
export function buildInitialValues(fields: ScopeField[]): FormValues {
  const values: FormValues = {}
  for (const field of fields) values[field.key] = field.default ?? ''
  return values
}

/**
 * Validazione lato client prima dell'invio, speculare alle regole del server:
 * required, tipo (text|number|date), `min` per i numeri, `maxLength` per i testi.
 */
export function validateFields(fields: ScopeField[], input: FormValues): FieldValidationResult {
  const errors: Record<string, string> = {}
  const values: FieldValues = {}

  for (const field of fields) {
    const raw = (input[field.key] ?? '').trim()

    if (raw === '') {
      if (field.required) errors[field.key] = `${field.label}: campo obbligatorio`
      continue
    }

    switch (field.type) {
      case 'number': {
        const parsed = Number(raw.replace(',', '.'))
        if (!Number.isFinite(parsed)) {
          errors[field.key] = `${field.label}: inserisci un numero`
        } else if (field.min !== null && parsed < field.min) {
          errors[field.key] = `${field.label}: minimo ${field.min}`
        } else {
          values[field.key] = parsed
        }
        break
      }
      case 'date': {
        if (!DATE_PATTERN.test(raw) || Number.isNaN(Date.parse(raw))) {
          errors[field.key] = `${field.label}: data non valida`
        } else {
          values[field.key] = raw
        }
        break
      }
      default: {
        if (field.maxLength !== null && raw.length > field.maxLength) {
          errors[field.key] = `${field.label}: massimo ${field.maxLength} caratteri`
        } else {
          values[field.key] = raw
        }
      }
    }
  }

  return { errors, values }
}
