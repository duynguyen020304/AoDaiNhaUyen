import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { CalendarClock, ExternalLink, Loader2, RefreshCcw, Search, Send, Trash2, Upload, UsersRound } from 'lucide-react'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'
import { HttpError } from '@/api/client'
import {
  createSocialPost,
  disconnectSocialAccount,
  getSocialAccounts,
  getSocialPosts,
  uploadSocialMedia,
  selectFacebookPage,
  type SocialAccountConnection,
  type SocialPost,
} from '@/api/social'

type TabKey = 'fanpages' | 'composer' | 'history'

type UploadedMedia = {
  id: string
  fileName: string
  contentType: string
  publicUrl: string
  progress: number
  status: 'uploading' | 'uploaded' | 'error'
}

const PROFILE_STORAGE_KEY = 'zernio_profile_id'
const CONNECT_STATE_STORAGE_KEY = 'zernio_connect_state'

function formatDateTime(value: string | null) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function getErrorMessage(error: unknown) {
  if (error instanceof HttpError) return error.message
  if (error instanceof Error) return error.message
  return 'Đã xảy ra lỗi. Vui lòng thử lại.'
}

function statusBadge(isActive: boolean) {
  return isActive
    ? <Badge variant="success">Đang kết nối</Badge>
    : <Badge variant="outline" className="border-gray-200 bg-gray-50 text-gray-500">Đã ngắt</Badge>
}

function postStatusBadge(status: string | null) {
  switch (status) {
    case 'published':
    case 'success':
      return <Badge variant="success">Đã đăng</Badge>
    case 'scheduled':
    case 'pending':
      return <Badge variant="warning">Đã lên lịch</Badge>
    case 'failed':
    case 'error':
      return <Badge variant="outline" className="border-destructive/40 text-destructive">Lỗi</Badge>
    default:
      return <Badge variant="outline">{status || 'Không rõ'}</Badge>
  }
}

function redirectUrl() {
  return `${window.location.origin}/admin/facebook`
}

function clearCallbackQuery() {
  const url = new URL(window.location.href)
  ;[
    'connected',
    'profileId',
    'accountId',
    'username',
    'pageId',
    'tempToken',
    'userProfileId',
    'userProfileName',
    'userProfilePicture',
    'state',
    'error',
    'error_description',
  ].forEach((key) => {
    url.searchParams.delete(key)
  })
  window.history.replaceState({}, '', `${url.pathname}${url.search}${url.hash}`)
}

function clearStoredConnectState() {
  window.sessionStorage.removeItem(CONNECT_STATE_STORAGE_KEY)
}

