import { request } from './client'
import type { AuthUser } from '@/types/auth'

export async function getCurrentUser(): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/me')
}

export async function login(email: string, password: string): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}

export async function logout(): Promise<void> {
  await request<void>('/api/auth/logout', { method: 'POST' })
}

export async function refreshSession(): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/refresh', { method: 'POST' })
}
