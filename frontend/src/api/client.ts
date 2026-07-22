import axios from 'axios'
import toast from 'react-hot-toast'
import { appConfig } from '../app/config'
import { useAuthStore } from '../stores/auth.store'
import type { ApiResponse, AuthSession } from './types'

export const apiClient = axios.create({
  baseURL: appConfig.apiUrl,
  timeout: 3000,
  headers: {
    'Content-Type': 'application/json',
  },
})

let refreshPromise: Promise<AuthSession> | null = null

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status
    const originalRequest = error.config

    if (status === 401 && originalRequest && !originalRequest._retry) {
      const refreshToken = useAuthStore.getState().refreshToken

      if (!refreshToken) {
        useAuthStore.getState().clearSession()
        return Promise.reject(error)
      }

      originalRequest._retry = true
      refreshPromise ??= refreshSession(refreshToken).finally(() => {
        refreshPromise = null
      })

      try {
        const session = await refreshPromise
        useAuthStore.getState().setSession(session)
        originalRequest.headers.Authorization = `Bearer ${session.accessToken}`
        return apiClient(originalRequest)
      } catch (refreshError) {
        useAuthStore.getState().clearSession()
        toast.error('Oturum suresi doldu. Lutfen tekrar giris yapin.')
        return Promise.reject(refreshError)
      }
    }

    if (status === 403) {
      toast.error('Bu islem icin yetkiniz yok.')
    }

    if (status === 423) {
      toast.error('Hesap kilitli. Lutfen kalan sureyi bekleyin.')
    }

    if (status === 429) {
      toast.error('Cok fazla deneme. Lutfen biraz bekleyin.')
    }

    return Promise.reject(error)
  },
)

async function refreshSession(refreshToken: string) {
  const response = await axios.post<ApiResponse<AuthSession>>(
    `${appConfig.apiUrl}/auth/refresh`,
    { refreshToken },
    { timeout: 3000 },
  )

  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.error?.message ?? 'Refresh token yenilenemedi.')
  }

  return response.data.data
}
