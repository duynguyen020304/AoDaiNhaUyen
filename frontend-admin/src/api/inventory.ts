import { request } from './client'

export interface LowStockAlert {
  variantId: string
  productName: string
  variantName: string | null
  size: string | null
  color: string | null
  sku: string
  stockQty: number
}

export async function getLowStockAlerts(threshold = 5): Promise<LowStockAlert[]> {
  return request<LowStockAlert[]>(`/api/admin/inventory/low-stock?threshold=${threshold}`)
}
