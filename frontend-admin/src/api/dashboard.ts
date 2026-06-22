import { API_BASE_URL, request } from './client'
import type {
  DashboardSummary,
  RevenueData,
  OrderStatusDistribution,
  RecentOrder,
  TopProduct,
  UserGrowthData,
} from '@/types/dashboard'

export async function getDashboardSummary(): Promise<DashboardSummary> {
  return request<DashboardSummary>('/api/admin/dashboard/summary')
}

export async function getRevenue(period = 30): Promise<RevenueData> {
  return request<RevenueData>(`/api/admin/dashboard/revenue?period=${period}`)
}

export async function downloadDashboardReportPdf(fromDate: string, toDate: string): Promise<void> {
  const params = new URLSearchParams({ fromDate, toDate })
  const response = await fetch(`${API_BASE_URL}/api/admin/dashboard/report.pdf?${params}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('Không thể xuất PDF dashboard.')
  }

  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `bao-cao-dashboard-${fromDate}-${toDate}.pdf`
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

export async function getOrdersByStatus(): Promise<OrderStatusDistribution> {
  return request<OrderStatusDistribution>('/api/admin/dashboard/orders-by-status')
}

export async function getRecentOrders(limit = 10): Promise<RecentOrder[]> {
  return request<RecentOrder[]>(`/api/admin/dashboard/recent-orders?limit=${limit}`)
}

export async function getTopProducts(limit = 5): Promise<TopProduct[]> {
  return request<TopProduct[]>(`/api/admin/dashboard/top-products?limit=${limit}`)
}

export async function getUserGrowth(period = 30): Promise<UserGrowthData> {
  return request<UserGrowthData>(`/api/admin/dashboard/user-growth?period=${period}`)
}
