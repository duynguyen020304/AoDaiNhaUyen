import type { RecentOrder } from '@/types/dashboard'

interface RecentOrdersTableProps {
  orders: RecentOrder[]
  loading?: boolean
}

const STATUS_LABELS: Record<string, string> = {
  pending: 'Chờ xác nhận',
  confirmed: 'Đã xác nhận',
  processing: 'Đang xử lý',
  shipping: 'Đang giao',
  completed: 'Hoàn thành',
  cancelled: 'Đã hủy',
  returned: 'Trả hàng',
}

const STATUS_CLASSES: Record<string, string> = {
  pending: 'bg-amber-100 text-amber-700',
  confirmed: 'bg-blue-100 text-blue-700',
  processing: 'bg-purple-100 text-purple-700',
  shipping: 'bg-indigo-100 text-indigo-700',
  completed: 'bg-green-100 text-green-700',
  cancelled: 'bg-red-100 text-red-700',
  returned: 'bg-gray-100 text-gray-700',
}

function formatCurrency(n: number): string {
  return `${n.toLocaleString('vi-VN')} ₫`
}

function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
}

export function RecentOrdersTable({ orders, loading }: RecentOrdersTableProps) {
  if (loading) {
    return (
      <div className="bg-white rounded-xl border border-border p-6 animate-pulse">
        <div className="h-5 bg-muted rounded w-40 mb-4" />
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="h-10 bg-muted rounded my-2" />
        ))}
      </div>
    )
  }

  return (
    <div className="bg-white rounded-xl border border-border p-6">
      <h3 className="text-sm font-semibold text-ink mb-4">Đơn hàng gần đây</h3>
      {orders.length === 0 ? (
        <div className="py-8 text-center text-sm text-muted-foreground">
          Chưa có đơn hàng nào
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left">
                <th className="pb-3 font-medium text-muted-foreground">Mã ĐH</th>
                <th className="pb-3 font-medium text-muted-foreground">Khách hàng</th>
                <th className="pb-3 font-medium text-muted-foreground text-right">Tổng tiền</th>
                <th className="pb-3 font-medium text-muted-foreground">Trạng thái</th>
                <th className="pb-3 font-medium text-muted-foreground text-right">Ngày tạo</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr key={order.orderCode} className="border-b border-border/50 last:border-0">
                  <td className="py-3 font-medium text-ink">{order.orderCode}</td>
                  <td className="py-3 text-ink">{order.customerName}</td>
                  <td className="py-3 text-right font-medium">{formatCurrency(order.totalAmount)}</td>
                  <td className="py-3">
                    <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_CLASSES[order.status] ?? 'bg-gray-100 text-gray-700'}`}>
                      {STATUS_LABELS[order.status] ?? order.status}
                    </span>
                  </td>
                  <td className="py-3 text-right text-muted-foreground">{formatDate(order.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
