import type { HermesReportFilters as HermesReportFiltersType } from '@/types/hermes'
import { Button } from '@/components/ui/button'

interface Props {
  filters: HermesReportFiltersType
  onChange: (filters: Partial<HermesReportFiltersType>) => void
  onReset: () => void
}

export function HermesReportFilters({ filters, onChange, onReset }: Props) {
  return (
    <section className="rounded-xl border border-zinc-200 bg-white p-4 shadow-sm" aria-label="Bộ lọc báo cáo Hermes">
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-5">
        <label className="space-y-1 text-xs font-medium text-zinc-600">
          Tìm kiếm
          <input
            value={filters.q ?? ''}
            onChange={(event) => onChange({ q: event.target.value })}
            placeholder="Tiêu đề, tóm tắt, correlation id"
            className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm"
          />
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600">
          Mức độ
          <select value={filters.severity ?? ''} onChange={(event) => onChange({ severity: event.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm">
            <option value="">Tất cả</option>
            <option value="info">Info</option>
            <option value="warning">Warning</option>
            <option value="high">High</option>
            <option value="critical">Critical</option>
          </select>
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600">
          Trạng thái
          <select value={filters.status ?? ''} onChange={(event) => onChange({ status: event.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm">
            <option value="">Tất cả</option>
            <option value="open">Open</option>
            <option value="acknowledged">Acknowledged</option>
            <option value="resolved">Resolved</option>
          </select>
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600">
          Nguồn
          <select value={filters.source ?? ''} onChange={(event) => onChange({ source: event.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm">
            <option value="">Tất cả</option>
            <option value="hermes_cron">Chủ động</option>
            <option value="hermes_agent">Tự động theo sự kiện</option>
            <option value="hermes_chat">Từ chat admin</option>
          </select>
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600">
          Loại
          <input
            value={filters.type ?? ''}
            onChange={(event) => onChange({ type: event.target.value })}
            placeholder="provider_health"
            className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm"
          />
        </label>
      </div>
      <div className="mt-3 flex justify-end">
        <Button variant="outline" onClick={onReset}>Xóa bộ lọc</Button>
      </div>
    </section>
  )
}
