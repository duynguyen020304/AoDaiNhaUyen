import { useEffect, useState, useCallback } from 'react'
import { RefreshCw, Truck, CheckCircle2, ArrowRight, ChevronLeft, ChevronRight, Search, FilterX } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { getAdminOrders, updateOrderStatus, createShipment } from '@/api/admin'
import { invalidateAdminDashboardQueries } from '@/queries/invalidateAdminQueries'
import type { AdminOrderListItem } from '@/types/admin'
import { PageSizeSelect } from '@/components/admin/PageSizeSelect'

const STATUS_LABELS: Record<string, string> = {
  pending: 'Chờ xác nhận',
  confirmed: 'Đã xác nhận',
  processing: 'Đang chuẩn bị',
  shipping: 'Đang giao hàng',
  completed: 'Hoàn thành',
  cancelled: 'Đã hủy',
  returned: 'Đã trả hàng',
}

const STATUS_COLORS: Record<string, string> = {
  pending: 'bg-gray-100 text-gray-700',
  confirmed: 'bg-blue-100 text-blue-700',
  processing: 'bg-blue-100 text-blue-700',
  shipping: 'bg-orange-100 text-orange-700',
  completed: 'bg-emerald-100 text-emerald-700',
  cancelled: 'bg-red-100 text-red-700',
  returned: 'bg-red-100 text-red-700',
}

const NEXT_STATUS: Record<string, string> = {
  pending: 'confirmed',
  confirmed: 'processing',
  processing: 'shipping',
  shipping: 'completed',
}

const FILTER_TABS = [
  { key: 'all', label: 'Tất cả' },
  { key: 'pending', label: 'Chờ xác nhận' },
  { key: 'confirmed', label: 'Đã xác nhận' },
  { key: 'processing', label: 'Đang chuẩn bị' },
  { key: 'shipping', label: 'Đang giao' },
  { key: 'completed', label: 'Hoàn thành' },
  { key: 'cancelled', label: 'Đã hủy' },
  { key: 'returned', label: 'Đã trả hàng' },
]

const SORT_OPTIONS = [
  { value: 'newest', label: 'Mới nhất' },
  { value: 'oldest', label: 'Cũ nhất' },
  { value: 'amount_desc', label: 'Tổng tiền cao → thấp' },
  { value: 'amount_asc', label: 'Tổng tiền thấp → cao' },
]

