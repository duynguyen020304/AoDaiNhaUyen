import { useCallback, useEffect, useMemo, useState } from 'react'
import { CalendarClock, ExternalLink, ImageUp, Loader2, Pencil, Plug, RefreshCcw, Send, Share2, Trash2, Video } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  connectFacebookPage,
  deleteFacebookPost,
  disconnectFacebookPage,
  getFacebookConnections,
  getFacebookPosts,
  publishFacebookPhoto,
  publishFacebookPost,
  publishFacebookVideo,
  updateFacebookPost,
  type FacebookConnection,
  type FacebookPost,
} from '@/api/facebook'
import { HttpError } from '@/api/client'

function formatDateTime(value: string | null) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function toIsoOrUndefined(value: string) {
  return value ? new Date(value).toISOString() : undefined
}

function getErrorMessage(error: unknown) {
  if (error instanceof HttpError) return error.message
  if (error instanceof Error) return error.message
  return 'Đã xảy ra lỗi. Vui lòng thử lại.'
}

type ComposerMode = 'text' | 'photo' | 'video'

export function FacebookPage() {
  const [connections, setConnections] = useState<FacebookConnection[]>([])
  const [selectedPageId, setSelectedPageId] = useState('')
  const [posts, setPosts] = useState<FacebookPost[]>([])
  const [afterCursor, setAfterCursor] = useState<string | null>(null)
  const [pageId, setPageId] = useState('')
  const [pageName, setPageName] = useState('')
  const [pageAccessToken, setPageAccessToken] = useState('')
  const [message, setMessage] = useState('')
  const [link, setLink] = useState('')
  const [scheduledAt, setScheduledAt] = useState('')
  const [published, setPublished] = useState(true)
  const [mode, setMode] = useState<ComposerMode>('text')
  const [mediaFile, setMediaFile] = useState<File | null>(null)
  const [editingPost, setEditingPost] = useState<FacebookPost | null>(null)
  const [editMessage, setEditMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const selectedConnection = useMemo(
    () => connections.find((connection) => connection.pageId === selectedPageId) ?? null,
    [connections, selectedPageId],
  )

  const loadConnections = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getFacebookConnections()
      setConnections(data)
      if (!selectedPageId && data[0]) setSelectedPageId(data[0].pageId)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [selectedPageId])

  const loadPosts = useCallback(async (targetPageId = selectedPageId, cursor?: string | null) => {
    if (!targetPageId) return
    setLoading(true)
    setError(null)
    try {
      const data = await getFacebookPosts(targetPageId, cursor)
      setPosts((currentPosts) => (cursor ? [...currentPosts, ...data.items] : data.items))
      setAfterCursor(data.afterCursor)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [selectedPageId])

  useEffect(() => {
    const timeout = window.setTimeout(() => { void loadConnections() }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadConnections])

  useEffect(() => {
    if (!selectedPageId) return undefined
    const timeout = window.setTimeout(() => { void loadPosts(selectedPageId) }, 0)
    return () => window.clearTimeout(timeout)
  }, [selectedPageId, loadPosts])

  async function handleConnect(event: React.FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    setSuccess(null)
    try {
      const connection = await connectFacebookPage({ pageId, pageName: pageName || undefined, pageAccessToken })
      setPageAccessToken('')
      setPageId('')
      setPageName('')
      setSuccess('Đã kết nối Facebook Page. Token chỉ được lưu mã hóa trên backend.')
      setConnections(connections.filter((item) => item.pageId !== connection.pageId).concat(connection))
      setSelectedPageId(connection.pageId)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  async function handlePublish(event: React.FormEvent) {
    event.preventDefault()
    if (!selectedPageId) return
    setSaving(true)
    setError(null)
    setSuccess(null)
    try {
      const schedule = toIsoOrUndefined(scheduledAt)
      if (mode === 'photo') {
        if (!mediaFile) throw new Error('Vui lòng chọn ảnh.')
        await publishFacebookPhoto(selectedPageId, mediaFile, message, schedule, published)
      } else if (mode === 'video') {
        if (!mediaFile) throw new Error('Vui lòng chọn video.')
        await publishFacebookVideo(selectedPageId, mediaFile, message, schedule, published)
      } else {
        await publishFacebookPost(selectedPageId, { message, link: link || undefined, scheduledPublishTime: schedule, published })
      }
      setMessage('')
      setLink('')
      setScheduledAt('')
      setPublished(true)
      setMediaFile(null)
      setSuccess(schedule ? 'Đã lên lịch bài viết Facebook.' : 'Đã gửi bài viết lên Facebook.')
      await loadPosts(selectedPageId)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  async function handleUpdatePost(event: React.FormEvent) {
    event.preventDefault()
    if (!editingPost) return
    setSaving(true)
    setError(null)
    try {
      await updateFacebookPost(editingPost.id, { message: editMessage })
      setEditingPost(null)
      setEditMessage('')
      setSuccess('Đã cập nhật bài viết Facebook.')
      await loadPosts(selectedPageId)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  async function handleDeletePost(postId: string) {
    if (!window.confirm('Xóa bài viết Facebook này? Hành động này không thể hoàn tác trên Facebook.')) return
    setSaving(true)
    setError(null)
    try {
      await deleteFacebookPost(postId)
      setSuccess('Đã xóa bài viết Facebook.')
      await loadPosts(selectedPageId)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  async function handleDisconnect() {
    if (!selectedPageId) return
    if (!window.confirm('Ngắt kết nối Page này? Token đã lưu sẽ bị xóa mềm khỏi hệ thống.')) return
    setSaving(true)
    setError(null)
    try {
      await disconnectFacebookPage(selectedPageId)
      setPosts([])
      setSelectedPageId('')
      setSuccess('Đã ngắt kết nối Facebook Page.')
      await loadConnections()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Quản lý Facebook</h1>
          <p className="text-sm text-muted-foreground mt-1">Kết nối Facebook Page, đăng bài, lên lịch và quản lý nội dung đã đăng.</p>
        </div>
        <Button variant="outline" onClick={() => void loadConnections()} disabled={loading}>
          <RefreshCcw className="size-4" /> Làm mới
        </Button>
      </div>

      {error && <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div>}
      {success && <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700">{success}</div>}

      <div className="grid gap-5 xl:grid-cols-[420px_minmax(0,1fr)]">
        <div className="space-y-5">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2"><Plug className="size-5" />Kết nối Page</CardTitle>
              <CardDescription>Nhập Page ID và Page Access Token. Token không hiển thị lại sau khi lưu.</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-3" onSubmit={handleConnect}>
                <label className="block text-sm font-medium">Page ID<Input className="mt-1" value={pageId} onChange={(e) => setPageId(e.target.value)} required /></label>
                <label className="block text-sm font-medium">Tên Page (tuỳ chọn)<Input className="mt-1" value={pageName} onChange={(e) => setPageName(e.target.value)} /></label>
                <label className="block text-sm font-medium">Page Access Token<Input className="mt-1" type="password" value={pageAccessToken} onChange={(e) => setPageAccessToken(e.target.value)} required /></label>
                <Button className="w-full" disabled={saving || !pageId || !pageAccessToken}>{saving ? <Loader2 className="size-4 animate-spin" /> : <Share2 className="size-4" />}Kết nối</Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Page đang quản lý</CardTitle>
              <CardDescription>Chọn Page để đăng bài và tải danh sách bài viết.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {connections.length === 0 ? <p className="text-sm text-muted-foreground">Chưa có Page nào được kết nối.</p> : (
                <div className="space-y-2">
                  {connections.map((connection) => (
                    <button key={connection.pageId} type="button" className={`w-full rounded-lg border p-3 text-left transition ${selectedPageId === connection.pageId ? 'border-gold bg-gold/10' : 'hover:bg-muted/60'}`} onClick={() => setSelectedPageId(connection.pageId)}>
                      <div className="font-semibold">{connection.pageName || connection.pageId}</div>
                      <div className="text-xs text-muted-foreground">ID: {connection.pageId} · Token ****{connection.tokenLast4}</div>
                      <div className="text-xs text-muted-foreground">Xác thực: {formatDateTime(connection.lastValidatedAt)}</div>
                    </button>
                  ))}
                </div>
              )}
              {selectedConnection && <Button variant="outline" className="w-full text-destructive" onClick={handleDisconnect} disabled={saving}><Trash2 className="size-4" />Ngắt kết nối Page này</Button>}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-5">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2"><Send className="size-5" />Soạn bài Facebook</CardTitle>
              <CardDescription>Hỗ trợ text/link, ảnh, video và lịch đăng.</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={handlePublish}>
                <div className="flex flex-wrap gap-2">
                  {([
                    ['text', Send, 'Bài viết'],
                    ['photo', ImageUp, 'Ảnh'],
                    ['video', Video, 'Video'],
                  ] as const).map(([value, Icon, label]) => (
                    <Button key={value} type="button" variant={mode === value ? 'default' : 'outline'} onClick={() => setMode(value)}><Icon className="size-4" />{label}</Button>
                  ))}
                </div>
                <label className="block text-sm font-medium">Nội dung / caption<Textarea className="mt-1 min-h-32" value={message} onChange={(e) => setMessage(e.target.value)} placeholder="Nhập nội dung tiếng Việt..." /></label>
                {mode === 'text' ? (
                  <label className="block text-sm font-medium">Link đính kèm (tuỳ chọn)<Input className="mt-1" value={link} onChange={(e) => setLink(e.target.value)} placeholder="https://..." /></label>
                ) : (
                  <label className="block text-sm font-medium">File {mode === 'photo' ? 'ảnh' : 'video'}<Input className="mt-1" type="file" accept={mode === 'photo' ? 'image/*' : 'video/*'} onChange={(e) => setMediaFile(e.target.files?.[0] ?? null)} /></label>
                )}
                <div className="grid gap-3 md:grid-cols-2">
                  <label className="block text-sm font-medium">Lên lịch (tuỳ chọn)<Input className="mt-1" type="datetime-local" value={scheduledAt} onChange={(e) => setScheduledAt(e.target.value)} /></label>
                  <label className="flex items-center gap-2 rounded-lg border p-3 text-sm font-medium md:mt-6"><input type="checkbox" checked={published} onChange={(e) => setPublished(e.target.checked)} />Đăng công khai nếu không lên lịch</label>
                </div>
                <Button disabled={!selectedPageId || saving || (!message.trim() && !link.trim() && mode === 'text')}>{saving ? <Loader2 className="size-4 animate-spin" /> : <CalendarClock className="size-4" />}{scheduledAt ? 'Lên lịch' : 'Đăng bài'}</Button>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex-row items-center justify-between gap-3">
              <div>
                <CardTitle>Danh sách bài viết</CardTitle>
                <CardDescription>Bài viết lấy từ Facebook Graph API feed.</CardDescription>
              </div>
              <Button variant="outline" onClick={() => void loadPosts(selectedPageId)} disabled={!selectedPageId || loading}>{loading ? <Loader2 className="size-4 animate-spin" /> : <RefreshCcw className="size-4" />}Tải lại</Button>
            </CardHeader>
            <CardContent>
              {!selectedPageId ? <p className="text-sm text-muted-foreground">Chọn hoặc kết nối Page để xem bài viết.</p> : posts.length === 0 ? <p className="text-sm text-muted-foreground">Chưa có dữ liệu bài viết.</p> : (
                <div className="space-y-3">
                  {posts.map((post) => (
                    <article key={post.id} className="rounded-xl border bg-white p-4 shadow-sm">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div className="min-w-0 flex-1">
                          <div className="text-xs text-muted-foreground">{post.id} · {formatDateTime(post.createdTime)}</div>
                          <p className="mt-2 whitespace-pre-wrap text-sm text-gray-800">{post.message || '(Không có nội dung)'}</p>
                          {post.fullPicture && <img src={post.fullPicture} alt="Facebook post" className="mt-3 max-h-56 rounded-lg border object-cover" />}
                        </div>
                        <div className="flex gap-1">
                          {post.permalinkUrl && <Button variant="ghost" size="icon" className="size-8" onClick={() => window.open(post.permalinkUrl || '#', '_blank', 'noopener,noreferrer')} aria-label="Mở trên Facebook"><ExternalLink className="size-4" /></Button>}
                          <Button variant="ghost" size="icon" className="size-8" onClick={() => { setEditingPost(post); setEditMessage(post.message || '') }} aria-label="Sửa bài viết"><Pencil className="size-4" /></Button>
                          <Button variant="ghost" size="icon" className="size-8 text-destructive" onClick={() => void handleDeletePost(post.id)} aria-label="Xóa bài viết"><Trash2 className="size-4" /></Button>
                        </div>
                      </div>
                      <div className="mt-3 flex flex-wrap gap-2 text-xs text-muted-foreground">
                        <span className="rounded-full bg-muted px-2 py-1">{post.type || 'post'}</span>
                        <span className="rounded-full bg-muted px-2 py-1">{post.isPublished === false ? 'Chưa xuất bản' : 'Đã xuất bản'}</span>
                        {post.scheduledPublishTime && <span className="rounded-full bg-muted px-2 py-1">Lịch: {formatDateTime(post.scheduledPublishTime)}</span>}
                      </div>
                    </article>
                  ))}
                  {afterCursor && <Button variant="outline" className="w-full" onClick={() => void loadPosts(selectedPageId, afterCursor)} disabled={loading}>Tải thêm</Button>}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {editingPost && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <Card className="w-full max-w-2xl">
            <CardHeader>
              <CardTitle>Sửa bài viết Facebook</CardTitle>
              <CardDescription>Facebook chỉ cho phép sửa một số loại bài viết do Page/app tạo.</CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={handleUpdatePost}>
                <Textarea className="min-h-40" value={editMessage} onChange={(e) => setEditMessage(e.target.value)} />
                <div className="flex justify-end gap-2">
                  <Button type="button" variant="outline" onClick={() => setEditingPost(null)}>Hủy</Button>
                  <Button disabled={saving || !editMessage.trim()}>{saving ? <Loader2 className="size-4 animate-spin" /> : null}Lưu</Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
