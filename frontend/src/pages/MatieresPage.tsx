import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import type { CreateMatiereRequest, Matiere, NiveauScolaire } from '../api/types'

const NIVEAUX: { value: NiveauScolaire; label: string }[] = [
  { value: 'College', label: 'Collège' },
  { value: 'Lycee', label: 'Lycée' },
  { value: 'Superieur', label: 'Supérieur' },
]

const COULEURS = ['#c8ee68', '#71d8a0', '#ffa44f', '#e3ce64', '#63c5ce', '#ff6978']

export function MatieresPage() {
  const [matieres, setMatieres] = useState<Matiere[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [nom, setNom] = useState('')
  const [couleur, setCouleur] = useState(COULEURS[0])
  const [niveau, setNiveau] = useState<NiveauScolaire>('Lycee')
  const [submitting, setSubmitting] = useState(false)

  function load() {
    api
      .get<Matiere[]>('/api/matieres')
      .then((res) => setMatieres(res.data))
      .catch((err) => setError(extractErrorMessage(err, 'Impossible de charger les matières.')))
  }

  useEffect(load, [])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const payload: CreateMatiereRequest = { nom: nom.trim(), couleur, niveau }
      const res = await api.post<Matiere>('/api/matieres', payload)
      setMatieres((prev) => [...(prev ?? []), res.data].sort((a, b) => a.nom.localeCompare(b.nom)))
      setNom('')
    } catch (err) {
      setError(extractErrorMessage(err, 'Création impossible.'))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDelete(id: string) {
    const previous = matieres
    setMatieres((prev) => prev?.filter((m) => m.id !== id) ?? null)
    try {
      await api.delete(`/api/matieres/${id}`)
    } catch (err) {
      setMatieres(previous ?? null)
      setError(extractErrorMessage(err, 'Suppression impossible.'))
    }
  }

  return (
    <section className="matieres-page">
      <header className="dashboard-header">
        <div>
          <h1>Mes matières</h1>
          <p className="muted">Organise tes fiches et quiz par matière.</p>
        </div>
      </header>

      <form className="matiere-form" onSubmit={handleCreate}>
        <input
          value={nom}
          onChange={(e) => setNom(e.target.value)}
          placeholder="Nom de la matière (ex. Mathématiques)"
          required
          minLength={1}
          maxLength={100}
        />

        <select value={niveau} onChange={(e) => setNiveau(e.target.value as NiveauScolaire)}>
          {NIVEAUX.map((n) => (
            <option key={n.value} value={n.value}>
              {n.label}
            </option>
          ))}
        </select>

        <div className="color-picker">
          {COULEURS.map((c) => (
            <button
              key={c}
              type="button"
              className={`swatch ${couleur === c ? 'swatch-selected' : ''}`}
              style={{ background: c }}
              aria-label={`Couleur ${c}`}
              onClick={() => setCouleur(c)}
            />
          ))}
        </div>

        <button type="submit" disabled={submitting}>
          {submitting ? 'Ajout…' : 'Ajouter'}
        </button>
      </form>

      {error && <p className="form-error">{error}</p>}

      {matieres === null ? (
        <p className="page-status">Chargement…</p>
      ) : matieres.length === 0 ? (
        <p className="muted">Aucune matière pour l'instant — ajoute la première ci-dessus.</p>
      ) : (
        <ul className="matiere-list">
          {matieres.map((m) => (
            <li key={m.id} className="matiere-item">
              <span className="matiere-dot" style={{ background: m.couleur }} />
              <span className="matiere-nom">{m.nom}</span>
              <span className="badge">{NIVEAUX.find((n) => n.value === m.niveau)?.label ?? m.niveau}</span>
              <button type="button" className="icon-button" onClick={() => void handleDelete(m.id)} aria-label={`Supprimer ${m.nom}`}>
                ✕
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
