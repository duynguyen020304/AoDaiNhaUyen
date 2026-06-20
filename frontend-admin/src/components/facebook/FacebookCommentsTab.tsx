import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ExternalLink, Eye, EyeOff, Loader2, MessageCircle, RefreshCcw, Send, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Textarea } from '@/components/ui/textarea'
import { useFeedback } from '@/components/ui/feedbackContext'
import { HttpError } from '@/api/client'
import {
  deleteSocialComment,
  getSocialCommentedPosts,
  getSocialComments,
  replySocialComment,
  toggleSocialCommentHidden,
  type SocialAccountConnection,
  type SocialComment,
  type SocialCommentedPost,
} from '@/api/social'
import { FacebookEmptyState } from './FacebookEmptyState'

interface FacebookCommentsTabProps {
  accounts: SocialAccountConnection[]
  profileId?: string
  onOpenFanpages: () => void
}

function errorMessage(error: unknown) {
  return error instanceof HttpError || error instanceof Error ? error.message : 'Đã xảy ra lỗi. Vui lòng thử lại.'
}

function formatDate(value: string | null) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function displayPost(post: SocialCommentedPost) {
  return post.content || post.id
}

function CommentCard({
  comment,
  level = 0,
  busy,
  onReply,
  onToggleHidden,
  onDelete,
}: {
  comment: SocialComment
  level?: number
  busy: boolean
  onReply: (comment: SocialComment) => void
  onToggleHidden: (comment: SocialComment) => void
  onDelete: (comment: SocialComment) => void
}) {
  return (
    <div className={level > 0 ? 'ml-6 border-l border-border pl-4' : ''}>
      <article className="rounded-xl border bg-white p-4 shadow-sm">
        <div className="flex gap-3">
          {comment.author?.picture ? (
            <img src={comment.author.picture} alt="" className="size-10 rounded-full object-cover" />
          ) : (
            <div className="flex size-10 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
              {(comment.author?.name || '?').slice(0, 1).toUpperCase()}
            </div>
          )}
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h4 className="font-semibold text-ink">{comment.author?.name || 'Người dùng Facebook'}</h4>
              {comment.isHidden && <Badge variant="outline" className="border-amber-300 bg-amber-50 text-amber-700">Đã ẩn</Badge>}
            </div>
            <p className="mt-1 whitespace-pre-wrap text-sm text-ink">{comment.message || '—'}</p>
            <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
              <span>{formatDate(comment.createdTime)}</span>
              <span>• {comment.likeCount ?? 0} thích</span>
              <Button variant="ghost" size="sm" className="h-7 px-2 text-primary" disabled={busy} onClick={() => onReply(comment)}>
                Trả lời
              </Button>
              <Button variant="ghost" size="sm" className="h-7 px-2" disabled={busy} onClick={() => onToggleHidden(comment)}>
                {comment.isHidden ? <Eye className="size-3.5" /> : <EyeOff className="size-3.5" />}
                {comment.isHidden ? 'Hiện' : 'Ẩn'}
              </Button>
              <Button variant="ghost" size="sm" className="h-7 px-2 text-destructive hover:text-destructive" disabled={busy} onClick={() => onDelete(comment)}>
                <Trash2 className="size-3.5" />
                Xóa
              </Button>
            </div>
          </div>
        </div>
      </article>
      {comment.replies?.map((reply) => (
        <div key={reply.id} className="mt-3">
          <CommentCard comment={reply} level={level + 1} busy={busy} onReply={onReply} onToggleHidden={onToggleHidden} onDelete={onDelete} />
        </div>
      ))}
    </div>
  )
}

