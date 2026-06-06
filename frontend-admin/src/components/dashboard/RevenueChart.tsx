import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts'
import type { RevenuePoint } from '@/types/dashboard'

interface RevenueChartProps {
  data: RevenuePoint[]
  loading?: boolean
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  return `${d.getDate()}/${d.getMonth() + 1}`
}

function formatTooltip(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}tr ₫`
  return `${value.toLocaleString()} ₫`
}

export function RevenueChart({ data, loading }: RevenueChartProps) {
  if (loading) {
    return (
      <div className="bg-white rounded-xl border border-border p-6 animate-pulse">
        <div className="h-5 bg-muted rounded w-40 mb-4" />
        <div className="h-64 bg-muted rounded" />
      </div>
    )
  }

  const chartData = data.map((p) => ({
    date: formatDate(p.date),
    revenue: p.revenue,
    orders: p.orders,
  }))

  return (
    <div className="bg-white rounded-xl border border-border p-6">
      <h3 className="text-sm font-semibold text-ink mb-4">Doanh thu</h3>
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={chartData}>
            <defs>
              <linearGradient id="revenueGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#721311" stopOpacity={0.15} />
                <stop offset="95%" stopColor="#721311" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
            <XAxis
              dataKey="date"
              tick={{ fontSize: 12, fill: '#6a7282' }}
              axisLine={false}
              tickLine={false}
            />
            <YAxis
              tick={{ fontSize: 12, fill: '#6a7282' }}
              axisLine={false}
              tickLine={false}
              tickFormatter={formatTooltip}
            />
            <Tooltip
              formatter={(value) => [formatTooltip(Number(value) || 0), 'Doanh thu']}
              contentStyle={{
                borderRadius: '8px',
                border: '1px solid #f0f0f0',
                fontSize: '13px',
              }}
            />
            <Area
              type="monotone"
              dataKey="revenue"
              stroke="#721311"
              strokeWidth={2}
              fill="url(#revenueGradient)"
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}
