import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import type { CategoryListItem } from '@/types/admin'
import { useCategoryStore } from '@/stores/categoryStore'
import { ModalOverlay } from './ModalOverlay'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'

interface Props {
  open: boolean
  onClose: () => void
  category?: CategoryListItem | null
}

function slugify(text: string) {
  return text
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
}

export function CategoryFormModal({ open, onClose, category }: Props) {
  const { categories, createCategory, updateCategory } = useCategoryStore()

  const isEdit = !!category
  const [name, setName] = useState(category?.name ?? '')
  const [slug, setSlug] = useState(category?.slug ?? '')
  const [parent, setParent] = useState(category?.parent ?? '')
  const [description, setDescription] = useState(category?.description ?? '')
  const [imageUrl, setImageUrl] = useState(category?.imageUrl ?? '')
  const [sortOrder, setSortOrder] = useState(category?.sortOrder ?? 0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleNameChange = (val: string) => {
    setName(val)
    if (!isEdit) setSlug(slugify(val))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      if (isEdit && category) {
        await updateCategory(category.id, {
          name,
          slug,
          parent: parent || null,
          description: description || undefined,
          imageUrl: imageUrl || undefined,
          sortOrder,
        })
      } else {
        await createCategory({
          name,
          slug: slug || slugify(name),
          parent: parent || null,
          description: description || undefined,
          imageUrl: imageUrl || undefined,
          sortOrder,
        })
      }
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đã xảy ra lỗi.')
    } finally {
      setLoading(false)
    }
  }

  const parentOptions = categories
    .filter((c) => c.id !== category?.id)
    .map((c) => (
      <option key={c.id} value={c.id}>{c.name}</option>
    ))

  return (
    <ModalOverlay open={open} onClose={onClose}>
      <div className="p-6">
        <h2 className="text-lg font-semibold mb-4">
          {isEdit ? 'Chỉnh sửa danh mục' : 'Thêm danh mục'}
        </h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="cat-name">Tên danh mục *</Label>
            <Input
              id="cat-name"
              value={name}
              onChange={(e) => handleNameChange(e.target.value)}
              required
              maxLength={120}
              placeholder="Ví dụ: Áo dài truyền thống..."
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cat-slug">Slug *</Label>
            <Input
              id="cat-slug"
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              required
              maxLength={150}
              placeholder="ao-dai-truyen-thong"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cat-parent">Danh mục cha</Label>
            <Select id="cat-parent" value={parent} onChange={(e) => setParent(e.target.value)}>
              <option value="">Không có (danh mục gốc)</option>
              {parentOptions}
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="cat-desc">Mô tả</Label>
            <Textarea
              id="cat-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={500}
              placeholder="Mô tả ngắn về danh mục này..."
              rows={3}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cat-image">URL hình ảnh</Label>
            <Input
              id="cat-image"
              value={imageUrl}
              onChange={(e) => setImageUrl(e.target.value)}
              placeholder="https://..."
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cat-sort">Thứ tự hiển thị</Label>
            <Input
              id="cat-sort"
              type="number"
              value={sortOrder}
              onChange={(e) => setSortOrder(Number(e.target.value) || 0)}
              className="w-24"
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
