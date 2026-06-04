import { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { useAuthStore } from '@/stores/authStore'
import { AdminRoute } from '@/auth/AdminRoute'
import { GuestRoute } from '@/auth/GuestRoute'
import { AdminLayout } from '@/components/AdminLayout'
import { LoginPage } from '@/pages/LoginPage'
import { ProductListPage } from '@/pages/ProductListPage'
import { ProductFormPage } from '@/pages/ProductFormPage'
import { CategoriesPage } from '@/pages/CategoriesPage'
import { UsersPage } from '@/pages/UsersPage'
import { RolesPage } from '@/pages/RolesPage'

export function App() {
  const status = useAuthStore((s) => s.status)
  const bootstrap = useAuthStore((s) => s.bootstrap)

  useEffect(() => {
    bootstrap()
  }, [bootstrap])

  if (status === 'loading') {
    return (
      <div className="flex items-center justify-center min-h-dvh">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    )
  }

  return (
    <BrowserRouter>
      <Routes>
        {/* Public login */}
        <Route element={<GuestRoute />}>
          <Route path="/login" element={<LoginPage />} />
        </Route>

        {/* Protected admin */}
        <Route element={<AdminRoute />}>
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<Navigate to="/admin/products" replace />} />
            <Route path="products" element={<ProductListPage />} />
            <Route path="products/new" element={<ProductFormPage />} />
            <Route path="products/:id/edit" element={<ProductFormPage />} />
            <Route path="categories" element={<CategoriesPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="roles" element={<RolesPage />} />
          </Route>
        </Route>

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/admin/products" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
