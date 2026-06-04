import { useState, useEffect, useRef } from 'react'
import { Search, Plus, Pencil, Trash2, Loader2, ChevronLeft, ChevronRight, Users } from 'lucide-react'
import { useUserStore } from '@/stores/userStore'
import type { AdminUserListItem } from '@/types/admin'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { UserFormModal } from '@/components/admin/UserFormModal'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('vi-VN')
}

export function UsersPage() {
  const {
    users, totalPages, totalItems, currentPage, search, loading, error,
    fetchUsers, fetchRoles, deleteUser, updateUserStatus, setSearch, clearError,
  } = useUserStore()

  const [searchInput, setSearchInput] = useState(search)
  const searchTimer = useRef<ReturnType<typeof setTimeout>>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [editUser, setEditUser] = useState<AdminUserListItem | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AdminUserListItem | null>(null)

  useEffect(() => {
    fetchUsers()
    fetchRoles()
  }, [fetchUsers, fetchRoles])

  function handleSearchInput(value: string) {
    setSearchInput(value)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => {
      setSearch(value)
      fetchUsers(value, 1)
    }, 300)
  }

  function handlePageChange(page: number) {
    fetchUsers(undefined, page)
  }

  function handleStatusChange(userId: string, newStatus: string) {
    void updateUserStatus(userId, newStatus)
  }

  function openCreate() {
    setEditUser(null)
    setFormOpen(true)
  }

  function openEdit(user: AdminUserListItem) {
    setEditUser(user)
    setFormOpen(true)
  }

  async function handleDelete() {
    if (!deleteTarget) return
    await deleteUser(deleteTarget.id)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">Quản lý người dùng</h1>
        <Button onClick={openCreate}>
          <Plus className="size-4" />
          Thêm người dùng
        </Button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span className="flex-1">{error}</span>
          <button onClick={clearError} className="underline shrink-0">Đóng</button>
        </div>
      )}

      {/* Search bar */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Tìm theo tên hoặc email..."
          value={searchInput}
          onChange={(e) => handleSearchInput(e.target.value)}
        />
      </div>

      {/* Table */}
      <div className="rounded-xl border bg-white overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow className="bg-primary hover:bg-primary">
              <TableHead className="text-primary-foreground">Họ tên</TableHead>
              <TableHead className="text-primary-foreground">Email</TableHead>
              <TableHead className="text-primary-foreground">SĐT</TableHead>
              <TableHead className="text-primary-foreground">Trạng thái</TableHead>
              <TableHead className="text-primary-foreground">Vai trò</TableHead>
              <TableHead className="text-primary-foreground">Ngày tạo</TableHead>
              <TableHead className="text-primary-foreground text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-12 text-muted-foreground">
                  <Loader2 className="size-6 animate-spin mx-auto mb-2 text-primary" />
                  Đang tải...
                </TableCell>
              </TableRow>
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-12 text-muted-foreground">
                  <Users className="size-8 mx-auto mb-2 opacity-40" />
                  Không có người dùng nào
                </TableCell>
              </TableRow>
            ) : (
              users.map((u) => (
                <TableRow key={u.id}>
                  <TableCell className="font-medium">{u.fullName}</TableCell>
                  <TableCell>{u.email ?? '—'}</TableCell>
                  <TableCell>{u.phone ?? '—'}</TableCell>
                  <TableCell>
                    <Select
                      value={u.status}
                      onChange={(e) => handleStatusChange(u.id, e.target.value)}
                      className="h-8 w-36 text-xs"
                    >
                      <option value="active">Hoạt động</option>
                      <option value="inactive">Không hoạt động</option>
                      <option value="blocked">Bị khóa</option>
                    </Select>
                  </TableCell>
                  <TableCell>
                    {u.roles.length > 0
                      ? u.roles.map((r) => <Badge key={r} variant="outline" className="mr-1">{r}</Badge>)
                      : '—'}
                  </TableCell>
                  <TableCell>{formatDate(u.createdAt)}</TableCell>
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button variant="ghost" size="icon" onClick={() => openEdit(u)} aria-label="Chỉnh sửa">
                        <Pencil className="size-4" />
                      </Button>
                      <Button variant="ghost" size="icon" onClick={() => setDeleteTarget(u)} aria-label="Xóa">
                        <Trash2 className="size-4 text-destructive" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>Tổng: {totalItems} người dùng</span>
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

      {/* Modals */}
      <UserFormModal
        key={editUser?.id ?? 'new'}
        open={formOpen}
        onClose={() => setFormOpen(false)}
        user={editUser}
      />
      <DeleteConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Xóa người dùng"
        message={`Bạn có chắc muốn xóa "${deleteTarget?.fullName}"? Hành động này không thể hoàn tác.`}
      />
    </div>
  )
}
