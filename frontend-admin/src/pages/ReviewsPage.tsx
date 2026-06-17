import { useEffect, useMemo, useRef, useState } from 'react'
import { ChevronLeft, ChevronRight, Eye, EyeOff, Loader2, MessageSquareReply, RefreshCw, Search, Star, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { getReviews, setReviewVisibility, deleteReview, replyToReview } from '@/api/admin'
import type { AdminReviewItem } from '@/types/admin'

const PAGE_SIZE = 12

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function Stars({ rating }: { rating: number }) {
  return (
    <span className="inline-flex items-center gap-0.5 text-amber-500" aria-label={`${rating} sao`}>
      {Array.from({ length: 5 }, (_, index) => (
        <Star key={index} className={`size-4 ${index < rating ? 'fill-current' : 'text-gray-300'}`} />
      ))}
    </span>
  )
}

export function ReviewsPage() {
  const [reviews, setReviews] = useState<AdminReviewItem[]>([])
  const [totalItems, setTotalItems] = useState(0)
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [rating, setRating] = useState<'all' | number>('all')
  const [visibleFilter, setVisibleFilter] = useState<'all' | 'visible' | 'hidden'>('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [replyTarget, setReplyTarget] = useState<AdminReviewItem | null>(null)
  const [replyText, setReplyText] = useState('')
  const searchTimer = useRef<ReturnType<typeof setTimeout>>(null)

  const totalPages = Math.max(1, Math.ceil(totalItems / PAGE_SIZE))
  const visibleCount = useMemo(() => reviews.filter((item) => item.isVisible).length, [reviews])
  const hiddenCount = reviews.length - visibleCount
  const avgRating = useMemo(() => {
    if (reviews.length === 0) return 0
    return reviews.reduce((sum, item) => sum + item.rating, 0) / reviews.length
  }, [reviews])

  async function fetchReviews(nextPage = page) {
    setLoading(true)
    setError(null)
    try {
      const response = await getReviews({
        search,
        rating,
        isVisible: visibleFilter === 'all' ? 'all' : visibleFilter === 'visible',
        page: nextPage,
        pageSize: PAGE_SIZE,
      })
      setReviews(response.data)
      setTotalItems(response.totalItem)
      setPage(nextPage)
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Không thể tải đánh giá.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- fetching data on filter change
    void fetchReviews(1)
  }, [search, rating, visibleFilter])

  function handleSearchInput(value: string) {
    setSearchInput(value)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => setSearch(value.trim()), 300)
  }

  async function handleVisibility(item: AdminReviewItem) {
    setBusyId(item.id)
    try {
      await setReviewVisibility(item.id, !item.isVisible)
      setReviews((prev) => prev.map((review) => (
        review.id === item.id ? { ...review, isVisible: !review.isVisible } : review
      )))
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Không thể cập nhật hiển thị.')
    } finally {
      setBusyId(null)
    }
  }

  async function handleDelete(item: AdminReviewItem) {
    if (!window.confirm('Xóa vĩnh viễn đánh giá này?')) return
    setBusyId(item.id)
    try {
      await deleteReview(item.id)
      setReviews((prev) => prev.filter((review) => review.id !== item.id))
      setTotalItems((value) => Math.max(0, value - 1))
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Không thể xóa đánh giá.')
    } finally {
      setBusyId(null)
    }
  }

  async function handleReply() {
    if (!replyTarget || !replyText.trim()) return
    setBusyId(replyTarget.id)
    try {
      await replyToReview(replyTarget.id, replyTarget.productId, replyText.trim())
      setReviews((prev) => prev.map((review) => (
        review.id === replyTarget.id ? { ...review, replyCount: review.replyCount + 1 } : review
      )))
      setReplyTarget(null)
      setReplyText('')
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Không thể gửi phản hồi.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Quản lý đánh giá</h1>
          <p className="mt-1 text-sm text-muted-foreground">Kiểm duyệt phản hồi sản phẩm, ẩn nội dung chưa phù hợp và trả lời khách hàng.</p>
        </div>
        <Button variant="outline" size="icon" onClick={() => fetchReviews(page)} disabled={loading} aria-label="Làm mới">
          <RefreshCw className={`size-4 ${loading ? 'animate-spin' : ''}`} />
        </Button>
      </div>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <div className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-border">
          <span className="text-sm text-muted-foreground">Tổng đánh giá</span>
          <strong className="mt-2 block text-3xl font-semibold text-ink tabular-nums">{totalItems}</strong>
        </div>
        <div className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-border">
          <span className="text-sm text-muted-foreground">Điểm trung bình trang này</span>
          <div className="mt-2 flex items-end gap-2">
            <strong className="text-3xl font-semibold text-ink tabular-nums">{avgRating.toFixed(1)}</strong>
            <Stars rating={Math.round(avgRating)} />
          </div>
        </div>
        <div className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-border">
          <span className="text-sm text-muted-foreground">Hiển thị / ẩn trang này</span>
          <strong className="mt-2 block text-3xl font-semibold text-ink tabular-nums">{visibleCount} / {hiddenCount}</strong>
        </div>
      </section>

      {error && (
        <div className="flex items-center gap-3 rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span className="flex-1">{error}</span>
          <button className="font-medium underline" onClick={() => setError(null)}>Đóng</button>
        </div>
      )}

      <section className="rounded-2xl bg-white p-4 shadow-sm ring-1 ring-border">
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-[1fr_180px_180px_auto]">
          <label className="relative block">
            <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="pl-9"
              value={searchInput}
              onChange={(event) => handleSearchInput(event.target.value)}
              placeholder="Tìm khách hàng, sản phẩm hoặc nội dung"
            />
          </label>
          <Select value={rating} onChange={(event) => setRating(event.target.value === 'all' ? 'all' : Number(event.target.value))}>
            <option value="all">Tất cả sao</option>
            <option value="5">5 sao</option>
            <option value="4">4 sao</option>
            <option value="3">3 sao</option>
            <option value="2">2 sao</option>
            <option value="1">1 sao</option>
          </Select>
          <Select value={visibleFilter} onChange={(event) => setVisibleFilter(event.target.value as 'all' | 'visible' | 'hidden')}>
            <option value="all">Tất cả trạng thái</option>
            <option value="visible">Đang hiển thị</option>
            <option value="hidden">Đang ẩn</option>
          </Select>
          <Button variant="secondary" onClick={() => { setSearchInput(''); setSearch(''); setRating('all'); setVisibleFilter('all') }}>
            Xóa lọc
          </Button>
        </div>
      </section>

      <section className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-border">
        {loading && reviews.length === 0 ? (
          <div className="flex min-h-80 flex-col items-center justify-center gap-3 text-sm text-muted-foreground">
            <Loader2 className="size-7 animate-spin text-primary" />
            Đang tải đánh giá...
          </div>
        ) : reviews.length === 0 ? (
          <div className="flex min-h-80 flex-col items-center justify-center gap-3 px-4 text-center text-sm text-muted-foreground">
            <MessageSquareReply className="size-10 text-primary/40" />
            Chưa có đánh giá phù hợp bộ lọc.
          </div>
        ) : (
          <div className="divide-y divide-border">
            {reviews.map((item) => {
              const busy = busyId === item.id
              return (
                <article key={item.id} className={`grid gap-4 p-5 transition-colors lg:grid-cols-[220px_1fr_auto] ${item.isVisible ? 'hover:bg-muted/30' : 'bg-muted/40'}`}>
                  <div className="space-y-2">
                    <div>
                      <h2 className="font-semibold text-ink">{item.userName ?? 'Khách hàng'}</h2>
                      <p className="text-xs text-muted-foreground">{item.userEmail ?? 'Không có email'}</p>
                    </div>
                    <Stars rating={item.rating} />
                    <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${item.isVisible ? 'bg-emerald-50 text-emerald-700' : 'bg-gray-100 text-gray-600'}`}>
                      {item.isVisible ? 'Đang hiển thị' : 'Đang ẩn'}
                    </span>
                  </div>

                  <div className="min-w-0 space-y-3">
                    <div>
                      <p className="text-sm font-medium text-primary">{item.productName ?? 'Sản phẩm'}</p>
                      <p className="text-xs text-muted-foreground">{formatDate(item.createdAt)} · {item.replyCount} phản hồi</p>
                    </div>
                    <p className="text-sm leading-6 text-ink">{item.content}</p>
                  </div>

                  <div className="flex items-start justify-end gap-1">
                    <Button variant="ghost" size="icon" onClick={() => setReplyTarget(item)} disabled={busy} aria-label="Phản hồi">
                      <MessageSquareReply className="size-4" />
                    </Button>
                    <Button variant="ghost" size="icon" onClick={() => handleVisibility(item)} disabled={busy} aria-label={item.isVisible ? 'Ẩn' : 'Hiển thị'}>
                      {item.isVisible ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                    </Button>
                    <Button variant="ghost" size="icon" onClick={() => handleDelete(item)} disabled={busy} aria-label="Xóa">
                      <Trash2 className="size-4 text-destructive" />
                    </Button>
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </section>

      <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-muted-foreground">
        <span>Tổng: {totalItems} đánh giá</span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="icon" disabled={page <= 1 || loading} onClick={() => fetchReviews(page - 1)} aria-label="Trang trước">
            <ChevronLeft className="size-4" />
          </Button>
          <span>Trang {page} / {totalPages}</span>
          <Button variant="outline" size="icon" disabled={page >= totalPages || loading} onClick={() => fetchReviews(page + 1)} aria-label="Trang sau">
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </div>

      {replyTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) setReplyTarget(null) }}>
          <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
            <h2 className="text-lg font-semibold text-ink">Phản hồi đánh giá</h2>
            <p className="mt-1 text-sm text-muted-foreground">{replyTarget.productName}</p>
            <textarea
              className="mt-4 min-h-32 w-full rounded-xl border bg-white px-3 py-2 text-sm outline-none transition focus:border-primary focus:ring-2 focus:ring-primary/20"
              value={replyText}
              onChange={(event) => setReplyText(event.target.value)}
              placeholder="Nhập nội dung phản hồi"
              maxLength={500}
            />
            <div className="mt-4 flex justify-end gap-2">
              <Button variant="outline" onClick={() => setReplyTarget(null)}>Hủy</Button>
              <Button onClick={handleReply} disabled={!replyText.trim() || busyId === replyTarget.id}>Gửi phản hồi</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
