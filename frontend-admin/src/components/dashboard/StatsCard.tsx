import type { LucideIcon } from 'lucide-react'

interface StatsCardProps {
  icon: LucideIcon
  label: string
  value: string
  growth: number
  growthLabel?: string
  onClick?: () => void
}

function formatGrowth(n: number): string {
  if (n > 0) return `↑${n}%`
  if (n < 0) return `↓${Math.abs(n)}%`
  return '0%'
}

export function StatsCard({ icon: Icon, label, value, growth, growthLabel, onClick }: StatsCardProps) {
  const isPositive = growth >= 0
  const Tag = onClick ? 'button' : 'div'

  return (
    <Tag
      onClick={onClick}
      className={`bg-white rounded-xl border border-border p-5 flex flex-col gap-3 transition-shadow hover:shadow-md ${onClick ? 'cursor-pointer text-left' : ''}`}
    >
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-muted-foreground">{label}</span>
        <div className="size-10 rounded-lg bg-primary/10 flex items-center justify-center">
          <Icon className="size-5 text-primary" />
        </div>
      </div>
      <div>
        <div className="text-2xl font-bold text-ink">{value}</div>
        <div className="flex items-center gap-1.5 mt-1">
          <span className={`text-xs font-medium ${isPositive ? 'text-green-600' : 'text-red-600'}`}>
            {formatGrowth(growth)}
          </span>
          {growthLabel && (
            <span className="text-xs text-muted-foreground">{growthLabel}</span>
          )}
        </div>
      </div>
    </Tag>
  )
}