export function FacebookCommentsTab({ accounts, profileId, onOpenFanpages }: FacebookCommentsTabProps) {
  const { toast, confirm } = useFeedback()
  const activeAccounts = useMemo(() => accounts.filter((account) => account.isActive), [accounts])
  const [accountId, setAccountId] = useState(() => activeAccounts[0]?.zernioAccountId ?? '')
  const [posts, setPosts] = useState<SocialCommentedPost[]>([])
  const [selectedPostId, setSelectedPostId] = useState('')
  const [comments, setComments] = useState<SocialComment[]>([])
  const [postCursor, setPostCursor] = useState<string | null>(null)
  const [commentCursor, setCommentCursor] = useState<string | null>(null)
  const [loadingPosts, setLoadingPosts] = useState(false)
  const [loadingComments, setLoadingComments] = useState(false)
  const [replyTarget, setReplyTarget] = useState<SocialComment | null>(null)
  const [draft, setDraft] = useState('')
  const [busyCommentId, setBusyCommentId] = useState<string | null>(null)

  useEffect(() => {
    if (accountId || !activeAccounts[0]) return undefined
    const timeout = window.setTimeout(() => setAccountId(activeAccounts[0].zernioAccountId), 0)
    return () => window.clearTimeout(timeout)
  }, [accountId, activeAccounts])

  const loadPosts = useCallback(async (cursor: string | null = null, append = false) => {
    if (!accountId) return
    setLoadingPosts(true)
    try {
      const data = await getSocialCommentedPosts('facebook', accountId, profileId, cursor, 25)
      setPosts((current) => append ? current.concat(data.items) : data.items)
      setPostCursor(data.nextCursor)
      setSelectedPostId((current) => current || data.items[0]?.id || '')
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setLoadingPosts(false)
    }
  }, [accountId, profileId, toast])

  const loadComments = useCallback(async (cursor: string | null = null, append = false) => {
    if (!accountId || !selectedPostId) return
    setLoadingComments(true)
    try {
      const data = await getSocialComments(selectedPostId, accountId, cursor, 50)
      setComments((current) => append ? current.concat(data.items) : data.items)
      setCommentCursor(data.nextCursor)
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setLoadingComments(false)
    }
  }, [accountId, selectedPostId, toast])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPosts([])
      setSelectedPostId('')
      setComments([])
      setPostCursor(null)
      setCommentCursor(null)
      void loadPosts(null, false)
    }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadPosts])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setComments([])
      setCommentCursor(null)
      setReplyTarget(null)
      setDraft('')
      void loadComments(null, false)
    }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadComments])

  const selectedPost = posts.find((post) => post.id === selectedPostId) ?? null

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!accountId || !selectedPostId || !draft.trim()) return
    setBusyCommentId(replyTarget?.id ?? 'post')
    try {
      if (replyTarget) {
        await replySocialComment(selectedPostId, accountId, draft.trim(), replyTarget.id)
        toast('Đã trả lời bình luận.', 'success')
      } else {
        await replySocialComment(selectedPostId, accountId, draft.trim())
        toast('Đã bình luận bài viết.', 'success')
      }
      setDraft('')
      setReplyTarget(null)
      await loadComments(null, false)
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setBusyCommentId(null)
    }
  }

  const handleToggleHidden = async (comment: SocialComment) => {
    setBusyCommentId(comment.id)
    try {
      await toggleSocialCommentHidden(selectedPostId, accountId, comment.id, !comment.isHidden)
      toast(comment.isHidden ? 'Đã hiện bình luận.' : 'Đã ẩn bình luận.', 'success')
      await loadComments(null, false)
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setBusyCommentId(null)
    }
  }

  const handleDelete = async (comment: SocialComment) => {
    const ok = await confirm({
      title: 'Xóa bình luận',
      message: `Xóa bình luận "${comment.message || comment.id}"? Hành động này không thể hoàn tác trên Facebook.`,
      confirmText: 'Xóa',
      destructive: true,
    })
    if (!ok) return

    setBusyCommentId(comment.id)
    try {
      await deleteSocialComment(selectedPostId, accountId, comment.id)
      toast('Đã xóa bình luận.', 'success')
      await loadComments(null, false)
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setBusyCommentId(null)
    }
  }

  if (activeAccounts.length === 0) {
    return (
      <FacebookEmptyState
        icon={<MessageCircle className="size-10" />}
        title="Chưa có fanpage Zernio để xem bình luận"
        description="Hãy kết nối fanpage qua Zernio trước khi quản lý bình luận."
        action={<Button onClick={onOpenFanpages}>Mở tab Fanpage</Button>}
      />
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3 rounded-2xl border bg-white p-4 shadow-sm">
        <label className="text-sm font-medium text-ink">
          Fanpage
          <select className="ml-2 rounded-lg border bg-white px-3 py-2 text-sm" value={accountId} onChange={(event) => setAccountId(event.target.value)}>
            {activeAccounts.map((account) => (
              <option key={account.id} value={account.zernioAccountId}>{account.displayName || account.username || account.zernioAccountId}</option>
            ))}
          </select>
        </label>
        <Button variant="outline" onClick={() => void loadPosts(null, false)} disabled={loadingPosts}>
          {loadingPosts ? <Loader2 className="size-4 animate-spin" /> : <RefreshCcw className="size-4" />}
          Tải bài viết
        </Button>
      </div>

      <div className="grid h-[calc(100dvh-260px)] min-h-[560px] gap-4 lg:grid-cols-[360px_minmax(0,1fr)]">
        <Card className="overflow-hidden">
          <CardHeader className="border-b bg-white">
            <CardTitle className="text-base">Bài viết</CardTitle>
          </CardHeader>
          <CardContent className="h-full overflow-auto p-0">
            {loadingPosts && posts.length === 0 ? (
              <div className="p-8 text-center text-sm text-muted-foreground"><Loader2 className="mx-auto mb-2 size-5 animate-spin text-primary" />Đang tải...</div>
            ) : posts.length === 0 ? (
              <div className="p-8 text-center text-sm text-muted-foreground">Chưa có bài viết phù hợp.</div>
            ) : (
              <div className="divide-y">
                {posts.map((post) => (
                  <button key={post.id} type="button" className={`block w-full p-4 text-left transition hover:bg-primary/5 ${selectedPostId === post.id ? 'bg-primary/10' : 'bg-white'}`} onClick={() => setSelectedPostId(post.id)}>
                    {post.picture && <img src={post.picture} alt="" className="mb-3 h-28 w-full rounded-lg object-cover" />}
                    <div className="line-clamp-2 text-sm font-medium text-ink">{displayPost(post)}</div>
                    <div className="mt-2 text-xs text-muted-foreground">{formatDate(post.createdTime)}</div>
                  </button>
                ))}
              </div>
            )}
            {postCursor && (
              <div className="border-t p-3">
                <Button variant="outline" className="w-full" disabled={loadingPosts} onClick={() => void loadPosts(postCursor, true)}>Tải thêm</Button>
              </div>
            )}
          </CardContent>
        </Card>

        <Card className="flex min-h-0 flex-col overflow-hidden">
          <CardHeader className="border-b bg-white">
            <div className="flex items-start justify-between gap-3">
              <div>
                <CardTitle className="text-base">Bình luận ({comments.length})</CardTitle>
                {selectedPost && <p className="mt-1 line-clamp-1 text-sm text-muted-foreground">{displayPost(selectedPost)}</p>}
              </div>
              {selectedPost?.permalink && (
                <Button variant="ghost" size="icon" onClick={() => window.open(selectedPost.permalink!, '_blank', 'noopener,noreferrer')} aria-label="Mở bài viết Facebook">
                  <ExternalLink className="size-4" />
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent className="flex min-h-0 flex-1 flex-col p-0">
            <div className="min-h-0 flex-1 space-y-3 overflow-auto bg-cream/40 p-4">
              {loadingComments && comments.length === 0 ? (
                <div className="py-12 text-center text-sm text-muted-foreground"><Loader2 className="mx-auto mb-2 size-5 animate-spin text-primary" />Đang tải bình luận...</div>
              ) : !selectedPostId ? (
                <div className="py-12 text-center text-sm text-muted-foreground">Chọn bài viết để xem bình luận.</div>
              ) : comments.length === 0 ? (
                <div className="py-12 text-center text-sm text-muted-foreground">Chưa có bình luận phù hợp bộ lọc.</div>
              ) : comments.map((comment) => (
                <CommentCard key={comment.id} comment={comment} busy={busyCommentId === comment.id} onReply={setReplyTarget} onToggleHidden={handleToggleHidden} onDelete={handleDelete} />
              ))}
              {commentCursor && (
                <Button variant="outline" className="w-full" disabled={loadingComments} onClick={() => void loadComments(commentCursor, true)}>Tải thêm bình luận</Button>
              )}
            </div>
            <form className="border-t bg-white p-4" onSubmit={handleSubmit}>
              {replyTarget && (
                <div className="mb-2 flex items-center justify-between rounded-lg bg-primary/10 px-3 py-2 text-xs text-primary">
                  <span>Đang trả lời: {replyTarget.author?.name || replyTarget.id}</span>
                  <button type="button" onClick={() => setReplyTarget(null)}>Hủy</button>
                </div>
              )}
              <div className="flex gap-2">
                <Textarea
                  className="min-h-12 flex-1 resize-none"
                  placeholder="Viết bình luận..."
                  value={draft}
                  onChange={(event) => setDraft(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' && !event.shiftKey) {
                      event.preventDefault()
                      event.currentTarget.form?.requestSubmit()
                    }
                  }}
                  disabled={!selectedPostId || !!busyCommentId}
                />
                <Button className="self-end" disabled={!draft.trim() || !selectedPostId || !!busyCommentId}>
                  {busyCommentId ? <Loader2 className="size-4 animate-spin" /> : <Send className="size-4" />}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
