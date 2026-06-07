import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { getLowStockAlerts } from '@/api/inventory'
import type { LowStockAlert } from '@/api/inventory'

export function LowStockAlerts() {
  const navigate = useNavigate()
  const [alerts, setAlerts] = useState<LowStockAlert[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getLowStockAlerts(5)
      .then(setAlerts)
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [])

  if (loading || alerts.length === 0) return null

  return (
    <div className="bg-amber-50 dark:bg-amber-950/20 border-l-4 border-amber-500 rounded-r-lg p-4">
      <div className="flex items-start gap-3">
        <AlertTriangle className="size-5 text-amber-600 dark:text-amber-400 mt-0.5 shrink-0" />
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-2">
            <h3 className="text-sm font-semibold text-amber-900 dark:text-amber-100">
              Sản phẩm sắp hết hàng
            </h3>
            <span className="inline-flex items-center rounded-full bg-amber-200 dark:bg-amber-800 px-2 py-0.5 text-xs font-medium text-amber-800 dark:text-amber-200">
              {alerts.length}
            </span>
          </div>
          <ul className="space-y-1 mb-3">
            {alerts.slice(0, 5).map((alert) => (
              <li
                key={alert.variantId}
                className="text-xs text-amber-800 dark:text-amber-300 flex items-center gap-1.5"
              >
                <span className="font-medium">{alert.productName}</span>
                {alert.size && <span className="text-amber-600">- {alert.size}</span>}
                {alert.color && <span className="text-amber-600">/ {alert.color}</span>}
                <span className="ml-auto text-amber-700 dark:text-amber-400">
                  Còn lại <span className="font-semibold">{alert.stockQty}</span>
                </span>
              </li>
            ))}
          </ul>
          <Button
            variant="outline"
            size="sm"
            className="text-xs border-amber-300 dark:border-amber-700 text-amber-800 dark:text-amber-200 hover:bg-amber-100 dark:hover:bg-amber-900/30"
            onClick={() => navigate('/admin/products')}
          >
            Quản lý kho
          </Button>
        </div>
      </div>
    </div>
  )
}
