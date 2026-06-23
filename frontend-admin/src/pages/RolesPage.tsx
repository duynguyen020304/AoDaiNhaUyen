import { useState, useEffect } from 'react'
import { Plus, Pencil, Trash2, Loader2, Shield, Search, ChevronLeft, ChevronRight } from 'lucide-react'
import { useRoleStore } from '@/stores/roleStore'
import type { RoleDto } from '@/types/admin'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { RoleFormModal } from '@/components/admin/RoleFormModal'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'
import { PageSizeSelect } from '@/components/admin/PageSizeSelect'

const PROTECTED_ROLES = new Set(['admin', 'customer'])

export function RolesPage() {
  const { roles, loading, error, fetchRoles, deleteRole, clearError } = useRoleStore()

  const [formOpen, setFormOpen] = useState(false)
  const [searchInput, setSearchInput] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [editRole, setEditRole] = useState<RoleDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<RoleDto | null>(null)

  useEffect(() => {
    fetchRoles()
  }, [fetchRoles])

  function openCreate() {
    setEditRole(null)
    setFormOpen(true)
  }

  function openEdit(role: RoleDto) {
    setEditRole(role)
    setFormOpen(true)
  }

  async function handleDelete() {
    if (!deleteTarget) return
    await deleteRole(deleteTarget.id)
  }

  function handleSearch(value: string) {
    setSearchInput(value)
    setPage(1)
  }

  const filtered = searchInput
    ? roles.filter((role) =>
        role.name.toLowerCase().includes(searchInput.toLowerCase()) ||
        (role.description ?? '').toLowerCase().includes(searchInput.toLowerCase())
      )
    : roles
  const totalPage = Math.max(1, Math.ceil(filtered.length / pageSize))
  const safePage = Math.min(page, totalPage)
  const paginated = filtered.slice((safePage - 1) * pageSize, safePage * pageSize)
  const startItem = filtered.length === 0 ? 0 : (safePage - 1) * pageSize + 1
  const endItem = Math.min(safePage * pageSize, filtered.length)

  function handlePageSizeChange(nextPageSize: number) {
    setPageSize(nextPageSize)
    setPage(1)
  }

  return (
    <div>
      <div className="flex items-center justify-between gap-4 mb-6">
        <h1 className="text-2xl font-bold tracking-tight text-ink">Quản lý vai trò</h1>
        <Button onClick={openCreate}>
          <Plus className="size-4" />
          Thêm vai trò
        </Button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span className="flex-1">{error}</span>
          <button onClick={clearError} className="underline shrink-0">Đóng</button>
        </div>
      )}

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9 w-60"
            placeholder="Tìm theo tên hoặc mô tả..."
            value={searchInput}
            onChange={(e) => handleSearch(e.target.value)}
          />
        </div>
      </div>

      <Card className="overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Tên vai trò</TableHead>
              <TableHead>Mô tả</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && roles.length === 0 ? (
              <TableRow>
                <TableCell colSpan={3} className="text-center py-12 text-muted-foreground">
                  <Loader2 className="size-6 animate-spin mx-auto mb-2 text-primary" />
                  Đang tải...
                </TableCell>
              </TableRow>
            ) : filtered.length === 0 ? (
              <TableRow>
                <TableCell colSpan={3} className="text-center py-12 text-muted-foreground">
                  <Shield className="size-8 mx-auto mb-2 opacity-40" />
                  Không có vai trò nào
                </TableCell>
              </TableRow>
            ) : (
              paginated.map((r) => (
                <TableRow key={r.id}>
                  <TableCell className="font-medium">
                    {r.name}
                    {PROTECTED_ROLES.has(r.name) && (
                      <span className="ml-2 text-xs text-muted-foreground">(hệ thống)</span>
                    )}
                  </TableCell>
                  <TableCell className="text-muted-foreground">{r.description ?? '—'}</TableCell>
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button variant="ghost" size="icon" onClick={() => openEdit(r)} aria-label="Chỉnh sửa">
                        <Pencil className="size-4" />
                      </Button>
                      {!PROTECTED_ROLES.has(r.name) && (
                        <Button variant="ghost" size="icon" onClick={() => setDeleteTarget(r)} aria-label="Xóa">
                          <Trash2 className="size-4 text-destructive" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>


      <div className="mt-4 flex flex-col gap-3 text-sm text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
        <span>
          Hiển thị {startItem}-{endItem} / {filtered.length} vai trò
        </span>
        <PageSizeSelect value={pageSize} onChange={handlePageSizeChange} disabled={loading} />
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={loading || safePage <= 1}
            onClick={() => setPage((value) => Math.max(1, value - 1))}
          >
            <ChevronLeft className="size-4" />
            Trước
          </Button>
          <span className="min-w-24 text-center text-ink">
            Trang {safePage} / {totalPage}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={loading || safePage >= totalPage}
            onClick={() => setPage((value) => Math.min(totalPage, value + 1))}
          >
            Sau
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </div>

      {/* Modals */}
      <RoleFormModal
        key={editRole?.id ?? 'new'}
        open={formOpen}
        onClose={() => setFormOpen(false)}
        role={editRole}
      />
      <DeleteConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Xóa vai trò"
        message={`Bạn có chắc muốn xóa vai trò "${deleteTarget?.name}"? Nếu vai trò đang được gán cho người dùng, thao tác sẽ thất bại.`}
      />
    </div>
  )
}
