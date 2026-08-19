import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, onAuthenticationLost, setAccessToken } from '../api/client'
import type { AuthResponse, LoginRequest, RegisterRequest, User } from '../api/types'

interface AuthContextValue {
  user: User | null
  /** true tant que la tentative de restauration de session au démarrage n'a pas abouti. */
  loading: boolean
  login: (payload: LoginRequest) => Promise<void>
  register: (payload: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  const applyAuth = useCallback((data: AuthResponse) => {
    setAccessToken(data.accessToken)
    setUser(data.user)
  }, [])

  // Au chargement, le cookie de refresh permet de retrouver la session sans re-login.
  useEffect(() => {
    let cancelled = false

    api
      .post<AuthResponse>('/api/auth/refresh')
      .then((res) => {
        if (!cancelled) applyAuth(res.data)
      })
      .catch(() => {
        if (!cancelled) setUser(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [applyAuth])

  useEffect(() => {
    onAuthenticationLost(() => setUser(null))
  }, [])

  const login = useCallback(
    async (payload: LoginRequest) => {
      const res = await api.post<AuthResponse>('/api/auth/login', payload)
      applyAuth(res.data)
    },
    [applyAuth],
  )

  const register = useCallback(
    async (payload: RegisterRequest) => {
      const res = await api.post<AuthResponse>('/api/auth/register', payload)
      applyAuth(res.data)
    },
    [applyAuth],
  )

  const logout = useCallback(async () => {
    try {
      await api.post('/api/auth/logout')
    } finally {
      setAccessToken(null)
      setUser(null)
    }
  }, [])

  const value = useMemo(
    () => ({ user, loading, login, register, logout }),
    [user, loading, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth doit être utilisé à l\'intérieur d\'un <AuthProvider>')
  return ctx
}
