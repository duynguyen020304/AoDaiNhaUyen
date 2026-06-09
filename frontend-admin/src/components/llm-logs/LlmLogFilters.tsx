import type { LlmAuditLogFilters } from '@/types/llmLogs'
import { Button } from '@/components/ui/button'

interface Props {
  filters: LlmAuditLogFilters
  onChange: (filters: Partial<LlmAuditLogFilters>) => void
  onReset: () => void
}

export function LlmLogFilters({ filters, onChange, onReset }: Props) {
  return (
    <section className="rounded-xl border border-zinc-200 bg-white p-4 shadow-sm dark:border-zinc-800 dark:bg-zinc-900" aria-label="Bộ lọc nhật ký LLM">
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-6">
        <label className="space-y-1 text-xs font-medium text-zinc-600 dark:text-zinc-300">
          Tìm kiếm
          <input
            value={filters.q ?? ''}
            onChange={(e) => onChange({ q: e.target.value })}
            placeholder="Prompt, output, request id"
            className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950"
          />
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600 dark:text-zinc-300">
          Nguồn
          <select value={filters.source ?? ''} onChange={(e) => onChange({ source: e.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950">
            <option value="">Tất cả</option>
            <option value="AdminAi">Admin AI</option>
            <option value="CustomerChat">Chat khách</option>
            <option value="IntentClassifier">Phân loại intent</option>
            <option value="TryOn">AI Try-On</option>
            <option value="ToolCall">Tool</option>
          </select>
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600 dark:text-zinc-300">
          Trạng thái
          <select value={filters.status ?? ''} onChange={(e) => onChange({ status: e.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950">
            <option value="">Tất cả</option>
            <option value="success">Thành công</option>
            <option value="failed">Lỗi</option>
            <option value="timeout">Timeout</option>
            <option value="cancelled">Đã hủy</option>
          </select>
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600 dark:text-zinc-300">
          Rủi ro
          <select value={filters.riskLevel ?? ''} onChange={(e) => onChange({ riskLevel: e.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950">
            <option value="">Tất cả</option>
            <option value="Read">Read</option>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600 dark:text-zinc-300">
          Từ ngày
          <input type="datetime-local" value={filters.from ?? ''} onChange={(e) => onChange({ from: e.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950" />
        </label>
        <label className="space-y-1 text-xs font-medium text-zinc-600 dark:text-zinc-300">
          Đến ngày
          <input type="datetime-local" value={filters.to ?? ''} onChange={(e) => onChange({ to: e.target.value })} className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950" />
        </label>
      </div>
      <div className="mt-3 flex justify-end">
        <Button variant="outline" onClick={onReset}>Xóa bộ lọc</Button>
      </div>
    </section>
  )
}
