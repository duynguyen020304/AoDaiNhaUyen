import { useCallback, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { RefreshCw } from 'lucide-react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useDashboardQueries } from '@/queries/dashboardQueries'
import { queryKeys } from '@/queries/queryKeys'
import { getSocialAnalytics } from '@/api/social'
import { StatsCardGrid } from '@/components/dashboard/StatsCardGrid'
import { RevenueChart } from '@/components/dashboard/RevenueChart'
import { OrdersByStatusChart } from '@/components/dashboard/OrdersByStatusChart'
import { RecentOrdersTable } from '@/components/dashboard/RecentOrdersTable'
import { TopProductsList } from '@/components/dashboard/TopProductsList'
import { SocialAnalyticsChart } from '@/components/dashboard/SocialAnalyticsChart'
import { UserGrowthChart } from '@/components/dashboard/UserGrowthChart'
import { LowStockAlerts } from '@/components/dashboard/LowStockAlerts'
import { Button } from '@/components/ui/button'

type Period = 7 | 30 | 90

const PERIOD_OPTIONS: { value: Period; label: string }[] = [
  { value: 7, label: '7 ngày' },
  { value: 30, label: '30 ngày' },
  { value: 90, label: '90 ngày' },
]

function toDateInputValue(date: Date) {
  return date.toISOString().slice(0, 10)
}

function getDateRange(period: Period) {
  const end = new Date()
  const start = new Date()
  start.setDate(end.getDate() - period + 1)
  return {
    fromDate: toDateInputValue(start),
    toDate: toDateInputValue(end),
  }
}

export function DashboardPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [period, setPeriod] = useState<Period>(30)
  const dateRange = getDateRange(period)
  const dashboard = useDashboardQueries(period)
  const socialAnalytics = useQuery({
    queryKey: queryKeys.dashboard.socialAnalytics(period),
    queryFn: () => getSocialAnalytics('facebook', dateRange.fromDate, dateRange.toDate),
    staleTime: 120_000,
  })
  const loading = dashboard.isPending
  const refreshing = dashboard.isFetching || socialAnalytics.isFetching
  const error = dashboard.error instanceof Error ? dashboard.error.message : null

  const revenueRef = useRef<HTMLDivElement>(null)
  const ordersRef = useRef<HTMLDivElement>(null)

  const handleRefresh = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.root })
  }, [queryClient])

  const handlePeriodChange = useCallback((p: Period) => {
    setPeriod(p)
  }, [])

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
        <div className="text-destructive text-sm font-medium">{error}</div>
        <Button variant="outline" size="sm" onClick={handleRefresh}>
          <RefreshCw className="size-4 mr-2" />
          Thử lại
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-ink">Tổng quan</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Theo dõi hoạt động kinh doanh của Nhã Uyên
          </p>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex bg-muted rounded-lg p-1">
            {PERIOD_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                onClick={() => handlePeriodChange(opt.value)}
                className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                  period === opt.value
                    ? 'bg-white text-ink shadow-sm'
                    : 'text-muted-foreground hover:text-ink'
                }`}
              >
                {opt.label}
              </button>
            ))}
          </div>
          <Button variant="outline" size="icon" onClick={handleRefresh} disabled={refreshing}>
            <RefreshCw className={`size-4 ${refreshing ? 'animate-spin' : ''}`} />
          </Button>
        </div>
      </div>

      {/* Stats cards */}
      <StatsCardGrid
        summary={dashboard.summary.data ?? null}
        loading={loading}
        onRevenueClick={() => revenueRef.current?.scrollIntoView({ behavior: 'smooth' })}
        onOrdersClick={() => ordersRef.current?.scrollIntoView({ behavior: 'smooth' })}
        onUsersClick={() => navigate('/admin/users')}
        onProductsClick={() => navigate('/admin/products')}
      />

      {/* Low stock alerts */}
      <LowStockAlerts />

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div ref={revenueRef}>
          <RevenueChart data={dashboard.revenue.data?.points ?? []} loading={loading} />
        </div>
        <OrdersByStatusChart data={dashboard.ordersByStatus.data ?? null} loading={loading} />
      </div>

      {/* Social analytics */}
      <SocialAnalyticsChart data={socialAnalytics.data ?? null} loading={socialAnalytics.isPending} />

      {/* Tables row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div ref={ordersRef}>
          <RecentOrdersTable orders={dashboard.recentOrders.data ?? []} loading={loading} />
        </div>
        <TopProductsList products={dashboard.topProducts.data ?? []} loading={loading} />
      </div>

      {/* User growth */}
      <UserGrowthChart data={dashboard.userGrowth.data?.points ?? []} loading={loading} />
    </div>
  )
}
