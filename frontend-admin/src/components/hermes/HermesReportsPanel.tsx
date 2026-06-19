import { useCallback, useEffect } from 'react'
import { FileText } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { HermesReportDetailDrawer } from '@/components/hermes/HermesReportDetailDrawer'
import { HermesReportFilters } from '@/components/hermes/HermesReportFilters'
import { HermesReportTable } from '@/components/hermes/HermesReportTable'
import { useHermesReportStore } from '@/stores/hermesReportStore'

export function HermesReportsPanel() {
  const items = useHermesReportStore((s) => s.items)
  const filters = useHermesReportStore((s) => s.filters)
  const selectedId = useHermesReportStore((s) => s.selectedId)
  const selectedReport = useHermesReportStore((s) => s.selectedReport)
  const loadingList = useHermesReportStore((s) => s.loadingList)
  const loadingDetail = useHermesReportStore((s) => s.loadingDetail)
  const error = useHermesReportStore((s) => s.error)
  const totalItem = useHermesReportStore((s) => s.totalItem)
  const hasNextPage = useHermesReportStore((s) => s.hasNextPage)
  const hasPreviousPage = useHermesReportStore((s) => s.hasPreviousPage)
  const setFilters = useHermesReportStore((s) => s.setFilters)
  const resetFilters = useHermesReportStore((s) => s.resetFilters)
  const fetchReports = useHermesReportStore((s) => s.fetchReports)
  const openDetail = useHermesReportStore((s) => s.openDetail)
  const closeDetail = useHermesReportStore((s) => s.closeDetail)

  const proactiveCount = items.filter((item) => item.source === 'hermes_cron').length
  const eventDrivenCount = items.filter((item) => item.source === 'hermes_agent').length

  const refresh = useCallback(() => {
    void fetchReports()
  }, [fetchReports])

  useEffect(() => {
    const timeoutId = window.setTimeout(refresh, 250)
    return () => window.clearTimeout(timeoutId)
  }, [filters, refresh])

  return (
    <div className="flex h-full flex-col overflow-y-auto bg-zinc-50 p-4 lg:p-6">
      <div className="mx-auto w-full max-w-7xl space-y-5">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <FileText className="size-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-ink">Báo cáo Hermes</h2>
            </div>
            <p className="mt-1 text-sm text-muted-foreground">Báo cáo do Hermes runner gửi về backend và lưu DB.</p>
          </div>
          <Button onClick={refresh} disabled={loadingList}>Làm mới</Button>
        </div>

        <div className="grid gap-3 md:grid-cols-3">
          <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">Chủ động</p>
            <p className="mt-1 text-2xl font-bold text-emerald-900">{proactiveCount}</p>
            <p className="mt-1 text-xs text-emerald-700">Báo cáo do lịch tự động/chiến lược tạo.</p>
          </div>
          <div className="rounded-xl border border-indigo-200 bg-indigo-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-indigo-700">Theo sự kiện</p>
            <p className="mt-1 text-2xl font-bold text-indigo-900">{eventDrivenCount}</p>
            <p className="mt-1 text-xs text-indigo-700">Báo cáo từ đơn hàng, tồn kho, SEO, review.</p>
          </div>
          <div className="rounded-xl border border-zinc-200 bg-white p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-zinc-600">Tổng báo cáo</p>
            <p className="mt-1 text-2xl font-bold text-zinc-950">{totalItem}</p>
            <p className="mt-1 text-xs text-muted-foreground">Lọc nguồn để xem Hermes chủ động làm gì.</p>
          </div>
        </div>

        <HermesReportFilters filters={filters} onChange={setFilters} onReset={resetFilters} />

        {error && (
          <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
            {error}
          </div>
        )}

        <div className="flex items-center justify-between" aria-live="polite">
          <p className="text-sm text-muted-foreground">{totalItem} báo cáo</p>
          <div className="flex gap-2">
            <Button variant="outline" disabled={!hasPreviousPage || loadingList} onClick={() => setFilters({ page: Math.max(1, filters.page - 1) })}>Trước</Button>
            <Button variant="outline" disabled={!hasNextPage || loadingList} onClick={() => setFilters({ page: filters.page + 1 })}>Sau</Button>
          </div>
        </div>

        <HermesReportTable items={items} loading={loadingList} onOpen={(id) => void openDetail(id)} />
        <HermesReportDetailDrawer open={Boolean(selectedId)} loading={loadingDetail} report={selectedReport} onClose={closeDetail} />
      </div>
    </div>
  )
}
