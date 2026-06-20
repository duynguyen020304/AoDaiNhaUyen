import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { CalendarClock, Edit3, ExternalLink, Loader2, MessageCircle, MessageSquareText, RefreshCcw, Search, Send, Trash2, Upload, UsersRound } from 'lucide-react'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'
import { FacebookCommentsTab } from '@/components/facebook/FacebookCommentsTab'
import { FacebookMessagesTab } from '@/components/facebook/FacebookMessagesTab'
import { useFeedback } from '@/components/ui/feedbackContext'
import { HttpError } from '@/api/client'
import {
  createSocialPost,
  deleteSocialPost,
  disconnectSocialAccount,
  unpublishSocialPost,
  getSocialAccounts,
  getSocialPosts,
  uploadSocialMedia,
  selectFacebookPage,
  type SocialAccountConnection,
  type SocialPost,
} from '@/api/social'

type TabKey = 'fanpages' | 'composer' | 'history' | 'comments' | 'messages'

const TAB_KEYS: TabKey[] = ['fanpages', 'composer', 'history', 'comments', 'messages']

type PostDeleteTarget = {
  id: string
  content: string | null
  status: string | null
}

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
const CLONE_DRAFT_STORAGE_KEY = 'facebook_clone_post_draft'

type CloneDraft = {
  content: string | null
  mediaItems: Array<{ type: string | null; url: string }>
  accountIds: string[]
  sourcePostId: string
}

