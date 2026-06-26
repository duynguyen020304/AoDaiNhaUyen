import { useCallback, useEffect, useMemo, useState } from 'react'
import { Eye, History, Loader2, RefreshCw, Search, X } from 'lucide-react'
import { getAdminAuditLog, getAdminAuditLogs, getAdminAuditLogStats } from '@/api/admin'
import type { AdminAuditLogDetail, AdminAuditLogItem, AdminAuditLogStats } from '@/types/admin'
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { PageSizeSelect } from '@/components/admin/PageSizeSelect'

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('vi-VN')
}

function formatPreview(value: string | null | undefined) {
  if (!value) return '—'
  return value.length > 160 ? `${value.slice(0, 160)}…` : value
}

const ACTION_LABELS: Record<string, string> = {
  create: 'Tạo mới',
  update: 'Cập nhật',
  delete: 'Xóa',
  restore: 'Khôi phục',
  upload: 'Tải lên',
  role_change: 'Đổi vai trò',
  status_change: 'Đổi trạng thái',
  visibility_change: 'Đổi hiển thị',
  set_primary: 'Đặt ảnh chính',
  make_public: 'Công khai',
  make_private: 'Riêng tư',
  send: 'Gửi',
  retry: 'Thử lại',
  cancel: 'Hủy',
}

