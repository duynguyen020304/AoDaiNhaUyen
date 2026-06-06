import type { TopProduct } from '@/types/dashboard'

interface TopProductsListProps {
  products: TopProduct[]
  loading?: boolean
}

function formatCurrency(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}tr`
  return n.toLocaleString('vi-VN')
}

export function TopProductsList({ products, loading }: TopProductsListProps) {
  if (loading) {
    return (
      <div className="bg-white rounded-xl border border-border p-6 animate-pulse">
        <div className="h-5 bg-muted rounded w-40 mb-4" />
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="flex items-center gap-3 my-3">
            <div className="size-10 bg-muted rounded" />
            <div className="flex-1 space-y-1">
              <div className="h-4 bg-muted rounded w-32" />
              <div className="h-3 bg-muted rounded w-20" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  return (
    <div className="bg-white rounded-xl border border-border p-6">
      <h3 className="text-sm font-semibold text-ink mb-4">Sản phẩm bán chạy</h3>
      {products.length === 0 ? (
        <div className="py-8 text-center text-sm text-muted-foreground">
          Chưa có dữ liệu
        </div>
      ) : (
        <div className="space-y-1">
          {products.map((product, index) => (
            <div
              key={product.productId ?? `product-${index}`}
              className="flex items-center gap-3 py-2.5"
            >
              <span className="text-xs font-bold text-muted-foreground w-5">
                {index + 1}
              </span>
              {product.imageUrl ? (
                <img
                  src={product.imageUrl}
                  alt={product.productName}
                  className="size-10 rounded object-cover"
                />
              ) : (
                <div className="size-10 rounded bg-muted flex items-center justify-center">
                  <span className="text-xs text-muted-foreground">...</span>
                </div>
              )}
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium text-ink truncate">
                  {product.productName}
                </div>
                <div className="text-xs text-muted-foreground">
                  Đã bán {product.soldCount} - {formatCurrency(product.revenue)} ₫
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
