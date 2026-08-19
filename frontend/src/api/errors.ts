import { AxiosError } from 'axios'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

/** Traduit une erreur Axios en message affichable, en privilégiant le détail renvoyé par l'API. */
export function extractErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof AxiosError)) return fallback

  const data = error.response?.data as ProblemDetails | undefined
  if (!data) return error.message || fallback

  const validation = data.errors && Object.values(data.errors).flat()
  if (validation?.length) return validation.join(' ')

  return data.detail ?? data.title ?? fallback
}
