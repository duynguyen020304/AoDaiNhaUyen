import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'

export function GuestRoute() {
  const status = useAuthStore((s) => s.status)
  const user = useAuthStore((s) => s.user)

  if (status === 'loading') return null

  if (status === 'authenticated' && user?.roles.includes('admin')) {
    return <Navigate to="/admin/products" replace />
  }

  return <Outlet />
}
