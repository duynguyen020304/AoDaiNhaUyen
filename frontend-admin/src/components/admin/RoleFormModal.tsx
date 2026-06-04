import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import type { RoleDto, CreateRoleRequest, UpdateRoleRequest } from '@/types/admin'
import { useRoleStore } from '@/stores/roleStore'
import { ModalOverlay } from './ModalOverlay'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'

interface Props {
  open: boolean
  onClose: () => void
  role?: RoleDto | null
}

export function RoleFormModal({ open, onClose, role }: Props) {
  const createRole = useRoleStore((s) => s.createRole)
  const updateRole = useRoleStore((s) => s.updateRole)

  const isEdit = !!role
  const [name, setName] = useState(role?.name ?? '')
  const [description, setDescription] = useState(role?.description ?? '')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      if (isEdit && role) {
        const data: UpdateRoleRequest = { name, description: description || undefined }
        await updateRole(role.id, data)
      } else {
        const data: CreateRoleRequest = { name, description: description || undefined }
        await createRole(data)
      }
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đã xảy ra lỗi.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <ModalOverlay open={open} onClose={onClose}>
      <div className="p-6">
        <h2 className="text-lg font-semibold mb-4">
          {isEdit ? 'Chỉnh sửa vai trò' : 'Thêm vai trò'}
        </h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="role-name">Tên vai trò *</Label>
            <Input
              id="role-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              maxLength={50}
              placeholder="Ví dụ: editor, staff..."
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="role-description">Mô tả</Label>
            <Textarea
              id="role-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={250}
              placeholder="Mô tả ngắn về vai trò này..."
              rows={3}
            />
          </div>

          {error && (
            <p className="text-sm text-destructive bg-destructive/10 rounded-md px-3 py-2">
              {error}
            </p>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={onClose}>
              Hủy
            </Button>
            <Button type="submit" disabled={loading}>
              {loading && <Loader2 className="size-4 animate-spin" />}
              {isEdit ? 'Cập nhật' : 'Tạo mới'}
            </Button>
          </div>
        </form>
      </div>
    </ModalOverlay>
  )
}
