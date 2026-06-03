import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AdminLayout } from '@/components/AdminLayout'
import { ProductListPage } from '@/pages/ProductListPage'
import { ProductFormPage } from '@/pages/ProductFormPage'

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/admin" element={<AdminLayout />}>
          <Route index element={<Navigate to="/admin/products" replace />} />
          <Route path="products" element={<ProductListPage />} />
          <Route path="products/new" element={<ProductFormPage />} />
          <Route path="products/:id/edit" element={<ProductFormPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/admin/products" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
