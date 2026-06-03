import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Pencil, Trash2, ChevronUp, ChevronDown, Search, Package } from 'lucide-react'
import { useProducts } from '@/hooks/useProducts'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'

function statusBadge(status: string) {
  switch (status) {
    case 'active': return <Badge variant="success">Đang bán</Badge>
    case 'draft': return <Badge variant="warning">Bản nháp</Badge>
    case 'inactive': return <Badge className="bg-gray-100 text-gray-500 border border-gray-200">Ngừng bán</Badge>
    default: return null
  }
}

export function ProductListPage() {
  const { products, allProducts, deleteProduct, reorderProducts, filterType, setFilterType, filterStatus, setFilterStatus, search, setSearch } = useProducts()
  const [confirmId, setConfirmId] = useState<string | null>(null)

  const handleDelete = () => {
    if (confirmId) {
      deleteProduct(confirmId)
      setConfirmId(null)
    }
  }

  const moveUp = (id: string) => {
    const items = [...products]
    const idx = items.findIndex(p => p.id === id)
    if (idx <= 0) return
    const prev = items[idx - 1]
    const current = items[idx]
    reorderProducts([
      { id: current.id, sortOrder: prev.sortOrder },
      { id: prev.id, sortOrder: current.sortOrder },
    ])
  }

  const moveDown = (id: string) => {
    const items = [...products]
    const idx = items.findIndex(p => p.id === id)
    if (idx < 0 || idx >= items.length - 1) return
    const next = items[idx + 1]
    const current = items[idx]
    reorderProducts([
      { id: current.id, sortOrder: next.sortOrder },
      { id: next.id, sortOrder: current.sortOrder },
    ])
  }

  const showReorder = filterType === 'phu-kien'

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

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-4">
        <Select className="w-40" value={filterType} onChange={e => setFilterType(e.target.value)}>
          <option value="all">Tất cả loại</option>
          <option value="ao-dai">Áo dài</option>
          <option value="phu-kien">Phụ kiện</option>
        </Select>
        <Select className="w-44" value={filterStatus} onChange={e => setFilterStatus(e.target.value)}>
          <option value="all">Tất cả trạng thái</option>
          <option value="active">Đang bán</option>
          <option value="draft">Bản nháp</option>
          <option value="inactive">Ngừng bán</option>
        </Select>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input className="pl-9 w-60" placeholder="Tìm theo tên..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        {showReorder && <span className="text-sm text-muted-foreground self-center ml-auto">Dùng mũi tên ↑↓ để sắp xếp thứ tự hiển thị</span>}
      </div>

      {/* Table */}
      <Card className="overflow-hidden">
        {products.length === 0 ? (
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
                <TableHead className="w-[80px]">Ảnh</TableHead>
                <TableHead>Tên sản phẩm</TableHead>
                <TableHead>Loại</TableHead>
                <TableHead>Giá gốc</TableHead>
                <TableHead>Tồn kho</TableHead>
                <TableHead>Trạng thái</TableHead>
                {showReorder && <TableHead className="w-[120px]">Thứ tự</TableHead>}
                <TableHead className="w-[100px] text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {products.map((p, i) => (
                <TableRow key={p.id} className={i % 2 === 0 ? 'bg-white' : 'bg-cream/50'}>
                  <TableCell>
                    {p.images[0] ? (
                      <img src={p.images[0].url} alt={p.name} className="size-14 object-cover rounded-md" />
                    ) : (
                      <div className="size-14 rounded-md bg-muted flex items-center justify-center">
                        <Package className="size-6 text-muted-foreground" />
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="font-medium text-ink">{p.name}</div>
                    <div className="text-xs text-muted-foreground">{p.slug}</div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className="border-burgundy/30 text-burgundy">
                      {p.type === 'ao-dai' ? 'Áo dài' : 'Phụ kiện'}
                    </Badge>
                  </TableCell>
                  <TableCell className="font-mono tabular-nums">
                    {p.variants[0]?.price.toLocaleString('vi-VN')}₫
                  </TableCell>
                  <TableCell>
                    {p.variants.reduce((sum, v) => sum + v.stockQty, 0)}
                  </TableCell>
                  <TableCell>{statusBadge(p.status)}</TableCell>
                  {showReorder && (
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button variant="ghost" size="icon" className="size-7" onClick={() => moveUp(p.id)}>
                          <ChevronUp className="size-3" />
                        </Button>
                        <span className="w-6 text-center tabular-nums text-sm">{p.sortOrder}</span>
                        <Button variant="ghost" size="icon" className="size-7" onClick={() => moveDown(p.id)}>
                          <ChevronDown className="size-3" />
                        </Button>
                      </div>
                    </TableCell>
                  )}
                  <TableCell className="text-right">
                    <div className="flex gap-1 justify-end">
                      <Link to={`/admin/products/${p.id}/edit`} aria-label={`Sửa ${p.name}`} className="inline-flex items-center justify-center size-8 rounded-lg hover:bg-muted transition-colors">
                        <Pencil className="size-4" />
                      </Link>
                      <Button variant="ghost" size="icon" className="size-8 text-destructive hover:text-destructive" onClick={() => setConfirmId(p.id)} aria-label={`Xóa ${p.name}`}>
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      {/* Delete confirmation dialog */}
      {confirmId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="fixed inset-0 bg-black/40" onClick={() => setConfirmId(null)} />
          <div className="relative bg-white rounded-xl shadow-lg p-6 w-full max-w-sm mx-4">
            <h2 className="text-lg font-semibold mb-2">Xác nhận xóa</h2>
            <p className="text-muted-foreground text-sm mb-4">Bạn có chắc muốn xóa sản phẩm "{allProducts.find(p => p.id === confirmId)?.name}"? Hành động này không thể hoàn tác.</p>
            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={() => setConfirmId(null)}>Hủy</Button>
              <Button variant="destructive" onClick={handleDelete}>
                <Trash2 className="size-4" /> Xóa
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
