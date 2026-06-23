import { useState, useEffect, useRef, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Pencil, Trash2, RotateCcw, ChevronLeft, ChevronRight, Search, Package, Eye, EyeOff, Loader2, Globe, FileX } from 'lucide-react'
import { useProductStore } from '@/stores/productStore'
import type { AdminProductListItem } from '@/types/admin'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { PageSizeSelect } from '@/components/admin/PageSizeSelect'

function statusBadge(status: string) {
  switch (status) {
    case 'active': return <Badge variant="success">Đang bán</Badge>
    case 'draft': return <Badge variant="warning">Bản nháp</Badge>
    case 'inactive': return <Badge className="bg-gray-100 text-gray-500 border border-gray-200">Ngừng bán</Badge>
    case 'out_of_stock': return <Badge variant="outline" className="border-orange-300 text-orange-600">Hết hàng</Badge>
    default: return <Badge variant="outline">{status}</Badge>
  }
}

function typeBadge(productType: string) {
  switch (productType) {
    case 'ao_dai': return <Badge variant="outline" className="border-burgundy/30 text-burgundy">Áo dài</Badge>
    case 'phu_kien': return <Badge variant="outline" className="border-burgundy/30 text-burgundy">Phụ kiện</Badge>
    default: return <Badge variant="outline">{productType}</Badge>
  }
}

