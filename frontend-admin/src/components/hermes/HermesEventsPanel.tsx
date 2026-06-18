import { useMemo, useState } from 'react'
import { ExternalLink, RefreshCcw, Share2 } from 'lucide-react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { createHermesMonitorLink, getHermesEvents } from '@/api/hermes'
import { Button } from '@/components/ui/button'
import { queryKeys } from '@/queries/queryKeys'
import type { HermesEventFilters, HermesEventListItem } from '@/types/hermes'

const statusLabel: Record<string, string> = {
  pending: 'Đang chờ',
  processing: 'Đang xử lý',
  completed: 'Hoàn tất',
  failed: 'Lỗi',
  dead: 'Dừng retry',
  cancelled: 'Đã hủy',
}

const statusClass: Record<string, string> = {
  pending: 'bg-amber-50 text-amber-800 ring-amber-200',
  processing: 'bg-blue-50 text-blue-800 ring-blue-200',
  completed: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  failed: 'bg-rose-50 text-rose-800 ring-rose-200',
  dead: 'bg-zinc-900 text-white ring-zinc-700',
  cancelled: 'bg-zinc-100 text-zinc-700 ring-zinc-200',
}

function shortId(id: string) {
  return id.slice(0, 8)
}

function fmt(value: string | null) {
  if (!value) return 'Chưa có'
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

export function HermesEventsPanel() {
  const [filters, setFilters] = useState<HermesEventFilters>({ page: 1, pageSize: 20 })
  const [openedLink, setOpenedLink] = useState<string | null>(null)
  const queryKey = useMemo(() => queryKeys.hermes.events(filters), [filters])

  const eventsQuery = useQuery({
    queryKey,
    queryFn: () => getHermesEvents(filters),
    staleTime: 5_000,
  })

  const createLink = useMutation({
    mutationFn: (eventId: string) => createHermesMonitorLink({ scopeType: 'event', scopeId: eventId, expiresInHours: 24 }),
    onSuccess: (link) => {
      setOpenedLink(link.url)
      window.open(link.url, '_blank', 'noopener,noreferrer')
    },
  })

  const items = eventsQuery.data?.data ?? []
  const total = eventsQuery.data?.totalItem ?? 0
  const totalPages = eventsQuery.data?.totalPage ?? 1

  return (
    <div className="flex h-full flex-col overflow-y-auto bg-zinc-50 p-4 lg:p-6">
      <div className="mx-auto w-full max-w-7xl space-y-5">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <Share2 className="size-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-ink">Sự kiện Hermes</h2>
            </div>
            <p className="mt-1 text-sm text-muted-foreground">Outbox event, worker state và link giám sát công khai đọc-only.</p>
          </div>
          <Button type="button" variant="outline" onClick={() => void eventsQuery.refetch()} disabled={eventsQuery.isFetching}>
            <RefreshCcw className="size-4" />
            Làm mới
          </Button>
        </div>

        <div className="grid gap-3 rounded-xl border border-zinc-200 bg-white p-3 shadow-sm md:grid-cols-4">
          <label className="space-y-1 text-sm font-medium text-zinc-700">
            Trạng thái
            <select
              value={filters.status ?? ''}
              onChange={(event) => setFilters((prev) => ({ ...prev, page: 1, status: event.target.value || undefined }))}
              className="h-10 w-full rounded-lg border border-zinc-200 bg-white px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-wine"
            >
              <option value="">Tất cả</option>
              {Object.entries(statusLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="space-y-1 text-sm font-medium text-zinc-700 md:col-span-2">
            Tìm kiếm
            <input
              value={filters.q ?? ''}
              onChange={(event) => setFilters((prev) => ({ ...prev, page: 1, q: event.target.value || undefined }))}
              placeholder="AggregateId, correlationId, idempotency..."
              className="h-10 w-full rounded-lg border border-zinc-200 px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-wine"
            />
          </label>
          <div className="flex items-end">
            <Button type="button" variant="secondary" className="w-full" onClick={() => setFilters({ page: 1, pageSize: 20 })}>Xóa lọc</Button>
          </div>
        </div>

        {eventsQuery.error && (
          <div className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700" role="alert">Không tải được event Hermes.</div>
        )}
        {openedLink && (
          <div className="flex flex-col gap-2 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-800 md:flex-row md:items-center md:justify-between" role="status">
            <span>Đã mở link giám sát. Link hết hạn sau 24 giờ.</span>
            <button type="button" className="text-left font-semibold underline" onClick={() => void navigator.clipboard.writeText(openedLink)}>Copy link</button>
          </div>
        )}

        <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[980px] text-left text-sm">
              <thead className="bg-zinc-50 text-xs uppercase tracking-wide text-zinc-500">
                <tr>
                  <th className="px-4 py-3">Event</th>
                  <th className="px-4 py-3">Aggregate</th>
                  <th className="px-4 py-3">Trạng thái</th>
                  <th className="px-4 py-3">Retry</th>
                  <th className="px-4 py-3">Lịch</th>
                  <th className="px-4 py-3 text-right">Giám sát</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-100">
                {items.map((item) => (
                  <EventRow key={item.id} item={item} busy={createLink.isPending} onOpen={() => createLink.mutate(item.id)} />
                ))}
                {eventsQuery.isLoading && Array.from({ length: 5 }).map((_, index) => (
                  <tr key={index} className="animate-pulse">
                    <td className="px-4 py-4" colSpan={6}><div className="h-5 rounded bg-zinc-100" /></td>
                  </tr>
                ))}
                {!eventsQuery.isLoading && items.length === 0 && (
                  <tr><td className="px-4 py-10 text-center text-zinc-500" colSpan={6}>Chưa có event Hermes.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="flex items-center justify-between text-sm text-zinc-600">
          <span>{total} event</span>
          <div className="flex gap-2">
            <Button type="button" variant="outline" disabled={filters.page <= 1 || eventsQuery.isFetching} onClick={() => setFilters((prev) => ({ ...prev, page: Math.max(1, prev.page - 1) }))}>Trước</Button>
            <Button type="button" variant="outline" disabled={filters.page >= totalPages || eventsQuery.isFetching} onClick={() => setFilters((prev) => ({ ...prev, page: prev.page + 1 }))}>Sau</Button>
          </div>
        </div>
      </div>
    </div>
  )
}

function EventRow({ item, busy, onOpen }: { item: HermesEventListItem; busy: boolean; onOpen: () => void }) {
  return (
    <tr className="align-top hover:bg-zinc-50/70">
      <td className="px-4 py-4">
        <div className="font-semibold text-zinc-900">{item.eventType}</div>
        <div className="mt-1 font-mono text-xs text-zinc-500">#{shortId(item.id)}</div>
        {item.lastError && <div className="mt-2 max-w-md truncate text-xs text-rose-700">{item.lastError}</div>}
      </td>
      <td className="px-4 py-4">
        <div className="text-zinc-900">{item.aggregateType}</div>
        <div className="mt-1 font-mono text-xs text-zinc-500">{item.aggregateId}</div>
      </td>
      <td className="px-4 py-4">
        <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ${statusClass[item.status] ?? 'bg-zinc-100 text-zinc-700 ring-zinc-200'}`}>
          {statusLabel[item.status] ?? item.status}
        </span>
      </td>
      <td className="px-4 py-4 text-zinc-700">{item.attempts}/{item.maxAttempts}</td>
      <td className="px-4 py-4 text-xs text-zinc-600">
        <div>Tạo: {fmt(item.createdAt)}</div>
        <div className="mt-1">Xử lý: {fmt(item.processedAt)}</div>
      </td>
      <td className="px-4 py-4 text-right">
        <Button type="button" size="sm" onClick={onOpen} disabled={busy}>
          <ExternalLink className="size-4" />
          Mở màn hình giám sát
        </Button>
      </td>
    </tr>
  )
}
