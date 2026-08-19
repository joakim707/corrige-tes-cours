import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useMatieres } from '../api/useMatieres'
import { FileImportButton } from '../components/FileImportButton'
import type { Correction } from '../api/types'

export function DevoirsPage() {
  const matieres = useMatieres()
  const [exercice, setExercice] = useState('')
  const [matiereId, setMatiereId] = useState('')
  const [correctionComplete, setCorrectionComplete] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [current, setCurrent] = useState<Correction | null>(null)
  const [history, setHistory] = useState<Correction[] | null>(null)

  function loadHistory() {
    api
      .get<Correction[]>('/api/corrections')
      .then((res) => setHistory(res.data))
      .catch(() => setHistory([]))
  }

  useEffect(loadHistory, [])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const res = await api.post<Correction>('/api/corrections', {
        exercice,
        matiereId: matiereId || null,
        demanderCorrectionComplete: correctionComplete,
      })
      setCurrent(res.data)
      setExercice('')
      loadHistory()
    } catch (err) {
      setError(extractErrorMessage(err, "L'IA n'a pas pu traiter cet exercice."))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="matieres-page">
      <header className="dashboard-header">
        <div>
          <h1>Aide aux devoirs</h1>
          <p className="muted">Soumets un exercice, obtiens des indices avant la correction.</p>
        </div>
      </header>

      <form className="stacked-form" onSubmit={handleSubmit}>
        <textarea
          value={exercice}
          onChange={(e) => setExercice(e.target.value)}
          placeholder="Colle ou tape ton exercice ici…"
          required
          minLength={3}
          maxLength={8000}
          rows={6}
        />

        <div className="form-row">
          <FileImportButton
            onExtracted={(text) => setExercice(text.slice(0, 8000))}
            onError={setError}
          />

          <select value={matiereId} onChange={(e) => setMatiereId(e.target.value)}>
            <option value="">Matière (détection auto si vide)</option>
            {matieres.map((m) => (
              <option key={m.id} value={m.id}>
                {m.nom}
              </option>
            ))}
          </select>

          <label className="checkbox-label">
            <input type="checkbox" checked={correctionComplete} onChange={(e) => setCorrectionComplete(e.target.checked)} />
            Correction complète directe
          </label>

          <button type="submit" disabled={submitting}>
            {submitting ? 'Réflexion…' : 'Envoyer'}
          </button>
        </div>
      </form>

      {error && <p className="form-error">{error}</p>}

      {current && (
        <article className="ai-response">
          <h2>Réponse</h2>
          <p className="ai-response-text">{current.contenuIA}</p>
        </article>
      )}

      <h2 className="section-title">Historique</h2>
      {history === null ? (
        <p className="page-status">Chargement…</p>
      ) : history.length === 0 ? (
        <p className="muted">Aucune correction pour l'instant.</p>
      ) : (
        <ul className="history-list">
          {history.map((c) => (
            <li key={c.id} className="history-item" onClick={() => setCurrent(c)}>
              <p className="history-input">{c.contenuInput.slice(0, 120)}{c.contenuInput.length > 120 ? '…' : ''}</p>
              <span className="muted small">{new Date(c.createdAt).toLocaleString('fr-FR')}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
