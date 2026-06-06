import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts'
import type { OrderStatusDistribution } from '@/types/dashboard'

interface OrdersByStatusChartProps {
  data: OrderStatusDistribution | null
  loading?: boolean
}

const STATUS_LABELS: Record<string, string> = {
  pending: 'Chờ xác nhận',
  confirmed: 'Đã xác nhận',
  processing: 'Đang xử lý',
  shipping: 'Đang giao',
  completed: 'Hoàn thành',
  cancelled: 'Đã hủy',
  returned: 'Trả hàng',
}

const STATUS_COLORS: Record<string, string> = {
  pending: '#f59e0b',
  confirmed: '#3b82f6',
  processing: '#8b5cf6',
  shipping: '#6366f1',
  completed: '#16a34a',
  cancelled: '#dc2626',
  returned: '#6b7280',
}

export function OrdersByStatusChart({ data, loading }: OrdersByStatusChartProps) {
  if (loading || !data) {
    return (
      <div className="bg-white rounded-xl border border-border p-6 animate-pulse">
        <div className="h-5 bg-muted rounded w-40 mb-4" />
        <div className="h-64 bg-muted rounded" />
      </div>
    )
  }

  const chartData = Object.entries(data)
    .filter(([, count]) => count > 0)
    .map(([status, count]) => ({
      name: STATUS_LABELS[status] ?? status,
      value: count,
      color: STATUS_COLORS[status] ?? '#6b7280',
    }))

  const total = chartData.reduce((sum, d) => sum + d.value, 0)

  if (total === 0) {
    return (
      <div className="bg-white rounded-xl border border-border p-6 flex items-center justify-center h-64">
        <span className="text-sm text-muted-foreground">Chưa có đơn hàng nào</span>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-xl border border-border p-6">
      <h3 className="text-sm font-semibold text-ink mb-4">Trạng thái đơn hàng</h3>
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={chartData}
              cx="50%"
              cy="50%"
              innerRadius={55}
              outerRadius={90}
              paddingAngle={2}
              dataKey="value"
            >
              {chartData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color} />
              ))}
            </Pie>
            <text
              x="50%"
              y="50%"
              textAnchor="middle"
              dominantBaseline="middle"
              className="fill-ink"
              style={{ fontSize: '20px', fontWeight: 700 }}
            >
              {total.toLocaleString('vi-VN')}
            </text>
            <Tooltip
              formatter={(value) => [Number(value).toLocaleString('vi-VN'), '']}
              contentStyle={{
                borderRadius: '8px',
                border: '1px solid #f0f0f0',
                fontSize: '13px',
              }}
            />
            <Legend
              layout="horizontal"
              verticalAlign="bottom"
              wrapperStyle={{ fontSize: '12px' }}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}
