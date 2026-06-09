import { Activity, AlertTriangle, CheckCircle2, Clock, Coins, Database } from 'lucide-react'
import type { LlmAuditLogStats } from '@/types/llmLogs'

interface Props {
  stats: LlmAuditLogStats | null
}

export function LlmLogStatsCards({ stats }: Props) {
  const cards = [
    { label: 'Tổng log', value: stats?.total ?? 0, icon: Database, tone: 'text-zinc-700' },
    { label: 'Thành công', value: stats?.success ?? 0, icon: CheckCircle2, tone: 'text-emerald-600' },
    { label: 'Lỗi', value: stats?.failed ?? 0, icon: AlertTriangle, tone: 'text-red-600' },
    { label: 'Timeout', value: stats?.timeout ?? 0, icon: Clock, tone: 'text-amber-600' },
    { label: 'Latency TB', value: `${Math.round(stats?.averageLatencyMs ?? 0)}ms`, icon: Activity, tone: 'text-blue-600' },
    { label: 'Token', value: stats?.totalTokens ?? 0, icon: Coins, tone: 'text-violet-600' },
  ]

  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
      {cards.map(({ label, value, icon: Icon, tone }) => (
        <div key={label} className="rounded-xl border border-zinc-200 bg-white p-4 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex items-center justify-between gap-3">
            <span className="text-xs font-medium text-zinc-500">{label}</span>
            <Icon className={`size-4 ${tone}`} />
          </div>
          <div className="mt-2 text-xl font-semibold text-zinc-900 dark:text-zinc-50">{value}</div>
        </div>
      ))}
    </div>
  )
}
