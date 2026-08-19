import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { extractErrorMessage } from '../api/errors'
import type { NiveauScolaire } from '../api/types'

const NIVEAUX: { value: NiveauScolaire; label: string }[] = [
  { value: 'College', label: 'Collège' },
  { value: 'Lycee', label: 'Lycée' },
  { value: 'Superieur', label: 'Supérieur' },
]

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [pseudo, setPseudo] = useState('')
  const [password, setPassword] = useState('')
  const [level, setLevel] = useState<NiveauScolaire>('Lycee')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await register({ email, password, pseudo, level })
      navigate('/', { replace: true })
    } catch (err) {
      setError(extractErrorMessage(err, 'Inscription impossible.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="auth-card" onSubmit={handleSubmit}>
      <h1>Créer un compte</h1>

      <label>
        Pseudo
        <input value={pseudo} onChange={(e) => setPseudo(e.target.value)} required minLength={2} maxLength={50} />
      </label>

      <label>
        Email
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
      </label>

      <label>
        Mot de passe
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={8}
          autoComplete="new-password"
        />
        <span className="field-hint">8 caractères minimum</span>
      </label>

      <label>
        Niveau scolaire
        <select value={level} onChange={(e) => setLevel(e.target.value as NiveauScolaire)}>
          {NIVEAUX.map((n) => (
            <option key={n.value} value={n.value}>
              {n.label}
            </option>
          ))}
        </select>
      </label>

      {error && <p className="form-error">{error}</p>}

      <button type="submit" disabled={submitting}>
        {submitting ? 'Création…' : 'Créer mon compte'}
      </button>

      <p className="form-switch">
        Déjà inscrit ? <Link to="/login">Se connecter</Link>
      </p>
    </form>
  )
}