function formatDateTime(value: string | null) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function getErrorMessage(error: unknown) {
  const message = error instanceof HttpError
    ? error.message
    : error instanceof Error
      ? error.message
      : 'Đã xảy ra lỗi. Vui lòng thử lại.'
  return message
    .replaceAll('Zernio API lỗi:', 'Lỗi dịch vụ:')
    .replaceAll('Lỗi Zernio', 'Lỗi dịch vụ')
    .replaceAll('Zernio', 'dịch vụ')
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

function getBestPostUrl(post: SocialPost) {
  return post.platformPostUrl || (post.platforms ?? []).find((platform) => platform.platformPostUrl)?.platformPostUrl || null
}

function getBestPublishedAt(post: SocialPost) {
  return post.publishedAt || (post.platforms ?? []).find((platform) => platform.publishedAt)?.publishedAt || null
}

function isPublishedPost(post: SocialPost) {
  return post.status === 'published' || post.status === 'success'
}

function isVideoUrl(url: string) {
  return /\.(mp4|mov|webm)(\?|$)/i.test(url)
}

function mediaFileName(url: string) {
  try {
    const pathname = new URL(url).pathname
    return decodeURIComponent(pathname.split('/').filter(Boolean).pop() || url)
  } catch {
    return url.split('/').pop() || url
  }
}

function cloneDraftMediaToUploadedMedia(draft: CloneDraft): UploadedMedia[] {
  return (draft.mediaItems ?? [])
    .filter((item) => item?.url)
    .map((item) => ({
      id: crypto.randomUUID(),
      fileName: mediaFileName(item.url),
      contentType: item.type?.includes('video') || isVideoUrl(item.url) ? 'video/mp4' : 'image/jpeg',
      publicUrl: item.url,
      progress: 100,
      status: 'uploaded' as const,
    }))
}

function getPlatformSummary(post: SocialPost) {
  const platforms = post.platforms ?? []
  if (platforms.length === 0) return null
  return platforms.map((platform) => `${platform.platform}: ${platform.status || '—'}`).join(', ')
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
  const navigate = useNavigate()
  const { toast, confirm } = useFeedback()
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
  const [postDeleteTarget, setPostDeleteTarget] = useState<PostDeleteTarget | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [cloneDraftApplied, setCloneDraftApplied] = useState(false)

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
    try {
      const data = await getSocialAccounts('facebook', sync, targetProfileId || undefined)
      setAccounts(data)
      setSelectedAccountIds((current) => current.filter((id) => data.some((account) => account.id === id && account.isActive)))
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setLoading(false)
    }
  }, [profileId, toast])

  const loadPosts = useCallback(async () => {
    setLoading(true)
    try {
      const data = await getSocialPosts('facebook', undefined, profileId || undefined, 1, 25)
      setPosts(data.items)
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setLoading(false)
    }
  }, [profileId, toast])

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
      toast(`Không thể kết nối: ${oauthError}`, 'error')
      return
    }

    if (callbackProfileId) {
      setProfileId(callbackProfileId)
      window.localStorage.setItem(PROFILE_STORAGE_KEY, callbackProfileId)
    }

    setLoading(true)
    try {
      if (pageId && tempToken) {
        await selectFacebookPage({
          profileId: callbackProfileId || profileId,
          pageId,
          tempToken,
          redirectUrl: redirectUrl(),
          userProfile: {
            id: url.searchParams.get('userProfileId') || 'zernio-user',
            name: url.searchParams.get('userProfileName') || 'Người dùng',
            profilePicture: url.searchParams.get('userProfilePicture') || `${window.location.origin}/logo.svg`,
          },
        })
      }

      await loadAccounts(true, callbackProfileId || profileId)
      toast('Đã đồng bộ fanpage Facebook.', 'success')
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setLoading(false)
    }
  }, [loadAccounts, profileId, toast])

  useEffect(() => {
    const timeout = window.setTimeout(() => { void loadAccounts(false) }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadAccounts])

  useEffect(() => {
    const timeout = window.setTimeout(() => { void handleCallback() }, 0)
    return () => window.clearTimeout(timeout)
  }, [handleCallback])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      const tab = new URLSearchParams(window.location.search).get('tab')
      if (TAB_KEYS.includes(tab as TabKey)) setActiveTab(tab as TabKey)
    }, 0)
    return () => window.clearTimeout(timeout)
  }, [])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      if (cloneDraftApplied) return
      const rawDraft = window.sessionStorage.getItem(CLONE_DRAFT_STORAGE_KEY)
      if (!rawDraft) return

      let draft: CloneDraft
      try {
        draft = JSON.parse(rawDraft) as CloneDraft
      } catch {
        window.sessionStorage.removeItem(CLONE_DRAFT_STORAGE_KEY)
        setCloneDraftApplied(true)
        return
      }

      if ((draft.accountIds?.length ?? 0) > 0 && accounts.length === 0) return

      const selectedIds = (draft.accountIds ?? [])
        .map((externalId) => accounts.find((account) => account.zernioAccountId === externalId || account.id === externalId)?.id)
        .filter((id): id is string => Boolean(id))

      setActiveTab('composer')
      setContent(draft.content ?? '')
      setUploadedMedia(cloneDraftMediaToUploadedMedia(draft))
      setPublishNow(true)
      setScheduledFor('')
      setSelectedAccountIds(selectedIds)
      window.sessionStorage.removeItem(CLONE_DRAFT_STORAGE_KEY)
      setCloneDraftApplied(true)

      if ((draft.accountIds?.length ?? 0) > 0 && selectedIds.length === 0) {
        toast('Không tự khớp được fanpage. Vui lòng chọn fanpage trước khi đăng.', 'error')
      } else {
        toast('Đã tạo bản sao. Bạn có thể chỉnh nội dung/ảnh rồi đăng bài mới.', 'success')
      }
    }, 0)
    return () => window.clearTimeout(timeout)
  }, [accounts, cloneDraftApplied, toast])

  async function handleEditPost(post: SocialPost) {
    if (!isPublishedPost(post)) {
      navigate(`/admin/facebook/posts/${encodeURIComponent(post.id)}/edit`)
      return
    }

    const accepted = await confirm({
      title: 'Bài đã đăng không thể sửa trực tiếp',
      message: 'Để chỉnh sửa, cần tạo bản sao, tải/chọn lại ảnh nếu cần rồi đăng thành bài mới. Sau đó bạn có thể xóa bài cũ nếu nền tảng cho phép.',
      confirmText: 'Tạo bản sao',
      cancelText: 'Hủy',
    })
    if (!accepted) return
    navigate(`/admin/facebook/posts/${encodeURIComponent(post.id)}/edit?clone=1`)
  }

  async function handleSync() {
    const normalizedProfileId = profileId.trim()
    if (normalizedProfileId) window.localStorage.setItem(PROFILE_STORAGE_KEY, normalizedProfileId)
    await loadAccounts(true, normalizedProfileId)
    await loadPosts()
  }

  async function handleDisconnect() {
    if (!deleteTarget) return
    setSaving(true)
    try {
      await disconnectSocialAccount(deleteTarget.id)
      setDeleteTarget(null)
      toast('Đã ngắt kết nối fanpage.', 'success')
      await loadAccounts(false)
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setSaving(false)
    }
  }

  async function handleDeletePost() {
    if (!postDeleteTarget) return
    setSaving(true)
    try {
      if (postDeleteTarget.status === 'published' || postDeleteTarget.status === 'success') {
        await unpublishSocialPost(postDeleteTarget.id, 'facebook')
        setPosts((current) => current.map((post) => post.id === postDeleteTarget.id ? { ...post, status: 'cancelled' } : post))
        toast('Đã gỡ bài viết khỏi Facebook.', 'success')
      } else {
        await deleteSocialPost(postDeleteTarget.id)
        setPosts((current) => current.filter((post) => post.id !== postDeleteTarget.id))
        toast('Đã xóa bài viết.', 'success')
      }
      setPostDeleteTarget(null)
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setSaving(false)
    }
  }

  async function handleMediaUpload(files: FileList | null) {
    const selectedFiles = Array.from(files ?? [])
    if (selectedFiles.length === 0) return

    for (const file of selectedFiles) {
      const isVideo = file.type.startsWith('video/')
      const maxBytes = isVideo ? 200 * 1024 * 1024 : 10 * 1024 * 1024
      const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'video/mp4', 'video/quicktime', 'video/webm']
      if (!allowedTypes.includes(file.type)) {
        toast(`File ${file.name} không đúng định dạng. Chỉ hỗ trợ JPG, PNG, WEBP, GIF, MP4, MOV hoặc WEBM.`, 'error')
        continue
      }
      if (file.size <= 0 || file.size > maxBytes) {
        toast(isVideo ? `Video ${file.name} vượt quá 200MB.` : `Ảnh ${file.name} vượt quá 10MB.`, 'error')
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
        toast(getErrorMessage(err), 'error')
      }
    }
  }

  async function handleCreatePost(event: FormEvent) {
    event.preventDefault()
    if (selectedAccountIds.length === 0) {
      toast('Vui lòng chọn ít nhất một fanpage.', 'error')
      return
    }

    setSaving(true)
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
      toast(publishNow ? 'Đã gửi bài viết để đăng.' : 'Đã gửi lịch đăng.', 'success')
      setActiveTab('history')
      await loadPosts()
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setSaving(false)
    }
  }

  const changeTab = useCallback((tab: TabKey) => {
    setActiveTab(tab)
    const url = new URL(window.location.href)
    if (tab === 'fanpages') url.searchParams.delete('tab')
    else url.searchParams.set('tab', tab)
    window.history.replaceState({}, '', `${url.pathname}${url.search}${url.hash}`)
    if (tab === 'history') void loadPosts()
  }, [loadPosts])

  return (
    <div>
      <div className="mb-6 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Quản lý fanpage</h1>
          <p className="mt-1 text-sm text-muted-foreground">Đồng bộ fanpage Facebook, đăng bài và theo dõi lịch sử đăng.</p>
        </div>
        <Button variant="outline" onClick={() => void handleSync()} disabled={loading || saving}>
          {loading ? <Loader2 className="size-4 animate-spin" /> : <RefreshCcw className="size-4" />}
          Đồng bộ
        </Button>
      </div>

      <div className="mb-4 rounded-2xl border bg-white p-2 shadow-sm" role="tablist" aria-label="Facebook admin tabs">
        <div className="flex flex-wrap gap-2">
          {([
            ['fanpages', UsersRound, 'Fanpage'],
            ['composer', Send, 'Đăng bài'],
            ['history', CalendarClock, 'Lịch sử'],
            ['comments', MessageCircle, 'Bình luận'],
            ['messages', MessageSquareText, 'Tin nhắn'],
          ] as const).map(([key, Icon, label]) => (
            <button
              key={key}
              type="button"
              role="tab"
              aria-selected={activeTab === key}
              className={`inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition ${activeTab === key ? 'bg-primary text-primary-foreground shadow-sm' : 'text-muted-foreground hover:bg-primary/10 hover:text-primary'}`}
              onClick={() => changeTab(key)}
            >
              <Icon className="size-4" />
              {label}
            </button>
          ))}
        </div>
      </div>

      {activeTab === 'fanpages' && (
        <div role="tabpanel">
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
                  <TableHead>Account ID</TableHead>
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
                      Chưa có fanpage. Hãy kết nối để đồng bộ danh sách.
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
        </div>
      )}

      {activeTab === 'composer' && (
        <Card role="tabpanel">
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><Send className="size-5" />Đăng bài Facebook</CardTitle>
            <CardDescription>Đăng ngay hoặc lên lịch cho fanpage đã chọn.</CardDescription>
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
                  <p className="mt-1 text-xs text-muted-foreground">Tải ảnh/video lên hệ thống trước khi đăng. Ảnh tối đa 10MB, video tối đa 200MB.</p>
                </div>
                <label className="flex cursor-pointer items-center justify-center gap-2 rounded-lg border border-dashed border-primary/40 bg-primary/5 px-4 py-6 text-sm font-medium text-primary transition hover:bg-primary/10">
                  <Upload className="size-4" />
                  Tải ảnh/video
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
                            {item.status === 'uploading' ? `Đang tải ${item.progress}%` : item.status === 'uploaded' ? 'Đã tải lên' : 'Tải lên lỗi'}
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
                {publishNow ? 'Đăng bài' : 'Lên lịch đăng'}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {activeTab === 'history' && (
        <Card className="overflow-hidden" role="tabpanel">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Bài viết</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Lịch đăng</TableHead>
                <TableHead>Đã đăng</TableHead>
                <TableHead>Link</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && posts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="py-12 text-center text-muted-foreground">
                    <Loader2 className="mx-auto mb-2 size-6 animate-spin text-primary" />
                    Đang tải...
                  </TableCell>
                </TableRow>
              ) : posts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="py-12 text-center text-muted-foreground">
                    <CalendarClock className="mx-auto mb-2 size-8 opacity-40" />
                    Chưa có lịch sử bài viết.
                  </TableCell>
                </TableRow>
              ) : (
                posts.map((post) => {
                  const postUrl = getBestPostUrl(post)
                  const publishedAt = getBestPublishedAt(post)
                  return (
                    <TableRow key={post.id}>
                      <TableCell>
                        <button type="button" className="line-clamp-2 max-w-xl text-left text-sm text-ink hover:text-primary" onClick={() => void handleEditPost(post)}>
                          {post.content || '—'}
                        </button>
                        <div className="font-mono text-xs text-muted-foreground">{post.id}</div>
                        {getPlatformSummary(post) && <div className="mt-1 text-xs text-muted-foreground">{getPlatformSummary(post)}</div>}
                      </TableCell>
                      <TableCell>{postStatusBadge(post.status)}</TableCell>
                      <TableCell>{formatDateTime(post.scheduledFor)}</TableCell>
                      <TableCell>{formatDateTime(publishedAt)}</TableCell>
                      <TableCell>
                        {postUrl ? (
                          <Button variant="ghost" size="icon" className="size-8" onClick={() => window.open(postUrl, '_blank', 'noopener,noreferrer')} aria-label="Mở bài viết">
                            <ExternalLink className="size-4" />
                          </Button>
                        ) : '—'}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="icon" className="size-8" onClick={() => void handleEditPost(post)} aria-label="Chỉnh sửa bài viết">
                          <Edit3 className="size-4" />
                        </Button>
                        <Button variant="ghost" size="icon" className="size-8 text-destructive hover:text-destructive" onClick={() => setPostDeleteTarget({ id: post.id, content: post.content, status: post.status })} aria-label="Xóa bài viết">
                          <Trash2 className="size-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  )
                })
              )}
            </TableBody>
          </Table>
        </Card>
      )}

      {activeTab === 'comments' && (
        <div role="tabpanel">
          <FacebookCommentsTab accounts={accounts} profileId={profileId || undefined} onOpenFanpages={() => changeTab('fanpages')} />
        </div>
      )}

      {activeTab === 'messages' && (
        <div role="tabpanel">
          <FacebookMessagesTab accounts={accounts} profileId={profileId || undefined} onOpenFanpages={() => changeTab('fanpages')} />
        </div>
      )}

      <DeleteConfirmModal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDisconnect}
        title="Ngắt kết nối fanpage"
        message={`Ngắt kết nối "${deleteTarget?.displayName || deleteTarget?.username || deleteTarget?.zernioAccountId}" khỏi danh sách quản lý?`}
      />
      <DeleteConfirmModal
        open={!!postDeleteTarget}
        onClose={() => setPostDeleteTarget(null)}
        onConfirm={handleDeletePost}
        title={postDeleteTarget?.status === 'published' || postDeleteTarget?.status === 'success' ? 'Gỡ bài đã đăng' : 'Xóa bài viết'}
        message={postDeleteTarget?.status === 'published' || postDeleteTarget?.status === 'success'
          ? `Gỡ bài viết "${postDeleteTarget?.content || postDeleteTarget?.id}" khỏi Facebook? Bản ghi lịch sử vẫn được giữ.`
          : `Xóa bài viết "${postDeleteTarget?.content || postDeleteTarget?.id}"?`}
      />
    </div>
  )
}
