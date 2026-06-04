import { Navigate, Outlet } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { useAuthStore } from '@/stores/authStore'

export function AdminRoute() {
  const status = useAuthStore((s) => s.status)
  const user = useAuthStore((s) => s.user)

  if (status === 'loading') {
    return (
      <div className="flex items-center justify-center min-h-dvh">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    )
  }

  if (status === 'anonymous' || !user?.roles.includes('admin')) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
