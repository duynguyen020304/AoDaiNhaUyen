import { StatsCard } from './StatsCard'
import type { DashboardSummary } from '@/types/dashboard'
import { DollarSign, ShoppingBag, Users, Package } from 'lucide-react'

interface StatsCardGridProps {
  summary: DashboardSummary | null
  loading?: boolean
  onRevenueClick?: () => void
  onOrdersClick?: () => void
  onUsersClick?: () => void
  onProductsClick?: () => void
}

function formatCurrency(n: number): string {
  if (n >= 1_000_000_000) return `${(n / 1_000_000_000).toFixed(1)} tỷ ₫`
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}tr ₫`
  return `${n.toLocaleString()} ₫`
}

function formatCount(n: number): string {
  return n.toLocaleString('vi-VN')
}

export function StatsCardGrid({
  summary,
  loading,
  onRevenueClick,
  onOrdersClick,
  onUsersClick,
  onProductsClick,
}: StatsCardGridProps) {
  if (loading || !summary) {
    return (
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="bg-white rounded-xl border border-border p-5 animate-pulse">
            <div className="flex items-center justify-between">
              <div className="h-4 bg-muted rounded w-20" />
              <div className="size-10 rounded-lg bg-muted" />
            </div>
            <div className="mt-3 space-y-2">
              <div className="h-7 bg-muted rounded w-28" />
              <div className="h-3 bg-muted rounded w-16" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <StatsCard
        icon={DollarSign}
        label="Doanh thu"
        value={formatCurrency(summary.totalRevenue)}
        growth={summary.revenueGrowth}
        growthLabel="so với 30 ngày trước"
        onClick={onRevenueClick}
      />
      <StatsCard
        icon={ShoppingBag}
        label="Đơn hàng"
        value={formatCount(summary.totalOrders)}
        growth={summary.ordersGrowth}
        growthLabel="so với 30 ngày trước"
        onClick={onOrdersClick}
      />
      <StatsCard
        icon={Users}
        label="Người dùng"
        value={formatCount(summary.totalUsers)}
        growth={summary.usersGrowth}
        growthLabel="so với 30 ngày trước"
        onClick={onUsersClick}
      />
      <StatsCard
        icon={Package}
        label="Sản phẩm"
        value={formatCount(summary.totalProducts)}
        growth={summary.productsGrowth}
        growthLabel="so với 30 ngày trước"
        onClick={onProductsClick}
      />
    </div>
  )
}
