import { describe, expect, it } from 'vitest'
import { AxiosError } from 'axios'
import { extractErrorMessage } from './errors'

function buildAxiosError(data: unknown): AxiosError {
  const error = new AxiosError('Request failed')
  error.response = { data, status: 400, statusText: '', headers: {}, config: {} as never }
  return error
}

describe('extractErrorMessage', () => {
  it("retourne le message par défaut si l'erreur n'est pas une AxiosError", () => {
    expect(extractErrorMessage(new Error('autre chose'), 'défaut')).toBe('défaut')
  })

  it('priorise les erreurs de validation sur detail/title', () => {
    const error = buildAxiosError({
      title: 'Erreur de validation',
      errors: { Email: ['Email invalide'], Password: ['8 caractères minimum'] },
    })

    expect(extractErrorMessage(error, 'défaut')).toBe('Email invalide 8 caractères minimum')
  })

  it("utilise 'detail' si présent et pas d'erreurs de validation", () => {
    const error = buildAxiosError({ title: 'Conflit', detail: 'Email déjà utilisé.' })

    expect(extractErrorMessage(error, 'défaut')).toBe('Email déjà utilisé.')
  })

  it("retombe sur 'title' si 'detail' est absent", () => {
    const error = buildAxiosError({ title: 'Connexion refusée' })

    expect(extractErrorMessage(error, 'défaut')).toBe('Connexion refusée')
  })

  it("retombe sur le message Axios si la réponse n'a pas de corps exploitable", () => {
    const error = buildAxiosError(undefined)

    expect(extractErrorMessage(error, 'défaut')).toBe(error.message)
  })
})
