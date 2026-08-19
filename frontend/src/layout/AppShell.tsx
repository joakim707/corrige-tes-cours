import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const LEVEL_LABELS: Record<string, string> = {
  College: 'Collège',
  Lycee: 'Lycée',
  Superieur: 'Supérieur',
}

const NAV_ITEMS = [
  { to: '/', label: 'Accueil', icon: '⌂', end: true },
  { to: '/devoirs', label: 'Devoirs', icon: '✎' },
  { to: '/fiches', label: 'Mes fiches', icon: '▤' },
  { to: '/quiz', label: 'Quiz', icon: '?' },
  { to: '/matieres', label: 'Organisation', icon: '◫' },
]

const PAGE_TITLES: Record<string, string> = {
  '/': 'Accueil',
  '/devoirs': 'Devoirs',
  '/fiches': 'Mes fiches',
  '/quiz': 'Quiz',
  '/matieres': 'Organisation',
}

export function AppShell() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const pageTitle = PAGE_TITLES[location.pathname] ?? 'Accueil'

  return (
    <div className="app">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-icon" />
          <div className="brand-text">
            <strong>Corrige tes cours</strong>
            <span>Coach scolaire IA</span>
          </div>
        </div>

        <div className="nav-label">Navigation</div>
        <nav className="nav">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              <span className="nav-icon">{item.icon}</span>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <button type="button" className="profile-mini" onClick={() => void logout()} title="Se déconnecter">
            <div className="avatar">{user?.pseudo.charAt(0).toUpperCase()}</div>
            <div>
              <div className="profile-name">{user?.pseudo}</div>
              <div className="profile-level">{user ? (LEVEL_LABELS[user.level] ?? user.level) : ''}</div>
            </div>
          </button>
        </div>
      </aside>

      <main className="main">
        <header className="topbar">
          <div className="breadcrumb">
            Tableau de bord / <strong>{pageTitle}</strong>
          </div>
        </header>

        <Outlet />
      </main>
    </div>
  )
}
