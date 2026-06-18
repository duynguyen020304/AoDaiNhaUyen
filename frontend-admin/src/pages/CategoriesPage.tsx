import { useState, useEffect } from 'react'
import { Search, Plus, Pencil, Trash2, RotateCcw, Loader2, FolderTree, Eye, EyeOff } from 'lucide-react'
import { useCategoryStore } from '@/stores/categoryStore'
import type { CategoryListItem } from '@/types/admin'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { CategoryFormModal } from '@/components/admin/CategoryFormModal'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('vi-VN')
}

export function CategoriesPage() {
  const {
    categories, loading, error, includeDeleted,
    fetchCategories, deleteCategory, restoreCategory, setIncludeDeleted, clearError,
  } = useCategoryStore()

  const [searchInput, setSearchInput] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editCategory, setEditCategory] = useState<CategoryListItem | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<CategoryListItem | null>(null)
  const [restoreTarget, setRestoreTarget] = useState<CategoryListItem | null>(null)

  useEffect(() => {
    fetchCategories()
  }, [fetchCategories])

  function handleToggleDeleted() {
    const next = !includeDeleted
    setIncludeDeleted(next)
    fetchCategories()
  }

  function handleSearchInput(value: string) {
    setSearchInput(value)
  }

  function openCreate() {
    setEditCategory(null)
    setFormOpen(true)
  }

  function openEdit(cat: CategoryListItem) {
    setEditCategory(cat)
    setFormOpen(true)
  }

  async function handleDelete() {
    if (!deleteTarget) return
    await deleteCategory(deleteTarget.id)
    setDeleteTarget(null)
  }

  async function handleRestore() {
    if (!restoreTarget) return
    await restoreCategory(restoreTarget.id)
    setRestoreTarget(null)
  }

  function parentName(id: string | null): string {
    if (!id) return '—'
    const parent = categories.find((c) => c.id === id)
    return parent?.name ?? '—'
  }

  const filtered = searchInput
    ? categories.filter((c) =>
        c.name.toLowerCase().includes(searchInput.toLowerCase()) ||
        c.slug.toLowerCase().includes(searchInput.toLowerCase())
      )
    : categories

  return (
    <div>
      <div className="flex items-center justify-between gap-4 mb-6">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Quản lý danh mục</h1>
        <Button onClick={openCreate}>
          <Plus className="size-4" />
          Thêm danh mục
        </Button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span className="flex-1">{error}</span>
          <button onClick={clearError} className="underline shrink-0">Đóng</button>
        </div>
      )}

      {/* Search bar + toggle */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9 w-60"
            placeholder="Tìm theo tên hoặc slug..."
            value={searchInput}
            onChange={(e) => handleSearchInput(e.target.value)}
          />
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
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Tên danh mục</TableHead>
              <TableHead>Slug</TableHead>
              <TableHead>Danh mục cha</TableHead>
              <TableHead>Sản phẩm</TableHead>
              <TableHead>Thứ tự</TableHead>
              <TableHead>Ngày tạo</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && categories.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-12 text-muted-foreground">
                  <Loader2 className="size-6 animate-spin mx-auto mb-2 text-primary" />
                  Đang tải...
                </TableCell>
              </TableRow>
            ) : filtered.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-12 text-muted-foreground">
                  <FolderTree className="size-8 mx-auto mb-2 opacity-40" />
                  Không có danh mục nào
                </TableCell>
              </TableRow>
            ) : (
              filtered.map((cat) => (
                  <TableRow key={cat.id} className={cat.isDeleted ? 'opacity-60 bg-muted/30' : ''}>
                    <TableCell className="font-medium">
                      {cat.name}
                      {cat.isDeleted && <Badge variant="outline" className="ml-2 text-xs text-destructive border-destructive/40">Đã xóa</Badge>}
                    </TableCell>
                    <TableCell className="text-muted-foreground">{cat.slug}</TableCell>
                    <TableCell className="text-muted-foreground">{parentName(cat.parent)}</TableCell>
                    <TableCell className="font-mono tabular-nums">{cat.productCount}</TableCell>
                    <TableCell className="font-mono tabular-nums">{cat.sortOrder}</TableCell>
                    <TableCell>{formatDate(cat.createdAt)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-1">
                        {cat.isDeleted ? (
                          <Button variant="ghost" size="icon" onClick={() => setRestoreTarget(cat)} aria-label="Khôi phục" title="Khôi phục">
                            <RotateCcw className="size-4 text-green-600" />
                          </Button>
                        ) : (
                          <>
                            <Button variant="ghost" size="icon" onClick={() => openEdit(cat)} aria-label="Chỉnh sửa">
                              <Pencil className="size-4" />
                            </Button>
                            <Button variant="ghost" size="icon" onClick={() => setDeleteTarget(cat)} aria-label="Xóa">
                              <Trash2 className="size-4 text-destructive" />
                            </Button>
                          </>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>

      {/* Modals */}
      <CategoryFormModal
        key={editCategory?.id ?? 'new'}
        open={formOpen}
        onClose={() => setFormOpen(false)}
        category={editCategory}
      />
      <DeleteConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Xóa danh mục"
        message={`Bạn có chắc muốn xóa "${deleteTarget?.name}"? Sản phẩm trong danh mục này sẽ không bị ảnh hưởng.`}
      />
      <DeleteConfirmModal
        open={!!restoreTarget}
        onClose={() => setRestoreTarget(null)}
        onConfirm={handleRestore}
        title="Khôi phục danh mục"
        message={`Bạn có muốn khôi phục "${restoreTarget?.name}"?`}
      />
    </div>
  )
}
