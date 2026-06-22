import { useCallback, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Download, RefreshCw } from 'lucide-react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useDashboardQueries } from '@/queries/dashboardQueries'
import { queryKeys } from '@/queries/queryKeys'
import { getSocialAnalytics } from '@/api/social'
import { downloadDashboardReportPdf } from '@/api/dashboard'
import { StatsCardGrid } from '@/components/dashboard/StatsCardGrid'
import { RevenueChart } from '@/components/dashboard/RevenueChart'
import { OrdersByStatusChart } from '@/components/dashboard/OrdersByStatusChart'
import { RecentOrdersTable } from '@/components/dashboard/RecentOrdersTable'
import { TopProductsList } from '@/components/dashboard/TopProductsList'
import { SocialAnalyticsChart } from '@/components/dashboard/SocialAnalyticsChart'
import { UserGrowthChart } from '@/components/dashboard/UserGrowthChart'
import { LowStockAlerts } from '@/components/dashboard/LowStockAlerts'
import { Button } from '@/components/ui/button'

type Period = 1 | 7 | 30 | 90

const PERIOD_OPTIONS: { value: Period; label: string }[] = [
  { value: 1, label: 'Hôm nay' },
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
  const [period, setPeriod] = useState<Period>(7)
  const dateRange = getDateRange(period)
  const [reportFromDate, setReportFromDate] = useState(dateRange.fromDate)
  const [reportToDate, setReportToDate] = useState(dateRange.toDate)
  const [exportingPdf, setExportingPdf] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)
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
    const nextRange = getDateRange(p)
    setReportFromDate(nextRange.fromDate)
    setReportToDate(nextRange.toDate)
  }, [])

  const handleExportPdf = useCallback(async () => {
    setExportError(null)
    setExportingPdf(true)
    try {
      await downloadDashboardReportPdf(reportFromDate, reportToDate)
    } catch (err) {
      setExportError(err instanceof Error ? err.message : 'Không thể xuất PDF dashboard.')
    } finally {
      setExportingPdf(false)
    }
  }, [reportFromDate, reportToDate])

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
          <div className="hidden items-center gap-2 rounded-lg border border-border bg-white px-2 py-1.5 md:flex">
            <input
              type="date"
              value={reportFromDate}
              onChange={(event) => setReportFromDate(event.target.value)}
              className="w-32 bg-transparent text-xs text-ink outline-none"
              aria-label="Từ ngày xuất báo cáo"
            />
            <span className="text-xs text-muted-foreground">→</span>
            <input
              type="date"
              value={reportToDate}
              onChange={(event) => setReportToDate(event.target.value)}
              className="w-32 bg-transparent text-xs text-ink outline-none"
              aria-label="Đến ngày xuất báo cáo"
            />
          </div>
          <Button variant="outline" size="sm" onClick={handleExportPdf} disabled={exportingPdf}>
            {exportingPdf ? <RefreshCw className="size-4 animate-spin" /> : <Download className="size-4" />}
            Xuất PDF
          </Button>
          <Button variant="outline" size="icon" onClick={handleRefresh} disabled={refreshing}>
            <RefreshCw className={`size-4 ${refreshing ? 'animate-spin' : ''}`} />
          </Button>
        </div>
      </div>

      {exportError && (
        <div className="rounded-lg border border-destructive/25 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {exportError}
        </div>
      )}

      <div className="flex items-center gap-2 rounded-lg border border-border bg-white px-3 py-2 md:hidden">
        <input
          type="date"
          value={reportFromDate}
          onChange={(event) => setReportFromDate(event.target.value)}
          className="min-w-0 flex-1 bg-transparent text-xs text-ink outline-none"
          aria-label="Từ ngày xuất báo cáo"
        />
        <span className="text-xs text-muted-foreground">→</span>
        <input
          type="date"
          value={reportToDate}
          onChange={(event) => setReportToDate(event.target.value)}
          className="min-w-0 flex-1 bg-transparent text-xs text-ink outline-none"
          aria-label="Đến ngày xuất báo cáo"
        />
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
