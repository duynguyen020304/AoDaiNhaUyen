import { useEffect, useState } from 'react'
import { Plus, RefreshCw, Trash2, RotateCcw } from 'lucide-react'
import { request, requestPaginated } from '@/api/client'
import type { PaginatedApiEnvelope } from '@/types/api'
import { Button } from '@/components/ui/button'

type CollectionItem = {
  id: string
  name: string
  slug: string
  description?: string | null
  coverImageUrl?: string | null
  isPublished: boolean
  isFeatured: boolean
  sortOrder: number
  productCount: number
  isDeleted: boolean
}

type FormState = {
  name: string
  slug: string
  description: string
  coverImageUrl: string
  isPublished: boolean
  isFeatured: boolean
  sortOrder: number
}

const emptyForm: FormState = {
  name: '',
  slug: '',
  description: '',
  coverImageUrl: '',
  isPublished: false,
  isFeatured: false,
  sortOrder: 0,
}

export function CollectionsPage() {
  const [items, setItems] = useState<CollectionItem[]>([])
  const [loading, setLoading] = useState(false)
  const [search, setSearch] = useState('')
  const [includeDeleted, setIncludeDeleted] = useState(false)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [editing, setEditing] = useState<CollectionItem | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const qs = new URLSearchParams({ search, includeDeleted: String(includeDeleted), page: '1', pageSize: '50' })
      const res: PaginatedApiEnvelope<CollectionItem[]> = await requestPaginated(`/api/admin/collections?${qs}`)
      setItems(res.data ?? [])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không tải được collections')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [includeDeleted])

  function startEdit(item: CollectionItem) {
    setEditing(item)
    setForm({
      name: item.name,
      slug: item.slug,
      description: item.description ?? '',
      coverImageUrl: item.coverImageUrl ?? '',
      isPublished: item.isPublished,
      isFeatured: item.isFeatured,
      sortOrder: item.sortOrder,
    })
  }

  function resetForm() {
    setEditing(null)
    setForm(emptyForm)
  }

  async function submit() {
    const body = JSON.stringify({
      name: form.name,
      slug: form.slug || null,
      description: form.description || null,
      coverImageUrl: form.coverImageUrl || null,
      isPublished: form.isPublished,
      isFeatured: form.isFeatured,
      sortOrder: form.sortOrder,
    })
    try {
      if (editing) {
        await request(`/api/admin/collections/${editing.id}`, { method: 'PUT', body })
      } else {
        await request('/api/admin/collections', { method: 'POST', body })
      }
      resetForm()
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không lưu được collection')
    }
  }

  async function remove(id: string) {
    if (!confirm('Xóa collection này?')) return
    try {
      await request(`/api/admin/collections/${id}`, { method: 'DELETE' })
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không xóa được collection')
    }
  }

  async function restore(id: string) {
    try {
      await request(`/api/admin/collections/${id}/restore`, { method: 'PATCH' })
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không khôi phục được collection')
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Collections / Lookbook</h1>
          <p className="text-sm text-slate-500">Quản lý bộ sưu tập sản phẩm và lookbook.</p>
        </div>
        <Button onClick={() => void load()} disabled={loading}><RefreshCw className="mr-2 size-4" />Làm mới</Button>
      </div>

      {error && <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div>}

      <div className="grid gap-4 lg:grid-cols-[360px_1fr]">
        <section className="rounded-xl border bg-white p-4 shadow-sm">
          <h2 className="mb-4 font-semibold">{editing ? 'Sửa collection' : 'Tạo collection'}</h2>
          <div className="space-y-3">
            <input className="w-full rounded border px-3 py-2" placeholder="Tên" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <input className="w-full rounded border px-3 py-2" placeholder="Slug" value={form.slug} onChange={(e) => setForm({ ...form, slug: e.target.value })} />
            <textarea className="w-full rounded border px-3 py-2" placeholder="Mô tả" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
            <input className="w-full rounded border px-3 py-2" placeholder="Cover image URL" value={form.coverImageUrl} onChange={(e) => setForm({ ...form, coverImageUrl: e.target.value })} />
            <input className="w-full rounded border px-3 py-2" type="number" value={form.sortOrder} onChange={(e) => setForm({ ...form, sortOrder: Number(e.target.value) })} />
            <label className="flex gap-2 text-sm"><input type="checkbox" checked={form.isPublished} onChange={(e) => setForm({ ...form, isPublished: e.target.checked })} /> Xuất bản</label>
            <label className="flex gap-2 text-sm"><input type="checkbox" checked={form.isFeatured} onChange={(e) => setForm({ ...form, isFeatured: e.target.checked })} /> Nổi bật</label>
            <div className="flex gap-2">
              <Button onClick={() => void submit()}><Plus className="mr-2 size-4" />{editing ? 'Lưu' : 'Tạo'}</Button>
              {editing && <Button variant="outline" onClick={resetForm}>Hủy</Button>}
            </div>
          </div>
        </section>

        <section className="rounded-xl border bg-white p-4 shadow-sm">
          <div className="mb-4 flex flex-wrap items-center gap-3">
            <input className="rounded border px-3 py-2" placeholder="Tìm collection" value={search} onChange={(e) => setSearch(e.target.value)} />
            <Button variant="outline" onClick={() => void load()}>Tìm</Button>
            <label className="flex gap-2 text-sm"><input type="checkbox" checked={includeDeleted} onChange={(e) => setIncludeDeleted(e.target.checked)} /> Bao gồm đã xóa</label>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="text-left text-slate-500"><tr><th className="p-2">Tên</th><th className="p-2">Slug</th><th className="p-2">SP</th><th className="p-2">Trạng thái</th><th className="p-2"></th></tr></thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className="border-t">
                    <td className="p-2 font-medium">{item.name}</td>
                    <td className="p-2 text-slate-500">{item.slug}</td>
                    <td className="p-2">{item.productCount}</td>
                    <td className="p-2">{item.isDeleted ? 'Đã xóa' : item.isPublished ? 'Published' : 'Draft'}</td>
                    <td className="p-2 text-right">
                      <Button variant="outline" size="sm" onClick={() => startEdit(item)}>Sửa</Button>{' '}
                      {item.isDeleted ? <Button variant="outline" size="sm" onClick={() => void restore(item.id)}><RotateCcw className="size-4" /></Button> : <Button variant="destructive" size="sm" onClick={() => void remove(item.id)}><Trash2 className="size-4" /></Button>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </div>
  )
}
