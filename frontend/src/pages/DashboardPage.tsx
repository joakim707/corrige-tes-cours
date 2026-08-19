import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { api } from '../api/client'
import type { DashboardStats } from '../api/types'

const MODULES = [
  {
    key: 'homework',
    titre: 'Aide aux devoirs',
    desc: "Soumets un exercice, obtiens des indices avant la correction — l'IA ne donne jamais la réponse directement.",
    icon: '✎',
    to: '/devoirs',
  },
  {
    key: 'sheets',
    titre: 'Fiches de révision',
    desc: 'Colle ton cours ou importe un fichier, récupère une fiche structurée : résumé, points clés, définitions.',
    icon: '▤',
    to: '/fiches',
  },
  {
    key: 'quiz',
    titre: 'Quiz interactifs',
    desc: 'Génère un quiz depuis une fiche et teste-toi, avec un mode révision sur tes questions ratées.',
    icon: '?',
    to: '/quiz',
  },
  {
    key: 'plan',
    titre: 'Organisation',
    desc: 'Gère tes matières et retrouve tes fiches, corrections et quiz classés par sujet.',
    icon: '◫',
    to: '/matieres',
  },
]

export function DashboardPage() {
  const { user } = useAuth()
  const [stats, setStats] = useState<DashboardStats | null>(null)

  useEffect(() => {
    api
      .get<DashboardStats>('/api/dashboard/stats')
      .then((res) => setStats(res.data))
      .catch(() => setStats(null))
  }, [])

  const scoreValue = stats?.scoreMoyen !== null && stats?.scoreMoyen !== undefined ? Math.round(stats.scoreMoyen) : null

  return (
    <>
      <section className="hero">
        <div className="hero-content">
          <div className="hero-kicker">✦ Session du jour</div>

          <h1>
            Bon retour, {user?.pseudo}.<br />
            On <span>comprend</span> quoi aujourd'hui ?
          </h1>

          <p>
            Ton coach ne fait pas les exercices à ta place : il analyse ton raisonnement, repère ce qui bloque et
            t'aide à trouver toi-même la prochaine étape.
          </p>

          <div className="hero-actions">
            <Link to="/devoirs" className="btn btn-primary">
              <span>✎</span>
              <span>Commencer un devoir</span>
            </Link>
            <Link to="/fiches" className="btn btn-secondary">
              <span>▤</span>
              <span>Voir mes fiches</span>
            </Link>
          </div>
        </div>
      </section>

      <div className="stats-grid">
        <article className="stat-card" style={{ ['--stat-accent' as string]: '#72d9a3' }}>
          <div className="stat-label">Matières suivies</div>
          <div className="stat-value">{stats ? String(stats.matieresCount).padStart(2, '0') : '—'}</div>
        </article>

        <article className="stat-card" style={{ ['--stat-accent' as string]: '#ffa451' }}>
          <div className="stat-label">Fiches créées</div>
          <div className="stat-value">{stats ? String(stats.fichesCount).padStart(2, '0') : '—'}</div>
        </article>

        <article className="stat-card" style={{ ['--stat-accent' as string]: '#e4d066' }}>
          <div className="stat-label">Quiz terminés</div>
          <div className="stat-value">{stats ? String(stats.quizCount).padStart(2, '0') : '—'}</div>
        </article>

        <article className="stat-card" style={{ ['--stat-accent' as string]: '#64c7ce' }}>
          <div className="stat-label">Score moyen</div>
          <div className="stat-value">{scoreValue !== null ? `${scoreValue}%` : '—'}</div>
        </article>
      </div>

      <div className="section-heading">
        <div className="section-title-wrap">
          <div className="section-bar" style={{ ['--section-color' as string]: '#ff9f4d' }} />
          <div>
            <h2>Mes outils de travail</h2>
            <p>Choisis ton mode d'apprentissage.</p>
          </div>
        </div>
      </div>

      <section className="modules-grid">
        {MODULES.map((m) => (
          <Link key={m.key} to={m.to} className={`module-card module-${m.key}`}>
            <div className="module-top">
              <div className="module-icon">{m.icon}</div>
            </div>

            <h3>{m.titre}</h3>
            <p className="module-description">{m.desc}</p>

            <div className="module-footer">
              <div className="module-arrow">→</div>
            </div>
          </Link>
        ))}
      </section>

      {scoreValue !== null && (
        <section className="panel score-panel">
          <header className="panel-header">
            <div className="panel-title">
              <span className="panel-title-marker" style={{ background: '#d8eb71' }} />
              Ma progression
            </div>
          </header>

          <div className="score-body">
            <div className="ring" style={{ ['--value' as string]: String(scoreValue), ['--ring-color' as string]: '#c8ee68' }}>
              <div className="ring-content">
                <span className="ring-value">{scoreValue}%</span>
                <span className="ring-caption">score moyen</span>
              </div>
            </div>

            <p className="score-text">
              Calculé sur <strong>{stats?.quizCount ?? 0}</strong> quiz réalisé{(stats?.quizCount ?? 0) > 1 ? 's' : ''}.
            </p>
          </div>
        </section>
      )}

      <aside className="coach-note">
        <div className="coach-icon">💡</div>
        <div className="coach-copy">
          <strong>Ici, l'IA n'est pas là pour tricher.</strong>
          <p>
            Corrige tes cours te donne des indices, questionne ton raisonnement et adapte ses explications — mais la
            réponse finale reste la tienne.
          </p>
        </div>
      </aside>
    </>
  )
}
