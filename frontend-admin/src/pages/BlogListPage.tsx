import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { FileText, Loader2, Pencil, Plus, Search, Trash2 } from 'lucide-react'
import { useBlogStore } from '@/stores/blogStore'
import type { BlogPostListItem, BlogStatus } from '@/types/blog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleDateString('vi-VN') : '—'
}

function statusLabel(status: BlogStatus) {
  return status === 'Published' ? 'Đã xuất bản' : status === 'Archived' ? 'Lưu trữ' : 'Bản nháp'
}

export function BlogListPage() {
  const { posts, loading, error, search, status, fetchPosts, setSearch, setStatus, deletePost, clearError } = useBlogStore()
  const [deleteTarget, setDeleteTarget] = useState<BlogPostListItem | null>(null)

  useEffect(() => { fetchPosts() }, [fetchPosts])

  async function confirmDelete() {
    if (!deleteTarget) return
    await deletePost(deleteTarget.id)
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold">Quản lý bài đăng</h1>
          <p className="text-sm text-muted-foreground">Bài viết SEO hiển thị ở frontend.</p>
        </div>
        <Link to="/admin/blog/new"><Button><Plus className="size-4" />Bài mới</Button></Link>
      </div>

      {error && <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive"><span>{error}</span><button onClick={clearError} className="ml-3 underline">Đóng</button></div>}

      <div className="flex flex-col gap-3 md:flex-row md:items-center md:max-w-2xl">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input className="pl-9" value={search} onChange={(e) => setSearch(e.target.value)} onBlur={() => fetchPosts()} placeholder="Tìm tiêu đề hoặc tóm tắt..." />
        </div>
        <Select value={status} onChange={(e) => { setStatus(e.target.value as BlogStatus | ''); queueMicrotask(fetchPosts) }}>
          <option value="">Tất cả trạng thái</option>
          <option value="Draft">Bản nháp</option>
          <option value="Published">Đã xuất bản</option>
          <option value="Archived">Lưu trữ</option>
        </Select>
        <Button variant="outline" onClick={() => fetchPosts()}>Lọc</Button>
      </div>

      <div className="rounded-xl border bg-white overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow className="bg-primary hover:bg-primary">
              <TableHead className="text-primary-foreground">Tiêu đề</TableHead>
              <TableHead className="text-primary-foreground">Template</TableHead>
              <TableHead className="text-primary-foreground">Danh mục</TableHead>
              <TableHead className="text-primary-foreground">Trạng thái</TableHead>
              <TableHead className="text-primary-foreground">Tags</TableHead>
              <TableHead className="text-primary-foreground">Ngày xuất bản</TableHead>
              <TableHead className="text-primary-foreground text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && posts.length === 0 ? (
              <TableRow><TableCell colSpan={7} className="py-12 text-center"><Loader2 className="mx-auto mb-2 size-6 animate-spin" />Đang tải...</TableCell></TableRow>
            ) : posts.length === 0 ? (
              <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground"><FileText className="mx-auto mb-2 size-8 opacity-40" />Chưa có bài đăng</TableCell></TableRow>
            ) : posts.map((post) => (
              <TableRow key={post.id}>
                <TableCell><div className="font-medium">{post.title}</div><div className="text-xs text-muted-foreground">/{post.slug}</div></TableCell>
                <TableCell><Badge variant="outline">{post.template}</Badge></TableCell>
                <TableCell>{post.category ? <Badge variant="outline">{post.category.name}</Badge> : <span className="text-xs text-muted-foreground">—</span>}</TableCell>
                <TableCell><Badge>{statusLabel(post.status)}</Badge></TableCell>
                <TableCell><div className="flex max-w-xs flex-wrap gap-1">{post.tags.slice(0, 3).map(t => <Badge key={t} variant="outline">{t}</Badge>)}</div></TableCell>
                <TableCell>{formatDate(post.publishedAt)}</TableCell>
                <TableCell className="text-right"><div className="flex justify-end gap-1"><Link to={`/admin/blog/${post.id}/edit`}><Button variant="ghost" size="icon"><Pencil className="size-4" /></Button></Link><Button variant="ghost" size="icon" onClick={() => setDeleteTarget(post)}><Trash2 className="size-4 text-destructive" /></Button></div></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <DeleteConfirmModal open={!!deleteTarget} onClose={() => setDeleteTarget(null)} onConfirm={confirmDelete} title="Xóa bài đăng" message={`Bạn có chắc muốn xóa "${deleteTarget?.title}"?`} />
    </div>
  )
}
