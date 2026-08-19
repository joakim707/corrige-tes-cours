import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useMatieres } from '../api/useMatieres'
import type {
  FicheSummary,
  QuizAnswerDetail,
  QuizPlay,
  QuizResult,
  QuizResultSummary,
} from '../api/types'

type Stage = 'setup' | 'playing' | 'result'

export function QuizPage() {
  const matieres = useMatieres()
  const [fiches, setFiches] = useState<FicheSummary[]>([])
  const [stage, setStage] = useState<Stage>('setup')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [ficheId, setFicheId] = useState('')
  const [sujet, setSujet] = useState('')
  const [matiereId, setMatiereId] = useState('')
  const [nombreQuestions, setNombreQuestions] = useState(5)

  const [quiz, setQuiz] = useState<QuizPlay | null>(null)
  const [answers, setAnswers] = useState<Record<number, string>>({})
  const [locked, setLocked] = useState<Set<number>>(new Set())
  const [result, setResult] = useState<QuizResult | null>(null)

  const [history, setHistory] = useState<QuizResultSummary[] | null>(null)

  useEffect(() => {
    api.get<FicheSummary[]>('/api/fiches').then((res) => setFiches(res.data)).catch(() => setFiches([]))
    loadHistory()
  }, [])

  function loadHistory() {
    api
      .get<QuizResultSummary[]>('/api/quiz/results')
      .then((res) => setHistory(res.data))
      .catch(() => setHistory([]))
  }

  async function handleGenerate(e: FormEvent) {
    e.preventDefault()
    if (!ficheId && sujet.trim().length < 20) {
      setError('Choisis une fiche ou décris un sujet (20 caractères minimum).')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      const res = await api.post<QuizPlay>('/api/quiz/generate', {
        ficheId: ficheId || null,
        sujet: ficheId ? null : sujet,
        matiereId: matiereId || null,
        nombreQuestions,
      })
      setQuiz(res.data)
      setAnswers({})
      setLocked(new Set())
      setResult(null)
      setStage('playing')
    } catch (err) {
      setError(extractErrorMessage(err, "L'IA n'a pas pu générer ce quiz."))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!quiz) return
    setError(null)
    setSubmitting(true)
    try {
      const reponses = quiz.questions.map((q) => ({ questionIndex: q.index, reponse: answers[q.index] ?? '' }))
      const res = await api.post<QuizResult>(`/api/quiz/${quiz.id}/submit`, { reponses })
      setResult(res.data)
      setStage('result')
      loadHistory()
    } catch (err) {
      setError(extractErrorMessage(err, 'Soumission impossible.'))
    } finally {
      setSubmitting(false)
    }
  }

  function reviserErreurs() {
    if (!quiz || !result) return
    const wrongIndices = new Set(result.details.filter((d) => !d.correcte).map((d) => d.questionIndex))
    const nextLocked = new Set(quiz.questions.map((q) => q.index).filter((i) => !wrongIndices.has(i)))
    setLocked(nextLocked)
    setStage('playing')
    setResult(null)
  }

  function restart() {
    setStage('setup')
    setQuiz(null)
    setResult(null)
    setAnswers({})
    setLocked(new Set())
  }

  const detailByIndex = new Map<number, QuizAnswerDetail>((result?.details ?? []).map((d) => [d.questionIndex, d]))

  return (
    <section className="matieres-page">
      <header className="dashboard-header">
        <div>
          <h1>Quiz interactifs</h1>
          <p className="muted">Génère un quiz depuis une fiche et teste-toi.</p>
        </div>
      </header>

      {error && <p className="form-error">{error}</p>}

      {stage === 'setup' && (
        <form className="stacked-form" onSubmit={handleGenerate}>
          <div className="form-row">
            <select value={ficheId} onChange={(e) => setFicheId(e.target.value)}>
              <option value="">— Depuis un sujet libre —</option>
              {fiches.map((f) => (
                <option key={f.id} value={f.id}>
                  {f.titre}
                </option>
              ))}
            </select>

            <select value={matiereId} onChange={(e) => setMatiereId(e.target.value)}>
              <option value="">Matière (optionnel)</option>
              {matieres.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.nom}
                </option>
              ))}
            </select>

            <input
              type="number"
              min={3}
              max={20}
              value={nombreQuestions}
              onChange={(e) => setNombreQuestions(Number(e.target.value))}
              style={{ width: '80px' }}
            />
          </div>

          {!ficheId && (
            <textarea
              value={sujet}
              onChange={(e) => setSujet(e.target.value)}
              placeholder="Décris le sujet ou colle un cours (20 caractères minimum)…"
              rows={5}
            />
          )}

          <button type="submit" disabled={submitting}>
            {submitting ? 'Génération…' : 'Générer le quiz'}
          </button>
        </form>
      )}

      {stage === 'playing' && quiz && (
        <form className="stacked-form" onSubmit={handleSubmit}>
          <h2>{quiz.titre}</h2>
          {quiz.questions.map((q) => (
            <fieldset key={q.index} className="quiz-question" disabled={locked.has(q.index)}>
              <legend>
                {q.index + 1}. {q.enonce}
                {locked.has(q.index) && <span className="badge quiz-locked-badge">déjà correcte</span>}
              </legend>

              {q.type === 'Ouverte' ? (
                <input
                  type="text"
                  value={answers[q.index] ?? ''}
                  onChange={(e) => setAnswers((prev) => ({ ...prev, [q.index]: e.target.value }))}
                  placeholder="Ta réponse…"
                />
              ) : (
                <div className="quiz-options">
                  {q.options.map((opt) => (
                    <label key={opt} className="checkbox-label">
                      <input
                        type="radio"
                        name={`q-${q.index}`}
                        value={opt}
                        checked={answers[q.index] === opt}
                        onChange={() => setAnswers((prev) => ({ ...prev, [q.index]: opt }))}
                      />
                      {opt}
                    </label>
                  ))}
                </div>
              )}
            </fieldset>
          ))}

          <button type="submit" disabled={submitting}>
            {submitting ? 'Correction…' : 'Valider mes réponses'}
          </button>
        </form>
      )}

      {stage === 'result' && quiz && result && (
        <article className="fiche-detail">
          <div className="fiche-detail-header">
            <h2>Score : {result.score}%</h2>
            <div className="form-row">
              {result.details.some((d) => !d.correcte) && (
                <button type="button" className="secondary" onClick={reviserErreurs}>
                  Réviser les erreurs
                </button>
              )}
              <button type="button" className="secondary" onClick={restart}>
                Nouveau quiz
              </button>
            </div>
          </div>

          <ul className="quiz-results-list">
            {quiz.questions.map((q) => {
              const detail = detailByIndex.get(q.index)
              return (
                <li key={q.index} className={detail?.correcte ? 'quiz-result-ok' : 'quiz-result-ko'}>
                  <p>
                    <strong>{q.index + 1}. {q.enonce}</strong>
                  </p>
                  <p className="muted small">Ta réponse : {detail?.reponseUtilisateur || '(vide)'}</p>
                  <p className="small">{detail?.explication}</p>
                </li>
              )
            })}
          </ul>
        </article>
      )}

      <h2 className="section-title">Historique des scores</h2>
      {history === null ? (
        <p className="page-status">Chargement…</p>
      ) : history.length === 0 ? (
        <p className="muted">Aucun quiz réalisé pour l'instant.</p>
      ) : (
        <ul className="history-list">
          {history.map((h) => (
            <li key={h.id} className="history-item">
              <div>
                <p className="history-input">{h.quizTitre}</p>
                <span className="muted small">{new Date(h.passedAt).toLocaleString('fr-FR')}</span>
              </div>
              <span className="badge">{h.score}%</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
