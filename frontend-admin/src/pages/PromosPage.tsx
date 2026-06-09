import { useEffect, useRef, useState } from 'react'
import { ChevronLeft, ChevronRight, Eye, EyeOff, Loader2, Pencil, Plus, RotateCcw, Search, Tag, Trash2 } from 'lucide-react'
import { PromoFormModal } from '@/components/admin/PromoFormModal'
import { usePromoStore } from '@/stores/promoStore'
import type { AdminPromoItem } from '@/types/admin'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

const currency = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

function formatDiscount(promo: AdminPromoItem) {
  return promo.discountType === 'percentage' ? `${promo.discountValue}%` : currency.format(promo.discountValue)
}

function statusBadge(promo: AdminPromoItem) {
  if (promo.isDeleted) return <Badge variant="outline" className="border-destructive/40 text-destructive">Đã xóa</Badge>
  return promo.isActive ? <Badge variant="success">Đang hoạt động</Badge> : <Badge variant="warning">Đã tắt</Badge>
}

export function PromosPage() {
  const { promos, totalPages, totalItems, currentPage, search, activeFilter, includeDeleted, loading, error, fetchPromos, deletePromo, restorePromo, togglePromoStatus, setSearch, setActiveFilter, setIncludeDeleted, clearError } = usePromoStore()
  const [searchInput, setSearchInput] = useState(search)
  const [deleteTarget, setDeleteTarget] = useState<AdminPromoItem | null>(null)
  const [restoreTarget, setRestoreTarget] = useState<AdminPromoItem | null>(null)
  const [editingPromo, setEditingPromo] = useState<AdminPromoItem | null>(null)
  const [showFormModal, setShowFormModal] = useState(false)
  const searchTimer = useRef<ReturnType<typeof setTimeout>>(null)

  useEffect(() => { fetchPromos() }, [fetchPromos])

  function handleSearchInput(value: string) {
    setSearchInput(value)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => {
      setSearch(value)
      fetchPromos({ search: value, page: 1 })
    }, 300)
  }

  function handleActiveFilter(value: string) {
    setActiveFilter(value)
    fetchPromos({ activeFilter: value, page: 1 })
  }

  function handleToggleDeleted() {
    setIncludeDeleted(!includeDeleted)
    setTimeout(() => fetchPromos({ page: 1 }), 0)
  }

  async function handleDelete() {
    if (!deleteTarget) return
    await deletePromo(deleteTarget.id)
    setDeleteTarget(null)
  }

  async function handleRestore() {
    if (!restoreTarget) return
    await restorePromo(restoreTarget.id)
    setRestoreTarget(null)
  }

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Mã giảm giá</h1>
          <p className="text-sm text-muted-foreground mt-0.5">Quản lý mã khuyến mãi</p>
        </div>
        <Button onClick={() => { setEditingPromo(null); setShowFormModal(true) }} className="gap-2">
          <Plus className="size-4" />
          Tạo mã mới
        </Button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive mb-4">
          <span className="flex-1">{error}</span>
          <button type="button" onClick={clearError} className="underline shrink-0">Đóng</button>
        </div>
      )}

      <div className="flex flex-wrap items-center gap-3 mb-4">
        <Select className="w-44" value={activeFilter} onChange={(e) => handleActiveFilter(e.target.value)} aria-label="Lọc trạng thái">
          <option value="">Tất cả trạng thái</option>
          <option value="active">Đang hoạt động</option>
          <option value="inactive">Đã tắt</option>
        </Select>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input className="pl-9 w-64" placeholder="Tìm theo mã..." value={searchInput} onChange={(e) => handleSearchInput(e.target.value)} aria-label="Tìm mã giảm giá" />
        </div>
        <Button variant={includeDeleted ? 'default' : 'outline'} size="sm" onClick={handleToggleDeleted} className="gap-1.5 shrink-0">
          {includeDeleted ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
          {includeDeleted ? 'Ẩn đã xóa' : 'Hiện đã xóa'}
        </Button>
      </div>

      <Card className="overflow-hidden">
        {loading && promos.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <Loader2 className="size-8 animate-spin text-primary mb-2" />
            <p className="text-muted-foreground">Đang tải...</p>
          </div>
        ) : promos.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <Tag className="size-12 text-muted-foreground mb-4" />
            <p className="text-muted-foreground mb-2">Chưa có mã giảm giá nào</p>
            <Button onClick={() => { setEditingPromo(null); setShowFormModal(true) }}>Tạo mã đầu tiên</Button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader className="bg-burgundy [&_th]:text-white [&_th]:font-medium">
                <TableRow>
                  <TableHead>Mã</TableHead>
                  <TableHead>Loại</TableHead>
                  <TableHead>Giá trị</TableHead>
                  <TableHead>Đơn tối thiểu</TableHead>
                  <TableHead>Đã dùng / tối đa</TableHead>
                  <TableHead>Hạn sử dụng</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead className="w-[120px] text-right">Thao tác</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {promos.map((promo, i) => (
                  <TableRow key={promo.id} className={`${i % 2 === 0 ? 'bg-white' : 'bg-cream/50'} ${promo.isDeleted ? 'opacity-60 bg-muted/30' : ''}`}>
                    <TableCell className="font-mono font-semibold text-ink">{promo.code}</TableCell>
                    <TableCell>{promo.discountType === 'percentage' ? 'Phần trăm' : 'Số tiền'}</TableCell>
                    <TableCell>{formatDiscount(promo)}</TableCell>
                    <TableCell>{currency.format(promo.minOrderAmount)}</TableCell>
                    <TableCell className="font-mono tabular-nums">{promo.currentUses} / {promo.maxUses === 0 ? '∞' : promo.maxUses}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{formatDate(promo.startDate)} - {formatDate(promo.endDate)}</TableCell>
                    <TableCell>{statusBadge(promo)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex gap-1 justify-end">
                        {promo.isDeleted ? (
                          <Button variant="ghost" size="icon" className="size-8" onClick={() => setRestoreTarget(promo)} aria-label={`Khôi phục ${promo.code}`}>
                            <RotateCcw className="size-4 text-green-600" />
                          </Button>
                        ) : (
                          <>
                            <Button variant="ghost" size="icon" className="size-8" onClick={() => togglePromoStatus(promo.id, !promo.isActive)} aria-label={promo.isActive ? `Tắt ${promo.code}` : `Kích hoạt ${promo.code}`}>
                              {promo.isActive ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                            </Button>
                            <Button variant="ghost" size="icon" className="size-8" onClick={() => { setEditingPromo(promo); setShowFormModal(true) }} aria-label={`Sửa ${promo.code}`}>
                              <Pencil className="size-4" />
                            </Button>
                            <Button variant="ghost" size="icon" className="size-8 text-destructive hover:text-destructive" onClick={() => setDeleteTarget(promo)} aria-label={`Xóa ${promo.code}`}>
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
          </div>
        )}
      </Card>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-muted-foreground mt-4">
          <span>Tổng: {totalItems} mã</span>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="icon" disabled={currentPage <= 1} onClick={() => fetchPromos({ page: currentPage - 1 })} aria-label="Trang trước"><ChevronLeft className="size-4" /></Button>
            <span>Trang {currentPage} / {totalPages}</span>
            <Button variant="outline" size="icon" disabled={currentPage >= totalPages} onClick={() => fetchPromos({ page: currentPage + 1 })} aria-label="Trang sau"><ChevronRight className="size-4" /></Button>
          </div>
        </div>
      )}

      <PromoFormModal
        key={editingPromo?.id ?? 'new'}
        open={showFormModal}
        promo={editingPromo}
        onClose={() => {
          setShowFormModal(false)
          setEditingPromo(null)
        }}
      />

      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="fixed inset-0 bg-black/40" onClick={() => setDeleteTarget(null)} />
          <div className="relative bg-white rounded-xl shadow-lg p-6 w-full max-w-sm mx-4">
            <h2 className="text-lg font-semibold mb-2">Xác nhận xóa</h2>
            <p className="text-muted-foreground text-sm mb-4">Bạn có chắc muốn xóa mã "{deleteTarget.code}"?</p>
            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={() => setDeleteTarget(null)}>Hủy</Button>
              <Button variant="destructive" onClick={handleDelete}><Trash2 className="size-4" /> Xóa</Button>
            </div>
          </div>
        </div>
      )}

      {restoreTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="fixed inset-0 bg-black/40" onClick={() => setRestoreTarget(null)} />
          <div className="relative bg-white rounded-xl shadow-lg p-6 w-full max-w-sm mx-4">
            <h2 className="text-lg font-semibold mb-2">Khôi phục mã</h2>
            <p className="text-muted-foreground text-sm mb-4">Khôi phục mã "{restoreTarget.code}"?</p>
            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={() => setRestoreTarget(null)}>Hủy</Button>
              <Button onClick={handleRestore}><RotateCcw className="size-4" /> Khôi phục</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