export function ProductListPage() {
  const {
    products, totalPages, totalItems, currentPage, pageSize, search, statusFilter, includeDeleted, loading, error,
    fetchProducts, deleteProduct, restoreProduct, setSearch, setStatusFilter, setIncludeDeleted, setPageSize, clearError,
  } = useProductStore()

  const [searchInput, setSearchInput] = useState(search)
  const [productTypeFilter, setProductTypeFilter] = useState('')
  const searchTimer = useRef<ReturnType<typeof setTimeout>>(null)
  const [deleteTarget, setDeleteTarget] = useState<AdminProductListItem | null>(null)
  const [restoreTarget, setRestoreTarget] = useState<AdminProductListItem | null>(null)

  useEffect(() => {
    fetchProducts()
  }, [fetchProducts])

  const filteredProducts = useMemo(() => {
    let result = products
    if (productTypeFilter) result = result.filter((p) => p.productType === productTypeFilter)
    return result
  }, [products, productTypeFilter])

  function handleToggleDeleted() {
    const next = !includeDeleted
    setIncludeDeleted(next)
    fetchProducts(undefined, 1)
  }

  function handleSearchInput(value: string) {
    setSearchInput(value)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => {
      setSearch(value)
      fetchProducts(value, 1)
    }, 300)
  }

  function handlePageChange(page: number) {
    fetchProducts(undefined, page)
  }

  function handlePageSizeChange(nextPageSize: number) {
    setPageSize(nextPageSize)
    queueMicrotask(() => fetchProducts(undefined, 1))
  }

  async function handleDelete() {
    if (!deleteTarget) return
    await deleteProduct(deleteTarget.id)
    setDeleteTarget(null)
  }

  async function handleRestore() {
    if (!restoreTarget) return
    await restoreProduct(restoreTarget.id)
    setRestoreTarget(null)
  }

  async function handleToggleStatus(product: AdminProductListItem) {
    const newStatus = product.status === 'active' ? 'draft' : 'active'
    await useProductStore.getState().toggleProductStatus(product.id, newStatus)
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Sản phẩm</h1>
        <Link to="/admin/products/new" className="inline-flex items-center gap-2 rounded-lg h-9 px-4 py-2 font-medium text-sm bg-gold text-ink font-semibold hover:bg-gold/90 transition-colors focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 active:translate-y-px">
          <Plus className="size-4" />
          Thêm sản phẩm
        </Link>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive mb-4">
          <span className="flex-1">{error}</span>
          <button onClick={clearError} className="underline shrink-0">Đóng</button>
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <Select
          className="w-40"
          value={productTypeFilter}
          onChange={(e) => setProductTypeFilter(e.target.value)}
        >
          <option value="">Tất cả loại</option>
          <option value="ao_dai">Áo dài</option>
          <option value="phu_kien">Phụ kiện</option>
        </Select>
        <Select
          className="w-40"
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); fetchProducts(undefined, 1) }}
        >
          <option value="">Tất cả trạng thái</option>
          <option value="active">Đang bán</option>
          <option value="draft">Bản nháp</option>
          <option value="inactive">Ngừng bán</option>
          <option value="out_of_stock">Hết hàng</option>
        </Select>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input className="pl-9 w-60" placeholder="Tìm theo tên..." value={searchInput} onChange={(e) => handleSearchInput(e.target.value)} />
        </div>
        <Button
          variant={includeDeleted ? 'default' : 'outline'}
          size="sm"
          onClick={handleToggleDeleted}
          className="gap-1.5 shrink-0"
        >
          {includeDeleted ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
          {includeDeleted ? 'Ẩn đã xóa' : 'Hiện đã xóa'}
        </Button>
      </div>

      {/* Table */}
      <Card className="overflow-hidden">
        {loading && filteredProducts.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <Loader2 className="size-8 animate-spin text-primary mb-2" />
            <p className="text-muted-foreground">Đang tải...</p>
          </div>
        ) : filteredProducts.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <Package className="size-12 text-muted-foreground mb-4" />
            <p className="text-muted-foreground mb-2">Chưa có sản phẩm nào</p>
            <Link to="/admin/products/new" className="inline-flex items-center gap-2 rounded-lg h-9 px-4 py-2 font-medium text-sm bg-gold text-ink font-semibold hover:bg-gold/90 transition-colors focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 active:translate-y-px">
              Thêm sản phẩm đầu tiên
            </Link>
          </div>
        ) : (
          <Table>
            <TableHeader className="bg-burgundy [&_th]:text-white [&_th]:font-medium">
              <TableRow>
                <TableHead>Tên sản phẩm</TableHead>
                <TableHead>Loại</TableHead>
                <TableHead>Danh mục</TableHead>
                <TableHead>Biến thể</TableHead>
                <TableHead>Tồn kho</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Nổi bật</TableHead>
                <TableHead className="w-[100px] text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredProducts.map((p, i) => (
                <TableRow key={p.id} className={`${i % 2 === 0 ? 'bg-white' : 'bg-cream/50'} ${p.isDeleted ? 'opacity-60 bg-muted/30' : ''}`}>
                  <TableCell>
                    <div className="font-medium text-ink">
                      {p.name}
                      {p.isDeleted && <Badge variant="outline" className="ml-2 text-xs text-destructive border-destructive/40">Đã xóa</Badge>}
                    </div>
                    <div className="text-xs text-muted-foreground">{p.slug}</div>
                  </TableCell>
                  <TableCell>{typeBadge(p.productType)}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{p.categoryName || '—'}</TableCell>
                  <TableCell className="font-mono tabular-nums">{p.variantCount}</TableCell>
                  <TableCell className="font-mono tabular-nums">
                    <span className={p.totalStock <= 0 ? 'text-destructive font-semibold' : p.totalStock <= 5 ? 'text-orange-600 font-semibold' : ''}>
                      {p.totalStock}
                    </span>
                  </TableCell>
                  <TableCell>{p.isDeleted ? <span className="text-xs text-muted-foreground">—</span> : statusBadge(p.status)}</TableCell>
                  <TableCell>{p.isFeatured ? <Badge variant="success">Nổi bật</Badge> : '—'}</TableCell>
                  <TableCell className="text-right">
                    <div className="flex gap-1 justify-end">
                      {p.isDeleted ? (
                        <Button variant="ghost" size="icon" onClick={() => setRestoreTarget(p)} aria-label="Khôi phục" title="Khôi phục">
                          <RotateCcw className="size-4 text-green-600" />
                        </Button>
                      ) : (
                        <>
                          <Button variant="ghost" size="icon" className="size-8" onClick={() => handleToggleStatus(p)} aria-label={p.status === 'active' ? 'Gỡ bán' : 'Đăng bán'} title={p.status === 'active' ? 'Gỡ bán' : 'Đăng bán'}>
                            {p.status === 'active' ? <FileX className="size-4" /> : <Globe className="size-4" />}
                          </Button>
                          <Link to={`/admin/products/${p.id}/edit`} aria-label={`Sửa ${p.name}`} className="inline-flex items-center justify-center size-8 rounded-lg hover:bg-muted transition-colors">
                            <Pencil className="size-4" />
                          </Link>
                          <Button variant="ghost" size="icon" className="size-8 text-destructive hover:text-destructive" onClick={() => setDeleteTarget(p)} aria-label={`Xóa ${p.name}`}>
                            <Trash2 className="size-4" />
                          </Button>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      {/* Pagination */}
      {totalItems > 0 && (
        <div className="flex flex-col gap-3 text-sm text-muted-foreground mt-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <span>Tổng: {totalItems} sản phẩm</span>
            <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} disabled={loading} />
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              disabled={currentPage <= 1}
              onClick={() => handlePageChange(currentPage - 1)}
              aria-label="Trang trước"
            >
              <ChevronLeft className="size-4" />
            </Button>
            <span>Trang {currentPage} / {totalPages}</span>
            <Button
              variant="outline"
              size="icon"
              disabled={currentPage >= totalPages}
              onClick={() => handlePageChange(currentPage + 1)}
              aria-label="Trang sau"
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Delete confirmation dialog */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="fixed inset-0 bg-black/40" onClick={() => setDeleteTarget(null)} />
          <div className="relative bg-white rounded-xl shadow-lg p-6 w-full max-w-sm mx-4">
            <h2 className="text-lg font-semibold mb-2">Xác nhận xóa</h2>
            <p className="text-muted-foreground text-sm mb-4">Bạn có chắc muốn xóa sản phẩm "{deleteTarget.name}"?</p>
            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={() => setDeleteTarget(null)}>Hủy</Button>
              <Button variant="destructive" onClick={handleDelete}>
                <Trash2 className="size-4" /> Xóa
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Restore confirmation dialog */}
      {restoreTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="fixed inset-0 bg-black/40" onClick={() => setRestoreTarget(null)} />
          <div className="relative bg-white rounded-xl shadow-lg p-6 w-full max-w-sm mx-4">
            <h2 className="text-lg font-semibold mb-2">Khôi phục sản phẩm</h2>
            <p className="text-muted-foreground text-sm mb-4">Bạn có muốn khôi phục "{restoreTarget.name}"?</p>
            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={() => setRestoreTarget(null)}>Hủy</Button>
              <Button onClick={handleRestore}>
                <RotateCcw className="size-4" /> Khôi phục
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