export function FacebookPage() {
  const [accounts, setAccounts] = useState<SocialAccountConnection[]>([])
  const [posts, setPosts] = useState<SocialPost[]>([])
  const [activeTab, setActiveTab] = useState<TabKey>('fanpages')
  const [profileId, setProfileId] = useState(() => window.localStorage.getItem(PROFILE_STORAGE_KEY) ?? '')
  const [searchInput, setSearchInput] = useState('')
  const [selectedAccountIds, setSelectedAccountIds] = useState<string[]>([])
  const [content, setContent] = useState('')
  const [publishNow, setPublishNow] = useState(true)
  const [scheduledFor, setScheduledFor] = useState('')
  const [uploadedMedia, setUploadedMedia] = useState<UploadedMedia[]>([])
  const [deleteTarget, setDeleteTarget] = useState<SocialAccountConnection | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const filteredAccounts = useMemo(() => {
    const keyword = searchInput.trim().toLowerCase()
    if (!keyword) return accounts
    return accounts.filter((account) =>
      (account.displayName ?? '').toLowerCase().includes(keyword)
      || (account.username ?? '').toLowerCase().includes(keyword)
      || account.zernioAccountId.toLowerCase().includes(keyword)
    )
  }, [accounts, searchInput])

  const activeAccounts = useMemo(() => accounts.filter((account) => account.isActive), [accounts])

  const mediaUrls = useMemo(() => uploadedMedia
    .filter((item) => item.status === 'uploaded')
    .map((item) => item.publicUrl), [uploadedMedia])

  const hasUploadingMedia = uploadedMedia.some((item) => item.status === 'uploading')

  const loadAccounts = useCallback(async (sync = false, targetProfileId = profileId) => {
    setLoading(true)
    setError(null)
    try {
      const data = await getSocialAccounts('facebook', sync, targetProfileId || undefined)
      setAccounts(data)
      setSelectedAccountIds((current) => current.filter((id) => data.some((account) => account.id === id && account.isActive)))
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [profileId])

  const loadPosts = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getSocialPosts('facebook', undefined, profileId || undefined, 1, 25)
      setPosts(data.items)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [profileId])

  const handleCallback = useCallback(async () => {
    const url = new URL(window.location.href)
    const oauthError = url.searchParams.get('error_description') || url.searchParams.get('error')
    const connected = url.searchParams.get('connected')
    const callbackProfileId = url.searchParams.get('profileId')
    const pageId = url.searchParams.get('pageId')
    const tempToken = url.searchParams.get('tempToken')
    if (!oauthError && !connected && !pageId && !tempToken) return

    clearCallbackQuery()
    clearStoredConnectState()

    if (oauthError) {
      setError(`Zernio từ chối kết nối: ${oauthError}`)
      return
    }

    if (callbackProfileId) {
      setProfileId(callbackProfileId)
      window.localStorage.setItem(PROFILE_STORAGE_KEY, callbackProfileId)
    }

    setLoading(true)
    setError(null)
    try {
      if (pageId && tempToken) {
        await selectFacebookPage({
          profileId: callbackProfileId || profileId,
          pageId,
          tempToken,
          redirectUrl: redirectUrl(),
          userProfile: {
            id: url.searchParams.get('userProfileId') || 'zernio-user',
            name: url.searchParams.get('userProfileName') || 'Zernio user',
            profilePicture: url.searchParams.get('userProfilePicture') || 'https://zernio.com/favicon.ico',
          },
        })
      }

      await loadAccounts(true, callbackProfileId || profileId)
      setSuccess('Đã đồng bộ fanpage Facebook từ Zernio.')
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [loadAccounts, profileId])

  useEffect(() => {
    const timeout = window.setTimeout(() => { void loadAccounts(false) }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadAccounts])

  useEffect(() => {
    const timeout = window.setTimeout(() => { void handleCallback() }, 0)
    return () => window.clearTimeout(timeout)
  }, [handleCallback])

  async function handleSync() {
    const normalizedProfileId = profileId.trim()
    if (normalizedProfileId) window.localStorage.setItem(PROFILE_STORAGE_KEY, normalizedProfileId)
    await loadAccounts(true, normalizedProfileId)
    await loadPosts()
  }

  async function handleDisconnect() {
    if (!deleteTarget) return
    setSaving(true)
    setError(null)
    setSuccess(null)
    try {
      await disconnectSocialAccount(deleteTarget.id)
      setDeleteTarget(null)
      setSuccess('Đã ngắt kết nối fanpage.')
      await loadAccounts(false)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  async function handleMediaUpload(files: FileList | null) {
    const selectedFiles = Array.from(files ?? [])
    if (selectedFiles.length === 0) return

    setError(null)
    setSuccess(null)
    for (const file of selectedFiles) {
      const isVideo = file.type.startsWith('video/')
      const maxBytes = isVideo ? 200 * 1024 * 1024 : 10 * 1024 * 1024
      const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'video/mp4', 'video/quicktime', 'video/webm']
      if (!allowedTypes.includes(file.type)) {
        setError(`File ${file.name} không đúng định dạng. Chỉ hỗ trợ JPG, PNG, WEBP, GIF, MP4, MOV hoặc WEBM.`)
        continue
      }
      if (file.size <= 0 || file.size > maxBytes) {
        setError(isVideo ? `Video ${file.name} vượt quá 200MB.` : `Ảnh ${file.name} vượt quá 10MB.`)
        continue
      }

      const mediaId = crypto.randomUUID()
      setUploadedMedia((current) => current.concat({
        id: mediaId,
        fileName: file.name,
        contentType: file.type,
        publicUrl: '',
        progress: 0,
        status: 'uploading',
      }))

      try {
        const upload = await uploadSocialMedia(file)
        setUploadedMedia((current) => current.map((item) => item.id === mediaId
          ? { ...item, publicUrl: upload.publicUrl, progress: 100, status: 'uploaded' }
          : item))
      } catch (err) {
        setUploadedMedia((current) => current.map((item) => item.id === mediaId
          ? { ...item, progress: 0, status: 'error' }
          : item))
        setError(getErrorMessage(err))
      }
    }
  }

  async function handleCreatePost(event: FormEvent) {
    event.preventDefault()
    if (selectedAccountIds.length === 0) {
      setError('Vui lòng chọn ít nhất một fanpage.')
      return
    }

    setSaving(true)
    setError(null)
    setSuccess(null)
    try {
      await createSocialPost({
        content: content.trim(),
        accountIds: selectedAccountIds,
        publishNow,
        scheduledFor: publishNow || !scheduledFor ? null : new Date(scheduledFor).toISOString(),
        mediaUrls,
      })
      setContent('')
      setUploadedMedia([])
      setScheduledFor('')
      setPublishNow(true)
      setSuccess(publishNow ? 'Đã gửi bài viết sang Zernio để đăng.' : 'Đã gửi lịch đăng sang Zernio.')
      setActiveTab('history')
      await loadPosts()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div>
      <div className="mb-6 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Quản lý fanpage</h1>
          <p className="mt-1 text-sm text-muted-foreground">Đồng bộ Facebook qua Zernio, đăng bài và theo dõi lịch sử đăng.</p>
        </div>
        <Button variant="outline" onClick={() => void handleSync()} disabled={loading || saving}>
          {loading ? <Loader2 className="size-4 animate-spin" /> : <RefreshCcw className="size-4" />}
          Đồng bộ
        </Button>
      </div>

      {error && (
        <div className="mb-4 flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span className="flex-1">{error}</span>
          <button onClick={() => setError(null)} className="shrink-0 underline">Đóng</button>
        </div>
      )}
      {success && (
        <div className="mb-4 flex items-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          <span className="flex-1">{success}</span>
          <button onClick={() => setSuccess(null)} className="shrink-0 underline">Đóng</button>
        </div>
      )}

      <div className="mb-4 flex flex-wrap gap-2">
        {([
          ['fanpages', UsersRound, 'Fanpage'],
          ['composer', Send, 'Đăng bài'],
          ['history', CalendarClock, 'Lịch sử'],
        ] as const).map(([key, Icon, label]) => (
          <Button key={key} type="button" variant={activeTab === key ? 'default' : 'outline'} onClick={() => { setActiveTab(key); if (key === 'history') void loadPosts() }}>
            <Icon className="size-4" />
            {label}
          </Button>
        ))}
      </div>

      {activeTab === 'fanpages' && (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-3">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input className="w-72 pl-9" placeholder="Tìm fanpage..." value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
            </div>
          </div>

          <Card className="overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Fanpage</TableHead>
                  <TableHead>Zernio Account ID</TableHead>
                  <TableHead>Profile ID</TableHead>
                  <TableHead>Đồng bộ</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead className="text-right">Thao tác</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading && accounts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="py-12 text-center text-muted-foreground">
                      <Loader2 className="mx-auto mb-2 size-6 animate-spin text-primary" />
                      Đang tải...
                    </TableCell>
                  </TableRow>
                ) : filteredAccounts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="py-12 text-center text-muted-foreground">
                      <UsersRound className="mx-auto mb-2 size-8 opacity-40" />
                      Chưa có fanpage. Kết nối Zernio để đồng bộ danh sách.
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredAccounts.map((account) => (
                    <TableRow key={account.id} className={!account.isActive ? 'bg-muted/30 opacity-60' : ''}>
                      <TableCell>
                        <div className="font-medium text-ink">{account.displayName || account.username || 'Facebook Page'}</div>
                        <div className="text-xs text-muted-foreground">{account.username || '—'}</div>
                      </TableCell>
                      <TableCell className="font-mono text-xs">{account.zernioAccountId}</TableCell>
                      <TableCell className="font-mono text-xs">{account.zernioProfileId}</TableCell>
                      <TableCell>{formatDateTime(account.lastSyncedAt)}</TableCell>
                      <TableCell>{statusBadge(account.isActive)}</TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="icon" className="size-8 text-destructive hover:text-destructive" onClick={() => setDeleteTarget(account)} aria-label="Ngắt kết nối">
                          <Trash2 className="size-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </Card>
        </>
      )}

      {activeTab === 'composer' && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><Send className="size-5" />Đăng bài Facebook</CardTitle>
            <CardDescription>Zernio sẽ đăng ngay hoặc lên lịch cho fanpage đã chọn.</CardDescription>
          </CardHeader>
          <CardContent>
            <form className="space-y-5" onSubmit={handleCreatePost}>
              <div className="grid gap-3 md:grid-cols-2">
                {activeAccounts.map((account) => (
                  <label key={account.id} className="flex items-start gap-3 rounded-lg border bg-white p-3 text-sm">
                    <input
                      type="checkbox"
                      className="mt-1"
                      checked={selectedAccountIds.includes(account.id)}
                      onChange={(e) => setSelectedAccountIds((current) => e.target.checked ? current.concat(account.id) : current.filter((id) => id !== account.id))}
                    />
                    <span>
                      <span className="block font-medium text-ink">{account.displayName || account.username || account.zernioAccountId}</span>
                      <span className="block font-mono text-xs text-muted-foreground">{account.zernioAccountId}</span>
                    </span>
                  </label>
                ))}
              </div>
              {activeAccounts.length === 0 && <p className="text-sm text-muted-foreground">Chưa có fanpage để đăng bài.</p>}

              <label className="block text-sm font-medium">
                Nội dung
                <Textarea className="mt-1 min-h-36" value={content} onChange={(e) => setContent(e.target.value)} placeholder="Nhập nội dung bài viết..." required />
              </label>

              <div className="space-y-3">
                <div>
                  <div className="text-sm font-medium">Ảnh/video đính kèm</div>
                  <p className="mt-1 text-xs text-muted-foreground">Upload qua backend như ảnh sản phẩm, backend lưu lên S3 rồi trả URL public để gửi sang Zernio. Ảnh tối đa 10MB, video tối đa 200MB.</p>
                </div>
                <label className="flex cursor-pointer items-center justify-center gap-2 rounded-lg border border-dashed border-primary/40 bg-primary/5 px-4 py-6 text-sm font-medium text-primary transition hover:bg-primary/10">
                  <Upload className="size-4" />
                  Tải ảnh/video lên S3
                  <input
                    type="file"
                    className="hidden"
                    multiple
                    accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/quicktime,video/webm"
                    disabled={saving || hasUploadingMedia}
                    onChange={(event) => {
                      void handleMediaUpload(event.target.files)
                      event.target.value = ''
                    }}
                  />
                </label>
                {uploadedMedia.length > 0 && (
                  <div className="space-y-2">
                    {uploadedMedia.map((item) => (
                      <div key={item.id} className="flex items-center gap-3 rounded-lg border bg-white p-3 text-sm">
                        {item.publicUrl && item.contentType.startsWith('image/') ? (
                          <img src={item.publicUrl} alt="" className="size-12 rounded-md object-cover" />
                        ) : (
                          <div className="flex size-12 items-center justify-center rounded-md bg-muted text-xs uppercase text-muted-foreground">
                            {item.contentType.startsWith('video/') ? 'VID' : 'IMG'}
                          </div>
                        )}
                        <div className="min-w-0 flex-1">
                          <div className="truncate font-medium text-ink">{item.fileName}</div>
                          <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-muted">
                            <div className={`h-full ${item.status === 'error' ? 'bg-destructive' : 'bg-primary'}`} style={{ width: `${item.progress}%` }} />
                          </div>
                          <div className="mt-1 text-xs text-muted-foreground">
                            {item.status === 'uploading' ? `Đang upload ${item.progress}%` : item.status === 'uploaded' ? 'Đã upload lên S3' : 'Upload lỗi'}
                          </div>
                        </div>
                        <Button type="button" variant="ghost" size="icon" className="size-8" onClick={() => setUploadedMedia((current) => current.filter((media) => media.id !== item.id))} aria-label="Xóa media">
                          <Trash2 className="size-4" />
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div className="grid gap-3 md:grid-cols-2">
                <label className="flex items-center gap-2 rounded-lg border p-3 text-sm font-medium">
                  <input type="checkbox" checked={publishNow} onChange={(e) => setPublishNow(e.target.checked)} />
                  Đăng ngay
                </label>
                <label className="block text-sm font-medium">
                  Lên lịch
                  <Input className="mt-1" type="datetime-local" value={scheduledFor} disabled={publishNow} onChange={(e) => setScheduledFor(e.target.value)} />
                </label>
              </div>

              <Button disabled={saving || hasUploadingMedia || !content.trim() || selectedAccountIds.length === 0 || (!publishNow && !scheduledFor)}>
                {saving ? <Loader2 className="size-4 animate-spin" /> : <Send className="size-4" />}
                {publishNow ? 'Đăng qua Zernio' : 'Lên lịch qua Zernio'}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {activeTab === 'history' && (
        <Card className="overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Bài viết</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Lịch đăng</TableHead>
                <TableHead>Đã đăng</TableHead>
                <TableHead className="text-right">Link</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && posts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="py-12 text-center text-muted-foreground">
                    <Loader2 className="mx-auto mb-2 size-6 animate-spin text-primary" />
                    Đang tải...
                  </TableCell>
                </TableRow>
              ) : posts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="py-12 text-center text-muted-foreground">
                    <CalendarClock className="mx-auto mb-2 size-8 opacity-40" />
                    Chưa có lịch sử bài viết.
                  </TableCell>
                </TableRow>
              ) : (
                posts.map((post) => (
                  <TableRow key={post.id}>
                    <TableCell>
                      <div className="line-clamp-2 max-w-xl text-sm text-ink">{post.content || '—'}</div>
                      <div className="font-mono text-xs text-muted-foreground">{post.id}</div>
                    </TableCell>
                    <TableCell>{postStatusBadge(post.status)}</TableCell>
                    <TableCell>{formatDateTime(post.scheduledFor)}</TableCell>
                    <TableCell>{formatDateTime(post.publishedAt)}</TableCell>
                    <TableCell className="text-right">
                      {post.platformPostUrl ? (
                        <Button variant="ghost" size="icon" className="size-8" onClick={() => window.open(post.platformPostUrl || '#', '_blank', 'noopener,noreferrer')} aria-label="Mở bài viết">
                          <ExternalLink className="size-4" />
                        </Button>
                      ) : '—'}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </Card>
      )}

      <DeleteConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDisconnect}
        title="Ngắt kết nối fanpage"
        message={`Ngắt kết nối "${deleteTarget?.displayName || deleteTarget?.username || deleteTarget?.zernioAccountId}" khỏi danh sách quản lý?`}
      />
    </div>
  )
}
