import { queryOptions, useQueries, useQueryClient } from '@tanstack/react-query'
import * as dashboardApi from '@/api/dashboard'
import { queryKeys, type DashboardPeriod } from './queryKeys'

const DASHBOARD_GC_TIME = 30 * 60_000

export const dashboardQueryOptions = {
  summary: () => queryOptions({
    queryKey: queryKeys.dashboard.summary(),
    queryFn: dashboardApi.getDashboardSummary,
    staleTime: 60_000,
    gcTime: DASHBOARD_GC_TIME,
  }),
  revenue: (period: DashboardPeriod) => queryOptions({
    queryKey: queryKeys.dashboard.revenue(period),
    queryFn: () => dashboardApi.getRevenue(period),
    staleTime: 120_000,
    gcTime: DASHBOARD_GC_TIME,
  }),
  ordersByStatus: () => queryOptions({
    queryKey: queryKeys.dashboard.ordersByStatus(),
    queryFn: dashboardApi.getOrdersByStatus,
    staleTime: 60_000,
    gcTime: DASHBOARD_GC_TIME,
  }),
  recentOrders: (limit = 10) => queryOptions({
    queryKey: queryKeys.dashboard.recentOrders(limit),
    queryFn: () => dashboardApi.getRecentOrders(limit),
    staleTime: 30_000,
    gcTime: DASHBOARD_GC_TIME,
  }),
  topProducts: (limit = 5) => queryOptions({
    queryKey: queryKeys.dashboard.topProducts(limit),
    queryFn: () => dashboardApi.getTopProducts(limit),
    staleTime: 120_000,
    gcTime: DASHBOARD_GC_TIME,
  }),
  userGrowth: (period: DashboardPeriod) => queryOptions({
    queryKey: queryKeys.dashboard.userGrowth(period),
    queryFn: () => dashboardApi.getUserGrowth(period),
    staleTime: 120_000,
    gcTime: DASHBOARD_GC_TIME,
  }),
}

export function useDashboardQueries(period: DashboardPeriod) {
  const [summary, revenue, ordersByStatus, recentOrders, topProducts, userGrowth] = useQueries({
    queries: [
      dashboardQueryOptions.summary(),
      dashboardQueryOptions.revenue(period),
      dashboardQueryOptions.ordersByStatus(),
      dashboardQueryOptions.recentOrders(10),
      dashboardQueryOptions.topProducts(5),
      dashboardQueryOptions.userGrowth(period),
    ],
  })

  return {
    summary,
    revenue,
    ordersByStatus,
    recentOrders,
    topProducts,
    userGrowth,
    isPending: [summary, revenue, ordersByStatus, recentOrders, topProducts, userGrowth].some((query) => query.isPending),
    isFetching: [summary, revenue, ordersByStatus, recentOrders, topProducts, userGrowth].some((query) => query.isFetching),
    error: [summary, revenue, ordersByStatus, recentOrders, topProducts, userGrowth].find((query) => query.error)?.error,
  }
}

export function usePrefetchDashboard() {
  const queryClient = useQueryClient()

  return (period: DashboardPeriod) => {
    void queryClient.prefetchQuery(dashboardQueryOptions.summary())
    void queryClient.prefetchQuery(dashboardQueryOptions.revenue(period))
    void queryClient.prefetchQuery(dashboardQueryOptions.ordersByStatus())
    void queryClient.prefetchQuery(dashboardQueryOptions.recentOrders(10))
    void queryClient.prefetchQuery(dashboardQueryOptions.topProducts(5))
    void queryClient.prefetchQuery(dashboardQueryOptions.userGrowth(period))
  }
}