export function AuditLogsPage() {
  const [items, setItems] = useState<AdminAuditLogItem[]>([])
  const [stats, setStats] = useState<AdminAuditLogStats | null>(null)
  const [detail, setDetail] = useState<AdminAuditLogDetail | null>(null)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [loading, setLoading] = useState(true)
  const [detailLoading, setDetailLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [totalItems, setTotalItems] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [q, setQ] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [actionType, setActionType] = useState('')
  const [entityType, setEntityType] = useState('')
  const [success, setSuccess] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [list, nextStats] = await Promise.all([
        getAdminAuditLogs({ page, pageSize, q: q || undefined, actionType: actionType || undefined, entityType: entityType || undefined, success: success || undefined }),
        getAdminAuditLogStats({ q: q || undefined, actionType: actionType || undefined, entityType: entityType || undefined, success: success || undefined }),
      ])
      setItems(list.data)
      setTotalItems(list.totalItem)
      setTotalPages(list.totalPage)
      setStats(nextStats)
      if (list.data.length === 0) setDetail(null)
    } catch (err) {
      setItems([])
      setStats(null)
      setError(err instanceof Error ? err.message : 'Không thể tải nhật ký thao tác.')
    } finally {
      setLoading(false)
    }
  }, [actionType, entityType, page, pageSize, q, success])

  useEffect(() => {
    const id = window.setTimeout(() => {
      setQ(searchInput.trim())
      setPage(1)
    }, 300)
    return () => window.clearTimeout(id)
  }, [searchInput])

  useEffect(() => {
    void load()
  }, [load])

  async function openDetail(id: string) {
    setIsDetailOpen(true)
    setDetailLoading(true)
    try {
      setDetail(await getAdminAuditLog(id))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể tải chi tiết nhật ký.')
    } finally {
      setDetailLoading(false)
    }
  }

  function closeDetail() {
    setIsDetailOpen(false)
  }

  const entities = useMemo(() => Array.from(new Set(items.map((item) => item.entityType))).sort(), [items])
  const actions = useMemo(() => Array.from(new Set(items.map((item) => item.actionType))).sort(), [items])

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Nhật ký thao tác</h1>
          <p className="mt-1 text-sm text-muted-foreground">Theo dõi các thay đổi phát sinh từ hệ thống quản trị hiện tại.</p>
        </div>
        <Button variant="outline" onClick={() => void load()} disabled={loading}>
          <RefreshCw className={`size-4 ${loading ? 'animate-spin' : ''}`} />
          Làm mới
        </Button>
      </div>

      {stats && (
        <div className="grid gap-4 md:grid-cols-4">
          <StatCard label="Tổng sự kiện" value={stats.total} />
          <StatCard label="Thành công" value={stats.success} tone="green" />
          <StatCard label="Thất bại" value={stats.failed} tone="red" />
          <StatCard label="Admin hoạt động" value={stats.distinctActors} />
        </div>
      )}

      <Card className="p-4">
        <div className="grid gap-3 lg:grid-cols-[minmax(0,1.4fr)_200px_200px_160px]">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input className="pl-9" placeholder="Tìm theo đường dẫn, admin, entity hoặc nội dung..." value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
          </div>
          <select className="h-10 rounded-md border border-input bg-white px-3 text-sm" value={actionType} onChange={(e) => { setActionType(e.target.value); setPage(1) }}>
            <option value="">Tất cả hành động</option>
            {actions.map((action) => <option key={action} value={action}>{ACTION_LABELS[action] ?? action}</option>)}
          </select>
          <select className="h-10 rounded-md border border-input bg-white px-3 text-sm" value={entityType} onChange={(e) => { setEntityType(e.target.value); setPage(1) }}>
            <option value="">Tất cả đối tượng</option>
            {entities.map((entity) => <option key={entity} value={entity}>{entity}</option>)}
          </select>
          <select className="h-10 rounded-md border border-input bg-white px-3 text-sm" value={success} onChange={(e) => { setSuccess(e.target.value); setPage(1) }}>
            <option value="">Tất cả kết quả</option>
            <option value="true">Thành công</option>
            <option value="false">Thất bại</option>
          </select>
        </div>
      </Card>

      {error && <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">{error}</div>}

      <div className="relative">
        <Card className="overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Thời gian</TableHead>
                <TableHead>Admin</TableHead>
                <TableHead>Hành động</TableHead>
                <TableHead>Đối tượng</TableHead>
                <TableHead>Kết quả</TableHead>
                <TableHead>Nội dung</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={7} className="py-12 text-center text-muted-foreground">
                    <Loader2 className="mx-auto mb-2 size-6 animate-spin text-primary" />
                    Đang tải nhật ký...
                  </TableCell>
                </TableRow>
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="py-12 text-center text-muted-foreground">
                    <History className="mx-auto mb-2 size-8 opacity-40" />
                    Chưa có bản ghi phù hợp.
                  </TableCell>
                </TableRow>
              ) : items.map((item) => (
                <TableRow key={item.id} className="hover:bg-wine/5">
                  <TableCell className="whitespace-nowrap text-xs">{formatDateTime(item.createdAt)}</TableCell>
                  <TableCell>
                    <div className="font-medium text-ink">{item.actorName || 'Không rõ'}</div>
                    <div className="text-xs text-muted-foreground">{item.actorEmail || item.actorRoles || '—'}</div>
                  </TableCell>
                  <TableCell>
                    <div className="font-medium text-ink">{ACTION_LABELS[item.actionType] ?? item.actionType}</div>
                    <div className="text-xs text-muted-foreground">{item.httpMethod}</div>
                  </TableCell>
                  <TableCell>
                    <div className="font-medium text-ink">{item.entityType}</div>
                    <div className="text-xs text-muted-foreground break-all">{item.entityId || item.path}</div>
                  </TableCell>
                  <TableCell>
                    <span className={`inline-flex rounded-full px-2 py-1 text-xs font-medium ${item.success ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-red-50 text-red-700 border border-red-200'}`}>
                      {item.success ? `OK ${item.statusCode}` : `Lỗi ${item.statusCode}`}
                    </span>
                  </TableCell>
                  <TableCell className="max-w-[280px] text-xs text-muted-foreground">{formatPreview(item.requestPreview || item.responsePreview || item.error)}</TableCell>
                  <TableCell className="text-right">
                    <Button variant="outline" size="sm" onClick={() => void openDetail(item.id)}>
                      <Eye className="size-4" />
                      Xem chi tiết
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>

        {isDetailOpen && (
          <>
            <button
              type="button"
              aria-label="Đóng chi tiết"
              className="fixed inset-0 z-30 bg-black/30"
              onClick={closeDetail}
            />
            <aside className="fixed right-0 top-0 z-40 h-dvh w-full max-w-xl overflow-y-auto border-l bg-white shadow-2xl">
              <div className="sticky top-0 flex items-center justify-between gap-3 border-b bg-white px-5 py-4">
                <div>
                  <h2 className="text-lg font-semibold text-ink">Chi tiết nhật ký</h2>
                  <p className="text-sm text-muted-foreground">Xem đầy đủ request/response của thao tác đã chọn.</p>
                </div>
                <div className="flex items-center gap-3">
                  {detailLoading && <Loader2 className="size-4 animate-spin text-primary" />}
                  <Button variant="ghost" size="icon" onClick={closeDetail} aria-label="Đóng sidebar chi tiết">
                    <X className="size-5" />
                  </Button>
                </div>
              </div>
              <div className="space-y-4 p-5 text-sm">
                {!detail ? (
                  <div className="text-sm text-muted-foreground">Đang tải chi tiết nhật ký...</div>
                ) : (
                  <>
                    <Field label="Admin" value={detail.actorName || 'Không rõ'} subValue={detail.actorEmail || detail.actorRoles || undefined} />
                    <Field label="Hành động" value={ACTION_LABELS[detail.actionType] ?? detail.actionType} subValue={`${detail.httpMethod} · ${detail.statusCode}`} />
                    <Field label="Đối tượng" value={detail.entityType} subValue={detail.entityId || undefined} />
                    <Field label="Controller / Action" value={detail.controllerName || '—'} subValue={detail.actionName || undefined} />
                    <Field label="Đường dẫn" value={detail.path} subValue={detail.queryString || undefined} />
                    <Field label="Thời gian" value={formatDateTime(detail.createdAt)} />
                    <PreviewBlock title="Request preview" value={detail.requestPreview} />
                    <PreviewBlock title="Response preview" value={detail.responsePreview} />
                    <PreviewBlock title="Lỗi" value={detail.error} />
                  </>
                )}
              </div>
            </aside>
          </>
        )}
      </div>

      {totalItems > 0 && (
        <div className="flex flex-col gap-3 text-sm text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <span>Tổng: {totalItems} bản ghi</span>
            <PageSizeSelect value={pageSize} onChange={(value) => { setPageSize(value); setPage(1) }} disabled={loading} />
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Trước</Button>
            <span>Trang {page} / {totalPages}</span>
            <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>Sau</Button>
          </div>
        </div>
      )}
    </div>
  )
}

function StatCard({ label, value, tone = 'default' }: { label: string; value: number; tone?: 'default' | 'green' | 'red' }) {
  const toneClass = tone === 'green' ? 'text-emerald-600' : tone === 'red' ? 'text-red-600' : 'text-ink'
  return (
    <Card className="p-4">
      <div className="text-sm text-muted-foreground">{label}</div>
      <div className={`mt-2 text-2xl font-bold ${toneClass}`}>{value}</div>
    </Card>
  )
}

function Field({ label, value, subValue }: { label: string; value: string; subValue?: string }) {
  return (
    <div>
      <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="mt-1 break-all text-ink">{value}</div>
      {subValue && <div className="mt-1 break-all text-xs text-muted-foreground">{subValue}</div>}
    </div>
  )
}

function PreviewBlock({ title, value }: { title: string; value?: string | null }) {
  return (
    <div>
      <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</div>
      <pre className="mt-2 overflow-x-auto rounded-lg bg-zinc-950 p-3 text-xs text-zinc-100 whitespace-pre-wrap break-words">{value || '—'}</pre>
    </div>
  )
}
