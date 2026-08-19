import { useEffect, useState } from 'react'
import { api } from './client'
import type { Matiere } from './types'

/** Charge la liste des matières de l'utilisateur, pour peupler les sélecteurs. */
export function useMatieres() {
  const [matieres, setMatieres] = useState<Matiere[]>([])

  useEffect(() => {
    api
      .get<Matiere[]>('/api/matieres')
      .then((res) => setMatieres(res.data))
      .catch(() => setMatieres([]))
  }, [])

  return matieres
}
