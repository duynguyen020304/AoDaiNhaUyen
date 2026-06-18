import { useCallback, useEffect } from 'react'
import { FileSearch } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { LlmLogDetailDrawer } from '@/components/llm-logs/LlmLogDetailDrawer'
import { LlmLogFilters } from '@/components/llm-logs/LlmLogFilters'
import { LlmLogStatsCards } from '@/components/llm-logs/LlmLogStatsCards'
import { LlmLogTable } from '@/components/llm-logs/LlmLogTable'
import { useLlmLogStore } from '@/stores/llmLogStore'

export function LlmLogsPage() {
  const items = useLlmLogStore((s) => s.items)
  const filters = useLlmLogStore((s) => s.filters)
  const stats = useLlmLogStore((s) => s.stats)
  const selectedId = useLlmLogStore((s) => s.selectedId)
  const selectedLog = useLlmLogStore((s) => s.selectedLog)
  const loadingList = useLlmLogStore((s) => s.loadingList)
  const loadingDetail = useLlmLogStore((s) => s.loadingDetail)
  const error = useLlmLogStore((s) => s.error)
  const totalItem = useLlmLogStore((s) => s.totalItem)
  const hasNextPage = useLlmLogStore((s) => s.hasNextPage)
  const hasPreviousPage = useLlmLogStore((s) => s.hasPreviousPage)
  const setFilters = useLlmLogStore((s) => s.setFilters)
  const resetFilters = useLlmLogStore((s) => s.resetFilters)
  const fetchLogs = useLlmLogStore((s) => s.fetchLogs)
  const fetchStats = useLlmLogStore((s) => s.fetchStats)
  const openDetail = useLlmLogStore((s) => s.openDetail)
  const closeDetail = useLlmLogStore((s) => s.closeDetail)

  const refresh = useCallback(() => {
    void fetchLogs()
    void fetchStats()
  }, [fetchLogs, fetchStats])

  useEffect(() => {
    const timeoutId = window.setTimeout(refresh, 250)
    return () => window.clearTimeout(timeoutId)
  }, [filters, refresh])

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <FileSearch className="size-6 text-primary" />
            <h1 className="text-2xl font-bold tracking-tight text-ink">Nhật ký LLM</h1>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">Theo dõi lời gọi AI, công cụ, token, lỗi và hành động đã duyệt.</p>
        </div>
        <Button onClick={refresh} disabled={loadingList}>Làm mới</Button>
      </div>

      <LlmLogStatsCards stats={stats} />
      <LlmLogFilters filters={filters} onChange={setFilters} onReset={resetFilters} />

      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
          {error}
        </div>
      )}

      <div className="flex items-center justify-between" aria-live="polite">
        <p className="text-sm text-muted-foreground">{totalItem} log</p>
        <div className="flex gap-2">
          <Button variant="outline" disabled={!hasPreviousPage || loadingList} onClick={() => setFilters({ page: Math.max(1, filters.page - 1) })}>Trước</Button>
          <Button variant="outline" disabled={!hasNextPage || loadingList} onClick={() => setFilters({ page: filters.page + 1 })}>Sau</Button>
        </div>
      </div>

      <LlmLogTable items={items} loading={loadingList} onOpen={(id) => void openDetail(id)} />
      <LlmLogDetailDrawer open={Boolean(selectedId)} loading={loadingDetail} log={selectedLog} onClose={closeDetail} />
    </div>
  )
}
