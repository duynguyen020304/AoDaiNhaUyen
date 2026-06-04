import { useState, useEffect, useRef } from 'react'
import { Search, Trash2, Loader2, ChevronLeft, ChevronRight, Eye } from 'lucide-react'
import { getAllImages, deleteImage, getMediaStats, type UserImage, type MediaStats } from '@/api/media'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('vi-VN')
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function MediaPage() {
  const [images, setImages] = useState<UserImage[]>([])
  const [totalPages, setTotalPages] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [search, setSearch] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [sourceFilter, setSourceFilter] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [stats, setStats] = useState<MediaStats | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<UserImage | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [previewImage, setPreviewImage] = useState<string | null>(null)

  const searchTimer = useRef<ReturnType<typeof setTimeout>>(null)

  useEffect(() => {
    getAllImages(page, pageSize, sourceFilter || undefined, search || undefined)
      .then((result) => {
        setImages(result.items)
        setTotalPages(result.totalPages)
        setTotalItems(result.totalItems)
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Không thể tải danh sách ảnh.'))
      .finally(() => setLoading(false))
  }, [page, pageSize, sourceFilter, search])

  useEffect(() => {
    getMediaStats()
      .then(setStats)
      .catch(() => {})
  }, [])

  function handleSearchInput(value: string) {
    setSearchInput(value)
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => {
      setLoading(true)
      setSearch(value)
      setPage(1)
    }, 300)
  }

  function handlePageChange(newPage: number) {
    setLoading(true)
    setPage(newPage)
  }

  function handleFilterChange(value: string) {
    setLoading(true)
    setSourceFilter(value)
    setPage(1)
  }

  async function handleDelete() {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deleteImage(deleteTarget.id)
      setDeleteTarget(null)
      getAllImages(page, pageSize, sourceFilter || undefined, search || undefined)
        .then((result) => {
          setImages(result.items)
          setTotalPages(result.totalPages)
          setTotalItems(result.totalItems)
        })
        .catch(() => {})
      getMediaStats().then(setStats).catch(() => {})
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Xóa ảnh thất bại.')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Quản lý hình ảnh</h1>
          <p className="text-sm text-muted-foreground">Quản lý ảnh chat và AI try-on của người dùng</p>
        </div>
      </div>

      {/* Stats cards */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="rounded-lg border bg-card p-4">
            <div className="text-sm text-muted-foreground">Tổng ảnh</div>
            <div className="text-2xl font-bold">{stats.totalImages}</div>
          </div>
          <div className="rounded-lg border bg-card p-4">
            <div className="text-sm text-muted-foreground">Dung lượng</div>
            <div className="text-2xl font-bold">{formatFileSize(stats.totalSizeBytes)}</div>
          </div>
          <div className="rounded-lg border bg-card p-4">
            <div className="text-sm text-muted-foreground">Chat</div>
            <div className="text-2xl font-bold">{stats.chatImages}</div>
          </div>
          <div className="rounded-lg border bg-card p-4">
            <div className="text-sm text-muted-foreground">AI Try-On</div>
            <div className="text-2xl font-bold">{stats.aiTryOnImages}</div>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <div className="relative max-w-sm flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <Input
            placeholder="Tìm kiếm ảnh..."
            value={searchInput}
            onChange={(e) => handleSearchInput(e.target.value)}
            className="pl-9"
          />
        </div>
        <select
          value={sourceFilter}
          onChange={(e) => handleFilterChange(e.target.value)}
          className="h-9 rounded-md border bg-background px-3 text-sm"
        >
          <option value="">Tất cả nguồn</option>
          <option value="chat">Chat</option>
          <option value="ai_tryon">AI Try-On</option>
        </select>
        <span className="text-sm text-muted-foreground">{totalItems} ảnh</span>
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
      )}

      {/* Table */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-16">Ảnh</TableHead>
              <TableHead>Tên file</TableHead>
              <TableHead>Nguồn</TableHead>
              <TableHead>Kích thước</TableHead>
              <TableHead>Loại</TableHead>
              <TableHead>Ngày tạo</TableHead>
              <TableHead className="w-24">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && images.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-12">
                  <Loader2 className="size-6 animate-spin mx-auto text-muted-foreground" />
                </TableCell>
              </TableRow>
            ) : images.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-12 text-muted-foreground">
                  Chưa có hình ảnh nào.
                </TableCell>
              </TableRow>
            ) : (
              images.map((image) => (
                <TableRow key={image.id}>
                  <TableCell>
                    <button
                      type="button"
                      className="block size-12 rounded overflow-hidden cursor-pointer border"
                      onClick={() => setPreviewImage(image.url)}
                    >
                      <img
                        src={image.url}
                        alt={image.originalFileName ?? 'Ảnh'}
                        className="size-full object-cover"
                        loading="lazy"
                      />
                    </button>
                  </TableCell>
                  <TableCell className="max-w-[200px] truncate text-sm">
                    {image.originalFileName ?? image.objectKey}
                  </TableCell>
                  <TableCell>
                    <Badge variant={image.sourceType === 'ai_tryon' ? 'default' : 'secondary'}>
                      {image.sourceType === 'ai_tryon' ? 'AI Try-On' : 'Chat'}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-sm">{formatFileSize(image.fileSizeBytes)}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{image.mimeType}</TableCell>
                  <TableCell className="text-sm">{formatDate(image.createdAt)}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setPreviewImage(image.url)}
                        title="Xem"
                      >
                        <Eye className="size-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setDeleteTarget(image)}
                        title="Xóa"
                      >
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
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">
            Trang {page} / {totalPages}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => handlePageChange(page - 1)}
            >
              <ChevronLeft className="size-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => handlePageChange(page + 1)}
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Delete confirmation modal */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 max-w-md w-full mx-4 shadow-xl">
            <h3 className="text-lg font-semibold mb-2">Xác nhận xóa</h3>
            <p className="text-sm text-muted-foreground mb-4">
              Bạn có chắc muốn xóa ảnh "{deleteTarget.originalFileName ?? deleteTarget.objectKey}"?
              Ảnh sẽ bị xóa vĩnh viễn khỏi S3.
            </p>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setDeleteTarget(null)}>Hủy</Button>
              <Button variant="destructive" onClick={handleDelete} disabled={deleting}>
                {deleting && <Loader2 className="size-4 animate-spin mr-2" />}
                Xóa
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Preview modal */}
      {previewImage && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-8"
          onMouseDown={(e) => { if (e.target === e.currentTarget) setPreviewImage(null) }}
        >
          <button
            type="button"
            className="absolute top-4 right-4 text-white text-xl"
            onClick={() => setPreviewImage(null)}
          >
            ✕
          </button>
          <img
            src={previewImage}
            alt="Preview"
            className="max-w-full max-h-full rounded-lg object-contain"
          />
        </div>
      )}
    </div>
  )
}
