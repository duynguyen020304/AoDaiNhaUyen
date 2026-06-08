import { create } from 'zustand'
import type {
  DashboardSummary,
  RevenuePoint,
  OrderStatusDistribution,
  RecentOrder,
  TopProduct,
  UserGrowthPoint,
} from '@/types/dashboard'
import * as dashboardApi from '@/api/dashboard'

type Period = 7 | 30 | 90

interface DashboardState {
  summary: DashboardSummary | null
  revenue: RevenuePoint[]
  ordersByStatus: OrderStatusDistribution | null
  recentOrders: RecentOrder[]
  topProducts: TopProduct[]
  userGrowth: UserGrowthPoint[]
  period: Period
  loading: boolean
  error: string | null
  lastFetch: number | null
  cacheDurationMs: number

  fetchSummary: () => Promise<void>
  fetchRevenue: (period?: Period, force?: boolean) => Promise<void>
  fetchOrdersByStatus: () => Promise<void>
  fetchRecentOrders: () => Promise<void>
  fetchTopProducts: () => Promise<void>
  fetchUserGrowth: (period?: Period, force?: boolean) => Promise<void>
  fetchAll: (force?: boolean) => Promise<void>
  setPeriod: (period: Period) => void
}

export const useDashboardStore = create<DashboardState>((set, get) => ({
  summary: null,
  revenue: [],
  ordersByStatus: null,
  recentOrders: [],
  topProducts: [],
  userGrowth: [],
  period: 30,
  loading: false,
  error: null,

  lastFetch: null,
  cacheDurationMs: 30_000,

  fetchSummary: async () => {
    try {
      const summary = await dashboardApi.getDashboardSummary()
      set({ summary, error: null })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải tổng quan.'
      set({ error: message })
    }
  },

  fetchRevenue: async (period?: Period, force = false) => {
    try {
      const { lastFetch, cacheDurationMs, revenue } = get()
      if (!force && revenue.length > 0 && lastFetch && Date.now() - lastFetch < cacheDurationMs) return

      const p = period ?? get().period
      const data = await dashboardApi.getRevenue(p)
      set({ revenue: data.points, error: null })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải dữ liệu doanh thu.'
      set({ error: message })
    }
  },

  fetchOrdersByStatus: async () => {
    try {
      const distribution = await dashboardApi.getOrdersByStatus()
      set({ ordersByStatus: distribution, error: null })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải trạng thái đơn hàng.'
      set({ error: message })
    }
  },

  fetchRecentOrders: async () => {
    try {
      const orders = await dashboardApi.getRecentOrders(10)
      set({ recentOrders: orders, error: null })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải đơn hàng gần đây.'
      set({ error: message })
    }
  },

  fetchTopProducts: async () => {
    try {
      const products = await dashboardApi.getTopProducts(5)
      set({ topProducts: products, error: null })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải sản phẩm bán chạy.'
      set({ error: message })
    }
  },

  fetchUserGrowth: async (period?: Period, force = false) => {
    try {
      const { lastFetch, cacheDurationMs, userGrowth } = get()
      if (!force && userGrowth.length > 0 && lastFetch && Date.now() - lastFetch < cacheDurationMs) return

      const p = period ?? get().period
      const data = await dashboardApi.getUserGrowth(p)
      set({ userGrowth: data.points, error: null })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải dữ liệu người dùng.'
      set({ error: message })
    }
  },

  fetchAll: async (force = false) => {
    const { lastFetch, cacheDurationMs, summary } = get()
    if (!force && summary && lastFetch && Date.now() - lastFetch < cacheDurationMs) return

    set({ loading: true, error: null })
    try {
      const period = get().period
      const [summary, revenueData, ordersByStatus, recentOrders, topProducts, userGrowthData] =
        await Promise.all([
          dashboardApi.getDashboardSummary(),
          dashboardApi.getRevenue(period),
          dashboardApi.getOrdersByStatus(),
          dashboardApi.getRecentOrders(10),
          dashboardApi.getTopProducts(5),
          dashboardApi.getUserGrowth(period),
        ])
      set({
        summary,
        revenue: revenueData.points,
        ordersByStatus,
        recentOrders,
        topProducts,
        userGrowth: userGrowthData.points,
        loading: false,
        error: null,
        lastFetch: Date.now(),
      })
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải dữ liệu dashboard.'
      set({ loading: false, error: message })
    }
  },

  setPeriod: (period: Period) => {
    set({ period })
    get().fetchRevenue(period, true)
    get().fetchUserGrowth(period, true)
  },
}))
