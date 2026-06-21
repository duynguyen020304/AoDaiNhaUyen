import { useEffect, useState, useCallback } from 'react'
import { RefreshCw, Truck, CheckCircle2, ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { updateOrderStatus, createShipment } from '@/api/admin'
import { getRecentOrders } from '@/api/dashboard'
import { invalidateAdminDashboardQueries } from '@/queries/invalidateAdminQueries'
import type { RecentOrder } from '@/types/dashboard'

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
]

export function OrdersPage() {
  const [orders, setOrders] = useState<RecentOrder[]>([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState('all')
  const [processingId, setProcessingId] = useState<string | null>(null)
  const [showShipModal, setShowShipModal] = useState<RecentOrder | null>(null)
  const [carrier, setCarrier] = useState('')
  const [trackingNumber, setTrackingNumber] = useState('')

  const fetchOrders = useCallback(() => {
    setLoading(true)
    getRecentOrders(50)
      .then(setOrders)
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    const timeoutId = window.setTimeout(fetchOrders, 0)
    return () => window.clearTimeout(timeoutId)
  }, [fetchOrders])

  const filtered = filter === 'all' ? orders : orders.filter((o) => o.status === filter)

  async function handleAdvanceStatus(orderId: string, nextStatus: string) {
    try {
      setProcessingId(orderId)
      await updateOrderStatus(orderId, nextStatus)
      invalidateAdminDashboardQueries()
      setOrders((prev) =>
        prev.map((o) => (o.id === orderId ? { ...o, status: nextStatus } : o))
      )
    } catch {
      // Error handled silently for demo
    } finally {
      setProcessingId(null)
    }
  }

  async function handleShip() {
    if (!showShipModal) return
    try {
      setProcessingId(showShipModal.id)
      await createShipment(showShipModal.id, carrier || undefined, trackingNumber || undefined)
      invalidateAdminDashboardQueries()
      setOrders((prev) =>
        prev.map((o) => (o.id === showShipModal.id ? { ...o, status: 'shipping' } : o))
      )
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

  return (
    <div className="space-y-6">
      {/* Header */}
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

      {/* Filter tabs */}
      <div className="flex flex-wrap gap-1 bg-muted rounded-lg p-1">
        {FILTER_TABS.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setFilter(tab.key)}
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

      {/* Orders table */}
      <Card className="overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-12 text-muted-foreground text-sm">
            Đang tải...
          </div>
        ) : filtered.length === 0 ? (
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
                <TableHead className="text-center">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((order) => {
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
                                setOrders((prev) =>
                                  prev.map((o) => (o.id === order.id ? { ...o, status: 'cancelled' } : o))
                                )
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

      {/* Ship modal */}
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