export function OrdersPage() {
  const [orders, setOrders] = useState<AdminOrderListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState('all')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [minTotal, setMinTotal] = useState('')
  const [maxTotal, setMaxTotal] = useState('')
  const [sort, setSort] = useState('newest')
  const [page, setPage] = useState(1)
  const [totalPage, setTotalPage] = useState(1)
  const [totalItem, setTotalItem] = useState(0)
  const [pageSize, setPageSize] = useState(20)
  const [processingId, setProcessingId] = useState<string | null>(null)
  const [showShipModal, setShowShipModal] = useState<AdminOrderListItem | null>(null)
  const [carrier, setCarrier] = useState('')
  const [trackingNumber, setTrackingNumber] = useState('')

  const fetchOrders = useCallback(() => {
    setLoading(true)
    getAdminOrders({
      status: filter,
      search: search || undefined,
      fromDate: fromDate || undefined,
      toDate: toDate || undefined,
      minTotal: minTotal ? Number(minTotal) : undefined,
      maxTotal: maxTotal ? Number(maxTotal) : undefined,
      sort,
      page,
      pageSize,
    })
      .then((result) => {
        setOrders(result.data)
        setTotalPage(result.totalPage)
        setTotalItem(result.totalItem)
      })
      .catch(() => {
        setOrders([])
        setTotalPage(1)
        setTotalItem(0)
      })
      .finally(() => setLoading(false))
  }, [filter, fromDate, maxTotal, minTotal, page, pageSize, search, sort, toDate])

  useEffect(() => {
    const timeoutId = window.setTimeout(fetchOrders, 0)
    return () => window.clearTimeout(timeoutId)
  }, [fetchOrders])

  function changeFilter(nextFilter: string) {
    setFilter(nextFilter)
    setPage(1)
  }

  function applySearch() {
    setSearch(searchInput.trim())
    setPage(1)
  }

  function clearFilters() {
    setFilter('all')
    setSearchInput('')
    setSearch('')
    setFromDate('')
    setToDate('')
    setMinTotal('')
    setMaxTotal('')
    setSort('newest')
    setPage(1)
  }

  async function handleAdvanceStatus(orderId: string, nextStatus: string) {
    try {
      setProcessingId(orderId)
      await updateOrderStatus(orderId, nextStatus)
      invalidateAdminDashboardQueries()
      fetchOrders()
    } catch {
      // Error handled silently for demo
    } finally {
      setProcessingId(null)
    }
  }

  function handlePageSizeChange(nextPageSize: number) {
    setPageSize(nextPageSize)
    setPage(1)
  }

  async function handleShip() {
    if (!showShipModal) return
    try {
      setProcessingId(showShipModal.id)
      await createShipment(showShipModal.id, carrier || undefined, trackingNumber || undefined)
      invalidateAdminDashboardQueries()
      fetchOrders()
      setShowShipModal(null)
      setCarrier('')
      setTrackingNumber('')
    } catch {
      // Error handled silently
    } finally {
      setProcessingId(null)
    }
  }

  function formatDate(iso: string) {
    return new Date(iso).toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  function formatPrice(amount: number) {
    return amount.toLocaleString('vi-VN') + ' ₫'
  }

  const startItem = totalItem === 0 ? 0 : (page - 1) * pageSize + 1
  const endItem = Math.min(page * pageSize, totalItem)

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Đơn hàng</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Quản lý trạng thái và vận chuyển đơn hàng
          </p>
        </div>
        <Button variant="outline" size="icon" onClick={fetchOrders} disabled={loading}>
          <RefreshCw className={`size-4 ${loading ? 'animate-spin' : ''}`} />
        </Button>
      </div>

      <div className="flex flex-wrap gap-1 bg-muted rounded-lg p-1">
        {FILTER_TABS.map((tab) => (
          <button
            key={tab.key}
            onClick={() => changeFilter(tab.key)}
            className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
              filter === tab.key
                ? 'bg-white text-ink shadow-sm'
                : 'text-muted-foreground hover:text-ink'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <Card className="p-4">
        <div className="grid gap-3 lg:grid-cols-[minmax(240px,1.5fr)_160px_160px_140px_140px_180px_auto]">
          <label className="relative block">
            <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="pl-9"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              onKeyDown={(event) => { if (event.key === 'Enter') applySearch() }}
              placeholder="Tìm mã đơn, khách, SĐT, email..."
            />
          </label>
          <Input
            type="date"
            value={fromDate}
            onChange={(event) => { setFromDate(event.target.value); setPage(1) }}
            aria-label="Từ ngày"
          />
          <Input
            type="date"
            value={toDate}
            onChange={(event) => { setToDate(event.target.value); setPage(1) }}
            aria-label="Đến ngày"
          />
          <Input
            type="number"
            min="0"
            inputMode="numeric"
            value={minTotal}
            onChange={(event) => { setMinTotal(event.target.value); setPage(1) }}
            placeholder="Từ tiền"
          />
          <Input
            type="number"
            min="0"
            inputMode="numeric"
            value={maxTotal}
            onChange={(event) => { setMaxTotal(event.target.value); setPage(1) }}
            placeholder="Đến tiền"
          />
          <Select
            value={sort}
            onChange={(event) => { setSort(event.target.value); setPage(1) }}
            aria-label="Sắp xếp"
          >
            {SORT_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </Select>
          <div className="flex gap-2">
            <Button type="button" variant="outline" onClick={applySearch}>Lọc</Button>
            <Button type="button" variant="ghost" onClick={clearFilters} title="Xóa bộ lọc">
              <FilterX className="size-4" />
            </Button>
          </div>
        </div>
      </Card>

      <Card className="overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-12 text-muted-foreground text-sm">
            Đang tải...
          </div>
        ) : orders.length === 0 ? (
          <div className="flex items-center justify-center py-12 text-muted-foreground text-sm">
            Không có đơn hàng nào.
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã đơn</TableHead>
                <TableHead>Khách hàng</TableHead>
                <TableHead className="text-right">Tổng tiền</TableHead>
                <TableHead className="text-center">Trạng thái</TableHead>
                <TableHead>Ngày tạo</TableHead>
                <TableHead>Hoàn thành</TableHead>
                <TableHead className="text-center">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {orders.map((order) => {
                const nextStatus = NEXT_STATUS[order.status]
                const isProcessing = processingId === order.id
                return (
                  <TableRow key={order.id}>
                    <TableCell className="font-mono font-medium text-ink">{order.orderCode}</TableCell>
                    <TableCell className="text-ink">{order.customerName}</TableCell>
                    <TableCell className="text-right font-medium text-ink">{formatPrice(order.totalAmount)}</TableCell>
                    <TableCell className="text-center">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_COLORS[order.status] ?? 'bg-gray-100 text-gray-700'}`}>
                        {STATUS_LABELS[order.status] ?? order.status}
                      </span>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{formatDate(order.createdAt)}</TableCell>
                    <TableCell className="text-muted-foreground">{order.completedAt ? formatDate(order.completedAt) : '—'}</TableCell>
                    <TableCell className="text-center">
                      <div className="flex justify-center items-center gap-2">
                        {order.status === 'processing' ? (
                          <Button
                            variant="outline"
                            size="sm"
                            className="text-xs gap-1"
                            disabled={isProcessing}
                            onClick={() => setShowShipModal(order)}
                          >
                            <Truck className="size-3.5" />
                            Giao hàng
                          </Button>
                        ) : nextStatus ? (
                          <Button
                            variant="outline"
                            size="sm"
                            className="text-xs gap-1"
                            disabled={isProcessing}
                            onClick={() => handleAdvanceStatus(order.id, nextStatus)}
                          >
                            <ArrowRight className="size-3.5" />
                            {STATUS_LABELS[nextStatus]}
                          </Button>
                        ) : order.status === 'completed' ? (
                          <span className="inline-flex items-center gap-1 text-xs text-emerald-600">
                            <CheckCircle2 className="size-3.5" />
                            Xong
                          </span>
                        ) : null}

                        {['pending', 'confirmed', 'processing'].includes(order.status) && (
                          <Button
                            variant="destructive"
                            size="sm"
                            className="text-xs"
                            disabled={isProcessing}
                            onClick={async () => {
                              if (!confirm('Bạn có chắc muốn hủy đơn hàng này?')) return
                              try {
                                setProcessingId(order.id)
                                await updateOrderStatus(order.id, 'cancelled')
                                invalidateAdminDashboardQueries()
                                fetchOrders()
                              } catch {
                                // silent
                              } finally {
                                setProcessingId(null)
                              }
                            }}
                          >
                            Hủy
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        )}
      </Card>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between text-sm text-muted-foreground">
        <div className="flex items-center gap-3">
          <span>
            Hiển thị {startItem}-{endItem} / {totalItem} đơn hàng
          </span>
          <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} disabled={loading} />
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={loading || page <= 1}
            onClick={() => setPage((value) => Math.max(1, value - 1))}
          >
            <ChevronLeft className="size-4" />
            Trước
          </Button>
          <span className="min-w-24 text-center text-ink">
            Trang {page} / {totalPage}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={loading || page >= totalPage}
            onClick={() => setPage((value) => Math.min(totalPage, value + 1))}
          >
            Sau
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </div>

      {showShipModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={() => setShowShipModal(null)}>
          <div
            className="bg-white rounded-xl shadow-xl p-6 w-full max-w-md space-y-4"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="text-lg font-semibold text-ink">Tạo vận đơn</h2>
            <p className="text-sm text-muted-foreground">Đơn hàng: {showShipModal.orderCode}</p>
            <div className="space-y-3">
              <div>
                <label className="block text-sm font-medium text-ink mb-1" htmlFor="ship-carrier">Đơn vị vận chuyển</label>
                <input
                  id="ship-carrier"
                  type="text"
                  value={carrier}
                  onChange={(e) => setCarrier(e.target.value)}
                  placeholder="VD: GHN, GHTK, Viettel Post"
                  className="w-full px-3 py-2 border rounded-lg text-sm outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-ink mb-1" htmlFor="ship-tracking">Mã vận đơn</label>
                <input
                  id="ship-tracking"
                  type="text"
                  value={trackingNumber}
                  onChange={(e) => setTrackingNumber(e.target.value)}
                  placeholder="Nhập mã vận đơn"
                  className="w-full px-3 py-2 border rounded-lg text-sm outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                />
              </div>
            </div>
            <div className="flex gap-2 justify-end pt-2">
              <Button variant="outline" onClick={() => setShowShipModal(null)}>Hủy</Button>
              <Button onClick={handleShip} disabled={processingId === showShipModal.id}>
                {processingId === showShipModal.id ? 'Đang xử lý...' : 'Xác nhận giao hàng'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
