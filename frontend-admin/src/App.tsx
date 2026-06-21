import { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { useAuthStore } from '@/stores/authStore'
import { AdminRoute } from '@/auth/AdminRoute'
import { GuestRoute } from '@/auth/GuestRoute'
import { AdminLayout } from '@/components/AdminLayout'
import { LoginPage } from '@/pages/LoginPage'
import { DashboardPage } from '@/pages/DashboardPage'
import { ProductListPage } from '@/pages/ProductListPage'
import { ProductFormPage } from '@/pages/ProductFormPage'
import { CategoriesPage } from '@/pages/CategoriesPage'
import { CollectionsPage } from '@/pages/CollectionsPage'
import { UsersPage } from '@/pages/UsersPage'
import { RolesPage } from '@/pages/RolesPage'
import { MediaPage } from '@/pages/MediaPage'
import { HermesPage } from '@/pages/HermesPage'
import { OrdersPage } from '@/pages/OrdersPage'
import { ReviewsPage } from '@/pages/ReviewsPage'
import { ToolRiskPage } from '@/pages/ToolRiskPage'
import { PromosPage } from '@/pages/PromosPage'
import { BlogListPage } from '@/pages/BlogListPage'
import { BlogFormPage } from '@/pages/BlogFormPage'
import { AiChatPage } from '@/pages/AiChatPage'
import { AiTryOnFeedbackPage } from '@/pages/AiTryOnFeedbackPage'
import { MarketingDashboardPage } from '@/pages/MarketingDashboardPage'
import { EmailTemplatesPage } from '@/pages/EmailTemplatesPage'
import { SubscribersPage } from '@/pages/SubscribersPage'
import { EmailQueuePage } from '@/pages/EmailQueuePage'
import { HermesMonitorPage } from '@/pages/HermesMonitorPage'
import { FacebookPage } from '@/pages/FacebookPage'
import { FacebookPostEditPage } from '@/pages/FacebookPostEditPage'

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

        {/* Public signed Hermes monitor */}
        <Route path="/hermes-monitor/:token" element={<HermesMonitorPage />} />

        {/* Protected admin */}
        <Route element={<AdminRoute />}>
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<Navigate to="dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="products" element={<ProductListPage />} />
            <Route path="products/new" element={<ProductFormPage />} />
            <Route path="products/:id" element={<ProductFormPage />} />
            <Route path="products/:id/edit" element={<ProductFormPage />} />
            <Route path="categories" element={<CategoriesPage />} />
            <Route path="collections" element={<CollectionsPage />} />
            <Route path="promos" element={<PromosPage />} />
            <Route path="blog" element={<BlogListPage />} />
            <Route path="blog/new" element={<BlogFormPage />} />
            <Route path="blog/:id/edit" element={<BlogFormPage />} />
            <Route path="marketing" element={<MarketingDashboardPage />} />
            <Route path="email-templates" element={<EmailTemplatesPage />} />
            <Route path="subscribers" element={<SubscribersPage />} />
            <Route path="facebook" element={<FacebookPage />} />
            <Route path="facebook/posts/:postId/edit" element={<FacebookPostEditPage />} />
            <Route path="email-queue" element={<EmailQueuePage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="roles" element={<RolesPage />} />
            <Route path="media" element={<MediaPage />} />
            <Route path="orders" element={<OrdersPage />} />
            <Route path="reviews" element={<ReviewsPage />} />
            <Route path="ai-tryon-feedback" element={<AiTryOnFeedbackPage />} />
            <Route path="ai-chat" element={<AiChatPage />} />
            <Route path="ai-chat/:chatId" element={<AiChatPage />} />
            <Route path="hermes" element={<HermesPage />} />
            <Route path="hermes/:chatId" element={<HermesPage />} />
            <Route path="tools-risk" element={<ToolRiskPage />} />
          </Route>
        </Route>

        {/* Fallback */}
        <Route path="*" element={<Navigate to="/admin" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
