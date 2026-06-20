import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Copy, ExternalLink, Loader2, Save, Trash2, Upload } from 'lucide-react'
import { DeleteConfirmModal } from '@/components/admin/DeleteConfirmModal'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Textarea } from '@/components/ui/textarea'
import { useFeedback } from '@/components/ui/feedbackContext'
import { HttpError } from '@/api/client'
import {
  deleteSocialPost,
  getSocialPost,
  unpublishSocialPost,
  updateSocialPost,
  uploadSocialMedia,
  type SocialPost,
  type SocialPostMedia,
} from '@/api/social'

const CLONE_DRAFT_STORAGE_KEY = 'facebook_clone_post_draft'

type CloneDraft = {
  content: string | null
  mediaItems: Array<{ type: string | null; url: string }>
  accountIds: string[]
  sourcePostId: string
}

type UploadedMedia = {
  id: string
  fileName: string
  contentType: string
  publicUrl: string
  progress: number
  status: 'uploading' | 'uploaded' | 'error'
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

function mediaContentType(item: SocialPostMedia) {
  const type = item.type?.toLowerCase() ?? ''
  if (type.includes('video') || isVideoUrl(item.url)) return 'video/mp4'
  return 'image/jpeg'
}

function mediaFileName(url: string) {
  try {
    const pathname = new URL(url).pathname
    return decodeURIComponent(pathname.split('/').filter(Boolean).pop() || url)
  } catch {
    return url.split('/').pop() || url
  }
}

function mapPostMediaToUploadedMedia(mediaItems: SocialPostMedia[] | undefined | null): UploadedMedia[] {
  return (mediaItems ?? [])
    .filter((item) => item?.url)
    .map((item) => ({
      id: crypto.randomUUID(),
      fileName: mediaFileName(item.url),
      contentType: mediaContentType(item),
      publicUrl: item.url,
      progress: 100,
      status: 'uploaded' as const,
    }))
}

export function FacebookPostEditPage() {
  const { postId = '' } = useParams()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { toast } = useFeedback()
  const [post, setPost] = useState<SocialPost | null>(null)
  const [content, setContent] = useState('')
  const [uploadedMedia, setUploadedMedia] = useState<UploadedMedia[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)

  const isCloneMode = searchParams.get('clone') === '1'
  const isPublished = post ? isPublishedPost(post) : false

  const mediaUrls = useMemo(() => uploadedMedia
    .filter((item) => item.status === 'uploaded' && item.publicUrl)
    .map((item) => item.publicUrl), [uploadedMedia])
  const cloneMediaItems = useMemo(() => uploadedMedia
    .filter((item) => item.status === 'uploaded' && item.publicUrl)
    .map((item) => ({
      type: item.contentType.startsWith('video/') ? 'video' : 'image',
      url: item.publicUrl,
    })), [uploadedMedia])
  const hasUploadingMedia = uploadedMedia.some((item) => item.status === 'uploading')
  const postUrl = post ? getBestPostUrl(post) : null

  const loadPost = useCallback(async () => {
    if (!postId) return
    setLoading(true)
    try {
      const detail = await getSocialPost(postId)
      setPost(detail)
      setContent(detail.content || '')
      setUploadedMedia(mapPostMediaToUploadedMedia(detail.mediaItems))
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setLoading(false)
    }
  }, [postId, toast])

  useEffect(() => {
    const timeout = window.setTimeout(() => { void loadPost() }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadPost])

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

  function saveCloneDraft() {
    if (!post) return
    const draft: CloneDraft = {
      content: content.trim(),
      mediaItems: cloneMediaItems,
      accountIds: (post.platforms ?? []).map((platform) => platform.accountId).filter(Boolean),
      sourcePostId: post.id,
    }
    window.sessionStorage.setItem(CLONE_DRAFT_STORAGE_KEY, JSON.stringify(draft))
    toast('Đã chuẩn bị bản sao. Vui lòng kiểm tra fanpage, nội dung và ảnh trước khi đăng bài mới.', 'success')
    navigate('/admin/facebook?tab=composer')
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!postId) return

    if (isCloneMode || isPublished) {
      saveCloneDraft()
      return
    }

    setSaving(true)
    try {
      const updated = await updateSocialPost(postId, { content: content.trim(), mediaUrls })
      setPost(updated)
      setContent(updated.content || '')
      const updatedMedia = mapPostMediaToUploadedMedia(updated.mediaItems)
      setUploadedMedia(updatedMedia.length > 0 ? updatedMedia : uploadedMedia.filter((item) => item.status === 'uploaded'))
      toast('Đã cập nhật bài viết.', 'success')
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete() {
    if (!postId) return
    setSaving(true)
    try {
      if (post?.status === 'published' || post?.status === 'success') {
        await unpublishSocialPost(postId, 'facebook')
        toast('Đã gỡ bài viết khỏi Facebook.', 'success')
      } else {
        await deleteSocialPost(postId)
        toast('Đã xóa bài viết.', 'success')
      }
      navigate('/admin/facebook', { replace: true })
    } catch (err) {
      toast(getErrorMessage(err), 'error')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Button type="button" variant="ghost" className="mb-2 px-0" onClick={() => navigate('/admin/facebook')}>
            <ArrowLeft className="size-4" />
            Quay lại lịch sử
          </Button>
          <h1 className="text-2xl font-bold tracking-tight text-ink">{isCloneMode || isPublished ? 'Tạo bản sao để đăng lại' : 'Chỉnh sửa bài viết'}</h1>
          <p className="mt-1 font-mono text-xs text-muted-foreground">{postId}</p>
        </div>
        {postUrl && (
          <Button type="button" variant="outline" onClick={() => window.open(postUrl, '_blank', 'noopener,noreferrer')}>
            <ExternalLink className="size-4" />
            Mở link
          </Button>
        )}
      </div>

      {(isCloneMode || isPublished) && post && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          Bài đã đăng không thể sửa trực tiếp. Trang này dùng để tạo bản sao, chỉnh nội dung/ảnh, sau đó chuyển sang form đăng bài mới. Bài cũ vẫn giữ nguyên; bạn có thể xóa bài cũ sau nếu nền tảng cho phép.
        </div>
      )}

      {loading ? (
        <Card>
          <CardContent className="py-16 text-center text-muted-foreground">
            <Loader2 className="mx-auto mb-2 size-6 animate-spin text-primary" />
            Đang tải bài viết...
          </CardContent>
        </Card>
      ) : post ? (
        <form className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]" onSubmit={handleSubmit}>
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Nội dung</CardTitle>
                <CardDescription>{isCloneMode || isPublished ? 'Chỉnh nội dung cho bản sao trước khi tạo bài mới.' : 'Cập nhật nội dung bài viết. Bài đã đăng có thể bị Facebook giới hạn chỉnh sửa.'}</CardDescription>
              </CardHeader>
              <CardContent>
                <Textarea className="min-h-48" value={content} onChange={(event) => setContent(event.target.value)} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Ảnh/video</CardTitle>
                <CardDescription>Ảnh cũ hiển thị ở đây. Xóa/thêm media rồi bấm {isCloneMode || isPublished ? 'Tạo bản sao' : 'Cập nhật'} để lưu thay đổi.</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <label className="flex cursor-pointer items-center justify-center gap-2 rounded-lg border border-dashed border-primary/40 bg-primary/5 px-4 py-6 text-sm font-medium text-primary transition hover:bg-primary/10">
                  <Upload className="size-4" />
                  Thêm ảnh/video
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

                {uploadedMedia.length === 0 ? (
                  <div className="rounded-lg border border-dashed py-10 text-center text-sm text-muted-foreground">
                    Bài viết chưa có ảnh/video hoặc chưa đồng bộ được media.
                  </div>
                ) : (
                  <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                    {uploadedMedia.map((item) => (
                      <div key={item.id} className="overflow-hidden rounded-xl border bg-white">
                        {item.publicUrl && item.contentType.startsWith('image/') ? (
                          <img src={item.publicUrl} alt={item.fileName} className="h-44 w-full object-cover" />
                        ) : item.publicUrl && item.contentType.startsWith('video/') ? (
                          <video src={item.publicUrl} className="h-44 w-full bg-black object-contain" controls />
                        ) : (
                          <div className="flex h-44 items-center justify-center bg-muted text-xs uppercase text-muted-foreground">
                            {item.contentType.startsWith('video/') ? 'VID' : 'IMG'}
                          </div>
                        )}
                        <div className="space-y-2 p-3 text-sm">
                          <div className="truncate font-medium text-ink" title={item.fileName}>{item.fileName}</div>
                          <div className="h-1.5 overflow-hidden rounded-full bg-muted">
                            <div className={`h-full ${item.status === 'error' ? 'bg-destructive' : 'bg-primary'}`} style={{ width: `${item.progress}%` }} />
                          </div>
                          <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground">
                            <span>{item.status === 'uploading' ? `Đang upload ${item.progress}%` : item.status === 'uploaded' ? 'Sẵn sàng' : 'Upload lỗi'}</span>
                            <Button type="button" variant="ghost" size="icon" className="size-8 text-destructive hover:text-destructive" onClick={() => setUploadedMedia((current) => current.filter((media) => media.id !== item.id))} aria-label="Xóa media">
                              <Trash2 className="size-4" />
                            </Button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Trạng thái</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div className="flex items-center justify-between gap-3">
                  <span className="text-muted-foreground">Trạng thái</span>
                  {postStatusBadge(post.status)}
                </div>
                <div className="flex items-center justify-between gap-3">
                  <span className="text-muted-foreground">Lịch đăng</span>
                  <span>{formatDateTime(post.scheduledFor)}</span>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <span className="text-muted-foreground">Đã đăng</span>
                  <span>{formatDateTime(getBestPublishedAt(post))}</span>
                </div>
              </CardContent>
            </Card>

            {post.platforms?.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle>Nền tảng</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {post.platforms.map((platform, index) => (
                    <div key={`${platform.platform}-${platform.accountId}-${index}`} className="rounded-lg border p-3 text-sm">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="font-medium text-ink">{platform.platform}</span>
                        {postStatusBadge(platform.status)}
                      </div>
                      <div className="mt-2 font-mono text-xs text-muted-foreground">{platform.accountId}</div>
                      <div className="mt-2 text-xs text-muted-foreground">Đã đăng: {formatDateTime(platform.publishedAt)}</div>
                      {platform.errorMessage && <div className="mt-2 text-xs text-destructive">{platform.errorMessage}</div>}
                      {platform.platformPostUrl && (
                        <Button type="button" variant="link" className="mt-2 h-auto p-0" onClick={() => window.open(platform.platformPostUrl || '#', '_blank', 'noopener,noreferrer')}>
                          Mở link bài viết
                        </Button>
                      )}
                    </div>
                  ))}
                </CardContent>
              </Card>
            )}

            <Card>
              <CardContent className="flex flex-col gap-3 pt-6">
                <Button type="submit" disabled={saving || hasUploadingMedia || (!content.trim() && mediaUrls.length === 0)}>
                  {saving ? <Loader2 className="size-4 animate-spin" /> : isCloneMode || isPublished ? <Copy className="size-4" /> : <Save className="size-4" />}
                  {isCloneMode || isPublished ? 'Tạo bản sao' : 'Cập nhật'}
                </Button>
                <Button type="button" variant="destructive" disabled={saving} onClick={() => setConfirmDelete(true)}>
                  <Trash2 className="size-4" />
                  {post.status === 'published' || post.status === 'success' ? 'Gỡ bài đã đăng' : 'Xóa bài viết'}
                </Button>
              </CardContent>
            </Card>
          </div>
        </form>
      ) : (
        <Card>
          <CardContent className="py-16 text-center text-muted-foreground">
            Không tìm thấy bài viết.
          </CardContent>
        </Card>
      )}

      <DeleteConfirmModal
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        onConfirm={handleDelete}
        title={post?.status === 'published' || post?.status === 'success' ? 'Gỡ bài đã đăng' : 'Xóa bài viết'}
        message={post?.status === 'published' || post?.status === 'success'
          ? `Gỡ bài viết "${post?.content || postId}" khỏi Facebook? Bản ghi lịch sử vẫn được giữ.`
          : `Xóa bài viết "${post?.content || postId}"?`}
      />
    </div>
  )
}
