import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useMatieres } from '../api/useMatieres'
import { FileImportButton } from '../components/FileImportButton'
import type { Fiche, FicheSummary } from '../api/types'

export function FichesPage() {
  const matieres = useMatieres()
  const [cours, setCours] = useState('')
  const [matiereId, setMatiereId] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selected, setSelected] = useState<Fiche | null>(null)
  const [library, setLibrary] = useState<FicheSummary[] | null>(null)

  function loadLibrary() {
    api
      .get<FicheSummary[]>('/api/fiches')
      .then((res) => setLibrary(res.data))
      .catch(() => setLibrary([]))
  }

  useEffect(loadLibrary, [])

  async function handleGenerate(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const res = await api.post<Fiche>('/api/fiches/generate', { cours, matiereId: matiereId || null })
      setSelected(res.data)
      setCours('')
      loadLibrary()
    } catch (err) {
      setError(extractErrorMessage(err, "L'IA n'a pas pu générer cette fiche."))
    } finally {
      setSubmitting(false)
    }
  }

  async function openFiche(id: string) {
    setError(null)
    try {
      const res = await api.get<Fiche>(`/api/fiches/${id}`)
      setSelected(res.data)
    } catch (err) {
      setError(extractErrorMessage(err, 'Impossible de charger la fiche.'))
    }
  }

  async function handleDelete(id: string) {
    const previous = library
    setLibrary((prev) => prev?.filter((f) => f.id !== id) ?? null)
    if (selected?.id === id) setSelected(null)
    try {
      await api.delete(`/api/fiches/${id}`)
    } catch (err) {
      setLibrary(previous ?? null)
      setError(extractErrorMessage(err, 'Suppression impossible.'))
    }
  }

  async function handleExport(id: string, titre: string) {
    try {
      const res = await api.get(`/api/fiches/${id}/export/pdf`, { responseType: 'blob' })
      const url = URL.createObjectURL(res.data as Blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `${titre}.pdf`
      link.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      setError(extractErrorMessage(err, 'Export PDF impossible.'))
    }
  }

  return (
    <section className="matieres-page">
      <header className="dashboard-header">
        <div>
          <h1>Fiches de révision</h1>
          <p className="muted">Colle ton cours, récupère une fiche structurée.</p>
        </div>
      </header>

      <form className="stacked-form" onSubmit={handleGenerate}>
        <textarea
          value={cours}
          onChange={(e) => setCours(e.target.value)}
          placeholder="Colle ton cours ici (20 caractères minimum)…"
          required
          minLength={20}
          maxLength={20000}
          rows={8}
        />

        <div className="form-row">
          <FileImportButton
            onExtracted={(text) => setCours(text.slice(0, 20000))}
            onError={setError}
          />

          <select value={matiereId} onChange={(e) => setMatiereId(e.target.value)}>
            <option value="">Matière (optionnel)</option>
            {matieres.map((m) => (
              <option key={m.id} value={m.id}>
                {m.nom}
              </option>
            ))}
          </select>

          <button type="submit" disabled={submitting}>
            {submitting ? 'Génération…' : 'Générer la fiche'}
          </button>
        </div>
      </form>

      {error && <p className="form-error">{error}</p>}

      {selected && (
        <article className="fiche-detail">
          <div className="fiche-detail-header">
            <h2>{selected.titre}</h2>
            <button type="button" className="secondary" onClick={() => void handleExport(selected.id, selected.titre)}>
              Export PDF
            </button>
          </div>

          <p>{selected.resume}</p>

          {selected.pointsCles.length > 0 && (
            <>
              <h3>Points clés</h3>
              <ul>
                {selected.pointsCles.map((p) => (
                  <li key={p}>{p}</li>
                ))}
              </ul>
            </>
          )}

          {Object.keys(selected.definitions).length > 0 && (
            <>
              <h3>Définitions</h3>
              <ul>
                {Object.entries(selected.definitions).map(([terme, def]) => (
                  <li key={terme}>
                    <strong>{terme}</strong> — {def}
                  </li>
                ))}
              </ul>
            </>
          )}

          {selected.formules.length > 0 && (
            <>
              <h3>Formules</h3>
              <ul>
                {selected.formules.map((f) => (
                  <li key={f}>{f}</li>
                ))}
              </ul>
            </>
          )}
        </article>
      )}

      <h2 className="section-title">Bibliothèque</h2>
      {library === null ? (
        <p className="page-status">Chargement…</p>
      ) : library.length === 0 ? (
        <p className="muted">Aucune fiche pour l'instant.</p>
      ) : (
        <ul className="history-list">
          {library.map((f) => (
            <li key={f.id} className="history-item">
              <div onClick={() => void openFiche(f.id)}>
                <p className="history-input">{f.titre}</p>
                <span className="muted small">{new Date(f.createdAt).toLocaleString('fr-FR')}</span>
              </div>
              <button type="button" className="icon-button" onClick={() => void handleDelete(f.id)} aria-label={`Supprimer ${f.titre}`}>
                ✕
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
