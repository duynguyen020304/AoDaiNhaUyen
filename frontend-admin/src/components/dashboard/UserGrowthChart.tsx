import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts'
import type { UserGrowthPoint } from '@/types/dashboard'

interface UserGrowthChartProps {
  data: UserGrowthPoint[]
  loading?: boolean
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  return `${d.getDate()}/${d.getMonth() + 1}`
}

export function UserGrowthChart({ data, loading }: UserGrowthChartProps) {
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
    newUsers: p.newUsers,
    totalUsers: p.totalUsers,
  }))

  return (
    <div className="bg-white rounded-xl border border-border p-6">
      <h3 className="text-sm font-semibold text-ink mb-4">Tăng trưởng người dùng</h3>
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={chartData}>
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
            />
            <Tooltip
              contentStyle={{
                borderRadius: '8px',
                border: '1px solid #f0f0f0',
                fontSize: '13px',
              }}
              formatter={(value, name) => [
                Number(value).toLocaleString('vi-VN'),
                String(name) === 'totalUsers' ? 'Tổng người dùng' : 'Người dùng mới',
              ]}
            />
            <Line
              type="monotone"
              dataKey="totalUsers"
              name="totalUsers"
              stroke="#721311"
              strokeWidth={2}
              dot={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}
