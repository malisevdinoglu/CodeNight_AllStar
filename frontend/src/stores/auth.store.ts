import { create } from 'zustand'
import type { AuthSession, AuthUser, UserRole } from '../api/types'

type AuthState = {
  accessToken: string | null
  refreshToken: string | null
  user: AuthUser | null
  role: UserRole | null
  setSession: (session: AuthSession) => void
  clearSession: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  refreshToken: null,
  user: null,
  role: null,
  setSession: (session) =>
    set({
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      user: session.user,
      role: session.user.role,
    }),
  clearSession: () =>
    set({
      accessToken: null,
      refreshToken: null,
      user: null,
      role: null,
    }),
}))
