import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { extractErrorMessage } from '../api/errors'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login({ email, password })
      navigate('/', { replace: true })
    } catch (err) {
      setError(extractErrorMessage(err, 'Connexion impossible.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="auth-card" onSubmit={handleSubmit}>
      <h1>Connexion</h1>

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
          autoComplete="current-password"
        />
      </label>

      {error && <p className="form-error">{error}</p>}

      <button type="submit" disabled={submitting}>
        {submitting ? 'Connexion…' : 'Se connecter'}
      </button>

      <p className="form-switch">
        Pas encore de compte ? <Link to="/register">Créer un compte</Link>
      </p>
    </form>
  )
}
