import { useCallback, useEffect, useState } from 'react'
import { CheckCircle2, Loader2, RefreshCw, Star, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { getAiTryOnFeedback, updateAiTryOnFeedbackStatus } from '@/api/admin'
import { PageSizeSelect } from '@/components/admin/PageSizeSelect'
import type { AdminAiTryOnFeedbackItem } from '@/types/admin'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function Stars({ rating }: { rating: number }) {
  return (
    <span className="inline-flex text-amber-500" aria-label={`${rating} sao`}>
      {Array.from({ length: 5 }, (_, index) => <Star key={index} className={`size-4 ${index < rating ? 'fill-current' : 'text-gray-300'}`} />)}
    </span>
  )
}

export function AiTryOnFeedbackPage() {
  const [items, setItems] = useState<AdminAiTryOnFeedbackItem[]>([])
  const [totalItems, setTotalItems] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(12)
  const [rating, setRating] = useState<'all' | number>('all')
  const [status, setStatus] = useState<'all' | 'open' | 'resolved'>('all')
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [previewImageUrl, setPreviewImageUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))

  const fetchItems = useCallback(async (nextPage: number) => {
    setLoading(true)
    setError(null)
    try {
      const response = await getAiTryOnFeedback({
        page: nextPage,
        pageSize,
        rating,
        isResolved: status === 'all' ? 'all' : status === 'resolved',
      })
      setItems(response.data)
      setTotalItems(response.totalItem)
      setPage(nextPage)
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Không thể tải đánh giá AI try-on.')
    } finally {
      setLoading(false)
    }
  }, [pageSize, rating, status])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void fetchItems(1)
  }, [fetchItems])

  useEffect(() => {
    if (!previewImageUrl) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setPreviewImageUrl(null)
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [previewImageUrl])

  function handlePageSizeChange(nextPageSize: number) {
    setPageSize(nextPageSize)
    setPage(1)
  }

  async function handleToggle(item: AdminAiTryOnFeedbackItem) {
    setBusyId(item.id)
    try {
      const updated = await updateAiTryOnFeedbackStatus(item.id, { isResolved: !item.isResolved, adminNote: item.adminNote })
      setItems((current) => current.map((entry) => entry.id === item.id ? updated : entry))
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Không thể cập nhật trạng thái.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Đánh giá AI try-on</h1>
          <p className="mt-1 text-sm text-muted-foreground">Theo dõi phản hồi khách hàng sau khi tạo ảnh thử đồ AI.</p>
        </div>
        <Button variant="outline" size="icon" onClick={() => fetchItems(page)} disabled={loading} aria-label="Làm mới">
          <RefreshCw className={`size-4 ${loading ? 'animate-spin' : ''}`} />
        </Button>
      </div>

      {error && <div className="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">{error}</div>}

      <section className="rounded-2xl bg-white p-4 shadow-sm ring-1 ring-border">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-[180px_180px_auto]">
          <Select value={rating} onChange={(event) => setRating(event.target.value === 'all' ? 'all' : Number(event.target.value))}>
            <option value="all">Tất cả sao</option>
            <option value="5">5 sao</option>
            <option value="4">4 sao</option>
            <option value="3">3 sao</option>
            <option value="2">2 sao</option>
            <option value="1">1 sao</option>
          </Select>
          <Select value={status} onChange={(event) => setStatus(event.target.value as 'all' | 'open' | 'resolved')}>
            <option value="all">Tất cả trạng thái</option>
            <option value="open">Chưa xử lý</option>
            <option value="resolved">Đã xử lý</option>
          </Select>
          <Button variant="secondary" onClick={() => { setRating('all'); setStatus('all') }}>Xóa lọc</Button>
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-border">
        {loading && items.length === 0 ? (
          <div className="flex min-h-80 flex-col items-center justify-center gap-3 text-sm text-muted-foreground">
            <Loader2 className="size-7 animate-spin text-primary" />
            Đang tải đánh giá...
          </div>
        ) : items.length === 0 ? (
          <div className="flex min-h-80 items-center justify-center text-sm text-muted-foreground">Chưa có đánh giá phù hợp.</div>
        ) : (
          <div className="divide-y divide-border">
            {items.map((item) => (
              <article key={item.id} className="grid gap-4 p-5 lg:grid-cols-[180px_1fr_auto]">
                <button
                  type="button"
                  onClick={() => setPreviewImageUrl(item.imageUrl)}
                  className="group relative block overflow-hidden rounded-xl bg-muted text-left focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  aria-label="Preview ảnh AI try-on"
                >
                  <img src={item.imageUrl} alt="Ảnh AI try-on" className="aspect-[3/4] w-full object-cover transition-transform duration-200 group-hover:scale-105" />
                  <span className="absolute inset-x-0 bottom-0 bg-black/60 px-3 py-2 text-xs font-medium text-white opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100">
                    Preview ảnh
                  </span>
                </button>
                <div className="space-y-3">
                  <div>
                    <h2 className="font-semibold text-ink">{item.userName ?? 'Khách vãng lai'}</h2>
                    <p className="text-xs text-muted-foreground">{item.userEmail ?? 'Không có email'} · {formatDate(item.createdAt)}</p>
                  </div>
                  <Stars rating={item.rating} />
                  <p className="text-sm leading-6 text-ink">{item.comment || 'Không có nội dung góp ý.'}</p>
                  <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${item.isResolved ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
                    {item.isResolved ? 'Đã xử lý' : 'Chưa xử lý'}
                  </span>
                </div>
                <Button variant="outline" onClick={() => handleToggle(item)} disabled={busyId === item.id}>
                  <CheckCircle2 className="mr-2 size-4" />
                  {item.isResolved ? 'Mở lại' : 'Đánh dấu xử lý'}
                </Button>
              </article>
            ))}
          </div>
        )}
      </section>

      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <div className="flex items-center gap-3">
          <span>Tổng: {totalItems} đánh giá</span>
          <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} disabled={loading} />
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" disabled={page <= 1 || loading} onClick={() => fetchItems(page - 1)}>Trước</Button>
          <span>Trang {page} / {totalPages}</span>
          <Button variant="outline" disabled={page >= totalPages || loading} onClick={() => fetchItems(page + 1)}>Sau</Button>
        </div>
      </div>

      {previewImageUrl && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label="Preview ảnh AI try-on"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setPreviewImageUrl(null)
            }
          }}
        >
          <button
            type="button"
            onClick={() => setPreviewImageUrl(null)}
            className="absolute right-4 top-4 rounded-full bg-white/10 p-2 text-white transition-colors hover:bg-white/20 focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-white"
            aria-label="Đóng preview"
          >
            <X className="size-5" />
          </button>
          <img
            src={previewImageUrl}
            alt="Preview ảnh AI try-on"
            className="max-h-[90vh] max-w-full rounded-2xl object-contain shadow-2xl"
          />
        </div>
      )}
    </div>
  )
}
