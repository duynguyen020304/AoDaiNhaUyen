import {
  Bar,
  BarChart,
  Cell,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { SocialAnalytics } from '@/api/social'

interface SocialAnalyticsChartProps {
  data: SocialAnalytics | null
  loading?: boolean
}

const METRIC_LABELS: Record<string, string> = {
  impressions: 'Hiển thị',
  likes: 'Thích',
  comments: 'Bình luận',
  shares: 'Chia sẻ',
  clicks: 'Nhấp',
  views: 'Lượt xem',
}

const METRIC_COLORS: Record<string, string> = {
  impressions: '#721311',
  likes: '#dc2626',
  comments: '#f59e0b',
  shares: '#16a34a',
  clicks: '#2563eb',
  views: '#7c3aed',
}

function formatNumber(value: number) {
  return value.toLocaleString('vi-VN')
}

export function SocialAnalyticsChart({ data, loading }: SocialAnalyticsChartProps) {
  if (loading) {
    return (
      <div className="animate-pulse rounded-xl border border-border bg-white p-6">
        <div className="mb-4 h-5 w-48 rounded bg-muted" />
        <div className="h-64 rounded bg-muted" />
      </div>
    )
  }

  const metrics = data?.posts
  const chartData = metrics
    ? Object.entries(metrics).map(([key, value]) => ({
      key,
      name: METRIC_LABELS[key] ?? key,
      value: Number(value) || 0,
      fill: METRIC_COLORS[key] ?? '#6b7280',
    }))
    : []
  const total = chartData.reduce((sum, item) => sum + item.value, 0)

  return (
    <div className="rounded-xl border border-border bg-white p-6">
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-ink">Thống kê Fanpage Facebook</h3>
          <p className="mt-1 text-xs text-muted-foreground">
            Dữ liệu từ Zernio{data ? ` · ${data.fromDate} → ${data.toDate}` : ''}
          </p>
        </div>
        <div className="rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
          Tổng tương tác: {formatNumber(total)}
        </div>
      </div>

      {chartData.length === 0 ? (
        <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">
          Chưa có dữ liệu thống kê fanpage.
        </div>
      ) : (
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData} margin={{ left: 0, right: 8 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis
                dataKey="name"
                tick={{ fontSize: 12, fill: '#6a7282' }}
                axisLine={false}
                tickLine={false}
              />
              <YAxis
                tick={{ fontSize: 12, fill: '#6a7282' }}
                axisLine={false}
                tickLine={false}
                tickFormatter={(value) => formatNumber(Number(value) || 0)}
              />
              <Tooltip
                formatter={(value) => [formatNumber(Number(value) || 0), '']}
                contentStyle={{
                  borderRadius: '8px',
                  border: '1px solid #f0f0f0',
                  fontSize: '13px',
                }}
              />
              <Bar dataKey="value" radius={[8, 8, 0, 0]}>
                {chartData.map((item) => (
                  <Cell key={item.key} fill={item.fill} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
