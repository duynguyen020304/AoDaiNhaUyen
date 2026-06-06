import { request } from './client'
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
