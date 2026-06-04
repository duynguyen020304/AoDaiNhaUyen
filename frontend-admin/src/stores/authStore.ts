import { create } from 'zustand'
import type { AuthUser, AuthStatus } from '@/types/auth'
import * as authApi from '@/api/auth'

interface AuthState {
  status: AuthStatus
  user: AuthUser | null
  error: string | null
  bootstrap: () => Promise<void>
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const useAuthStore = create<AuthState>((set) => ({
  status: 'loading',
  user: null,
  error: null,

  bootstrap: async () => {
    try {
      const user = await authApi.getCurrentUser()
      set({ status: 'authenticated', user, error: null })
    } catch {
      try {
        const user = await authApi.refreshSession()
        set({ status: 'authenticated', user, error: null })
      } catch {
        set({ status: 'anonymous', user: null, error: null })
      }
    }
  },

  login: async (email: string, password: string) => {
    set({ error: null })
    try {
      const user = await authApi.login(email, password)
      set({ status: 'authenticated', user, error: null })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Đăng nhập thất bại.'
      set({ error: message })
      throw err
    }
  },

  logout: async () => {
    try {
      await authApi.logout()
    } finally {
      set({ status: 'anonymous', user: null, error: null })
    }
  },
}))
