import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import type { AuthResponse } from './types'

const baseURL = import.meta.env.VITE_API_URL ?? 'https://localhost:7148'

export const api = axios.create({
  baseURL,
  // Indispensable pour que le cookie HttpOnly du refresh token circule.
  withCredentials: true,
})

// Le JWT ne vit qu'en mémoire : jamais de localStorage (cf. §8 du doc de conception).
let accessToken: string | null = null
let onAuthLost: (() => void) | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function onAuthenticationLost(handler: () => void) {
  onAuthLost = handler
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean }

// Un seul appel /refresh en vol : les requêtes concurrentes attendent le même résultat.
let refreshPromise: Promise<string> | null = null

function refreshAccessToken(): Promise<string> {
  refreshPromise ??= api
    .post<AuthResponse>('/api/auth/refresh')
    .then((res) => {
      setAccessToken(res.data.accessToken)
      return res.data.accessToken
    })
    .finally(() => {
      refreshPromise = null
    })

  return refreshPromise
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetriableConfig | undefined
    const isRefreshCall = config?.url?.includes('/api/auth/refresh')

    if (error.response?.status !== 401 || !config || config._retried || isRefreshCall) {
      return Promise.reject(error)
    }

    config._retried = true
    try {
      const token = await refreshAccessToken()
      config.headers.Authorization = `Bearer ${token}`
      return api(config)
    } catch (refreshError) {
      setAccessToken(null)
      onAuthLost?.()
      return Promise.reject(refreshError)
    }
  },
)
