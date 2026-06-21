import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import type { AdminUserListItem, CreateUserRequest, UpdateUserRequest } from '@/types/admin'
import { useUserStore } from '@/stores/userStore'
import { ModalOverlay } from './ModalOverlay'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Button } from '@/components/ui/button'

interface Props {
  open: boolean
  onClose: () => void
  user?: AdminUserListItem | null
}

export function UserFormModal({ open, onClose, user }: Props) {
  const roles = useUserStore((s) => s.roles)
  const createUser = useUserStore((s) => s.createUser)
  const updateUser = useUserStore((s) => s.updateUser)

  const isEdit = !!user
  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [email, setEmail] = useState(user?.email ?? '')
  const [phone, setPhone] = useState(user?.phone ?? '')
  const [password, setPassword] = useState('')
  const [roleId, setRoleId] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    if (password && password.length < 8) {
      setError('Mật khẩu phải có ít nhất 8 ký tự.')
      setLoading(false)
      return
    }
    try {
      if (isEdit && user) {
        const data: UpdateUserRequest = {
          fullName,
          email: email || undefined,
          phone: phone || undefined,
        }
        await updateUser(user.id, data)
      } else {
        const data: CreateUserRequest = {
          fullName,
          email: email || undefined,
          phone: phone || undefined,
          password: password || undefined,
          roleId: roleId || undefined,
        }
        await createUser(data)
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
          {isEdit ? 'Chỉnh sửa người dùng' : 'Thêm người dùng'}
        </h2>
        <form onSubmit={handleSubmit} className="space-y-4" noValidate>
          <div className="space-y-2">
            <Label htmlFor="user-fullname">Họ tên *</Label>
            <Input
              id="user-fullname"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              required
              maxLength={100}
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="user-email">Email</Label>
              <Input
                id="user-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="user-phone">Số điện thoại</Label>
              <Input
                id="user-phone"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
              />
            </div>
          </div>
          {!isEdit && (
            <div className="space-y-2">
              <Label htmlFor="user-password">Mật khẩu</Label>
              <Input
                id="user-password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                minLength={8}
                maxLength={128}
                placeholder="Ít nhất 8 ký tự; để trống sẽ tạo tài khoản OAuth"
              />
            </div>
          )}
          {!isEdit && roles.length > 0 && (
            <div className="space-y-2">
              <Label htmlFor="user-role">Vai trò</Label>
              <Select
                id="user-role"
                value={roleId}
                onChange={(e) => setRoleId(e.target.value)}
              >
                <option value="">-- Chọn vai trò --</option>
                {roles.map((r) => (
                  <option key={r.id} value={r.id}>{r.name}</option>
                ))}
              </Select>
            </div>
          )}

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
