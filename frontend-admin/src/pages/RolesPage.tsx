import { useState, useEffect } from 'react'
import { Plus, Pencil, Trash2, Loader2, Shield } from 'lucide-react'
import { useRoleStore } from '@/stores/roleStore'
import type { RoleDto } from '@/types/admin'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { RoleFormModal } from '@/components/admin/RoleFormModal'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'

const PROTECTED_ROLES = new Set(['admin', 'customer'])

export function RolesPage() {
  const { roles, loading, error, fetchRoles, deleteRole, clearError } = useRoleStore()

  const [formOpen, setFormOpen] = useState(false)
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

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">Quản lý vai trò</h1>
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

      <div className="rounded-xl border bg-white overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow className="bg-primary hover:bg-primary">
              <TableHead className="text-primary-foreground">Tên vai trò</TableHead>
              <TableHead className="text-primary-foreground">Mô tả</TableHead>
              <TableHead className="text-primary-foreground text-right">Thao tác</TableHead>
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
            ) : roles.length === 0 ? (
              <TableRow>
                <TableCell colSpan={3} className="text-center py-12 text-muted-foreground">
                  <Shield className="size-8 mx-auto mb-2 opacity-40" />
                  Không có vai trò nào
                </TableCell>
              </TableRow>
            ) : (
              roles.map((r) => (
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
