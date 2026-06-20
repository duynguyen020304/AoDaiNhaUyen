import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Loader2, MessageSquareText, Paperclip, RefreshCcw, Send, X } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Textarea } from '@/components/ui/textarea'
import { useFeedback } from '@/components/ui/feedbackContext'
import { HttpError } from '@/api/client'
import {
  getSocialConversationMessages,
  getSocialConversations,
  markSocialConversationRead,
  sendSocialConversationMessage,
  uploadSocialMedia,
  type SocialAccountConnection,
  type SocialConversation,
  type SocialMessage,
} from '@/api/social'
import { FacebookEmptyState } from './FacebookEmptyState'

type UploadedAttachment = {
  id: string
  url: string
  type: 'image' | 'video' | 'audio' | 'file'
  name: string
  previewUrl: string | null
}

interface FacebookMessagesTabProps {
  accounts: SocialAccountConnection[]
  profileId?: string
  onOpenFanpages: () => void
}

function errorMessage(error: unknown) {
  return error instanceof HttpError || error instanceof Error ? error.message : 'Đã xảy ra lỗi. Vui lòng thử lại.'
}

function formatTime(value: string | null) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' }).format(new Date(value))
}

function customerName(conversation: SocialConversation) {
  return conversation.participantName || 'Facebook User'
}

function isImageAttachment(type: string | null | undefined, url: string | null | undefined) {
  return type === 'image' || /\.(jpe?g|png|webp|gif)(\?|$)/i.test(url ?? '')
}

function MessageBubble({ message }: { message: SocialMessage }) {
  return (
    <div className={`flex ${message.direction === 'outgoing' ? 'justify-end' : 'justify-start'}`}>
      <div className={`max-w-[78%] rounded-2xl px-4 py-3 text-sm shadow-sm ${message.direction === 'outgoing' ? 'bg-primary text-primary-foreground' : 'bg-white text-ink'}`}>
        {message.text && <p className="whitespace-pre-wrap">{message.text}</p>}
        {message.attachments.length > 0 && (
          <div className="mt-2 space-y-2">
            {message.attachments.map((attachment, index) => isImageAttachment(attachment.type, attachment.url) && attachment.url ? (
              <img key={`${attachment.url}-${index}`} src={attachment.url} alt={attachment.fileName || 'Ảnh đính kèm'} className="max-h-64 rounded-xl object-cover" />
            ) : (
              <a key={`${attachment.url}-${index}`} href={attachment.url ?? '#'} target="_blank" rel="noreferrer" className="block rounded-lg border border-white/30 bg-white/20 p-2 text-xs underline-offset-2 hover:underline">
                {attachment.type || 'file'} · {attachment.fileName || 'Tệp đính kèm'}
              </a>
            ))}
          </div>
        )}
        <div className={`mt-1 text-[11px] ${message.direction === 'outgoing' ? 'text-primary-foreground/70' : 'text-muted-foreground'}`}>{formatTime(message.createdAt)}</div>
      </div>
    </div>
  )
}

export function FacebookMessagesTab({ accounts, profileId, onOpenFanpages }: FacebookMessagesTabProps) {
  const { toast } = useFeedback()
  const activeAccounts = useMemo(() => accounts.filter((account) => account.isActive), [accounts])
  const [accountId, setAccountId] = useState(() => activeAccounts[0]?.zernioAccountId ?? '')
  const [conversations, setConversations] = useState<SocialConversation[]>([])
  const [selectedConversationId, setSelectedConversationId] = useState('')
  const [messages, setMessages] = useState<SocialMessage[]>([])
  const [conversationCursor, setConversationCursor] = useState<string | null>(null)
  const [messageCursor, setMessageCursor] = useState<string | null>(null)
  const [loadingConversations, setLoadingConversations] = useState(false)
  const [loadingMessages, setLoadingMessages] = useState(false)
  const [sending, setSending] = useState(false)
  const [draft, setDraft] = useState('')
  const [attachments, setAttachments] = useState<UploadedAttachment[]>([])
  const [uploadingAttachment, setUploadingAttachment] = useState(false)

  useEffect(() => {
    if (accountId || !activeAccounts[0]) return undefined
    const timeout = window.setTimeout(() => setAccountId(activeAccounts[0].zernioAccountId), 0)
    return () => window.clearTimeout(timeout)
  }, [accountId, activeAccounts])

  const loadConversations = useCallback(async (cursor: string | null = null, append = false) => {
    if (!accountId) return
    setLoadingConversations(true)
    try {
      const data = await getSocialConversations('facebook', accountId, profileId, cursor, 25)
      setConversations((current) => append ? current.concat(data.items) : data.items)
      setConversationCursor(data.nextCursor)
      setSelectedConversationId((current) => current || data.items[0]?.id || '')
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setLoadingConversations(false)
    }
  }, [accountId, profileId, toast])

  const loadMessages = useCallback(async (cursor: string | null = null, append = false) => {
    if (!accountId || !selectedConversationId) return
    setLoadingMessages(true)
    try {
      const data = await getSocialConversationMessages(selectedConversationId, accountId, cursor, 50)
      const sorted = [...data.items].sort((a, b) => new Date(a.createdAt ?? 0).getTime() - new Date(b.createdAt ?? 0).getTime())
      setMessages((current) => append ? sorted.concat(current) : sorted)
      setMessageCursor(data.nextCursor)
      await markSocialConversationRead(selectedConversationId, accountId)
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setLoadingMessages(false)
    }
  }, [accountId, selectedConversationId, toast])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setConversations([])
      setSelectedConversationId('')
      setMessages([])
      setConversationCursor(null)
      setMessageCursor(null)
      void loadConversations(null, false)
    }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadConversations])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setMessages([])
      setMessageCursor(null)
      void loadMessages(null, false)
    }, 0)
    return () => window.clearTimeout(timeout)
  }, [loadMessages])

  const selectedConversation = conversations.find((conversation) => conversation.id === selectedConversationId) ?? null

  const mediaTypeFromContentType = (contentType: string): UploadedAttachment['type'] => {
    if (contentType.startsWith('image/')) return 'image'
    if (contentType.startsWith('video/')) return 'video'
    if (contentType.startsWith('audio/')) return 'audio'
    return 'file'
  }

  const handleAttachmentUpload = async (files: FileList | null | undefined) => {
    const selectedFiles = Array.from(files ?? [])
    if (selectedFiles.length === 0) return
    setUploadingAttachment(true)
    try {
      const file = selectedFiles[0]
      const media = await uploadSocialMedia(file)
      const type = mediaTypeFromContentType(media.contentType)
      setAttachments([{
        id: crypto.randomUUID(),
        url: media.publicUrl,
        type,
        name: media.fileName,
        previewUrl: type === 'image' ? media.publicUrl : null,
      }])
      toast('Đã tải media lên S3.', 'success')
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setUploadingAttachment(false)
    }
  }

  const removeAttachment = (id: string) => {
    setAttachments((current) => current.filter((attachment) => attachment.id !== id))
  }

  const clearAttachments = () => {
    setAttachments([])
  }

  const handleSend = async (event: FormEvent) => {
    event.preventDefault()
    if (!accountId || !selectedConversationId || (!draft.trim() && attachments.length === 0)) return
    setSending(true)
    try {
      const attachment = attachments[0]
      await sendSocialConversationMessage(selectedConversationId, accountId, attachment ? {
        message: draft.trim() || undefined,
        attachmentUrl: attachment.url,
        attachmentType: attachment.type,
      } : {
        message: draft.trim(),
      })
      clearAttachments()
      setDraft('')
      toast('Đã gửi tin nhắn.', 'success')
      await loadMessages(null, false)
      await loadConversations(null, false)
    } catch (error) {
      toast(errorMessage(error), 'error')
    } finally {
      setSending(false)
    }
  }

  if (activeAccounts.length === 0) {
    return (
      <FacebookEmptyState
        icon={<MessageSquareText className="size-10" />}
        title="Chưa có fanpage Zernio để xem tin nhắn"
        description="Hãy kết nối fanpage qua Zernio trước khi chăm sóc khách hàng qua inbox."
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
        <Button variant="outline" onClick={() => void loadConversations(null, false)} disabled={loadingConversations}>
          {loadingConversations ? <Loader2 className="size-4 animate-spin" /> : <RefreshCcw className="size-4" />}
          Tải hội thoại
        </Button>
      </div>

      <div className="grid h-[calc(100dvh-260px)] min-h-[560px] gap-4 lg:grid-cols-[360px_minmax(0,1fr)]">
        <Card className="overflow-hidden">
          <CardHeader className="border-b bg-white">
            <CardTitle className="text-base">Hội thoại</CardTitle>
          </CardHeader>
          <CardContent className="h-full overflow-auto p-0">
            {loadingConversations && conversations.length === 0 ? (
              <div className="p-8 text-center text-sm text-muted-foreground"><Loader2 className="mx-auto mb-2 size-5 animate-spin text-primary" />Đang tải...</div>
            ) : conversations.length === 0 ? (
              <div className="p-8 text-center text-sm text-muted-foreground">Chưa có cuộc trò chuyện phù hợp bộ lọc.</div>
            ) : (
              <div className="divide-y">
                {conversations.map((conversation) => (
                  <button key={conversation.id} type="button" className={`block w-full p-4 text-left transition hover:bg-primary/5 ${selectedConversationId === conversation.id ? 'bg-primary/10' : 'bg-white'}`} onClick={() => setSelectedConversationId(conversation.id)}>
                    <div className="flex items-start gap-3">
                      {conversation.participantPicture ? <img src={conversation.participantPicture} alt="" className="size-10 rounded-full object-cover" /> : <div className="flex size-10 items-center justify-center rounded-full bg-muted text-sm font-semibold text-ink">{customerName(conversation).slice(0, 1).toUpperCase()}</div>}
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center justify-between gap-2">
                          <div className="truncate font-medium text-ink">{customerName(conversation)}</div>
                          <span className="text-xs text-muted-foreground">{formatTime(conversation.updatedTime)}</span>
                        </div>
                        <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{conversation.lastMessage || '—'}</p>
                        {(conversation.unreadCount ?? 0) > 0 && <Badge className="mt-2">{conversation.unreadCount} chưa đọc</Badge>}
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            )}
            {conversationCursor && (
              <div className="border-t p-3">
                <Button variant="outline" className="w-full" disabled={loadingConversations} onClick={() => void loadConversations(conversationCursor, true)}>Tải thêm</Button>
              </div>
            )}
          </CardContent>
        </Card>

        <Card className="flex min-h-0 flex-col overflow-hidden">
          <CardHeader className="border-b bg-white">
            <CardTitle className="text-base">{selectedConversation ? customerName(selectedConversation) : 'Tin nhắn'}</CardTitle>
            {selectedConversation && <p className="text-xs text-muted-foreground">Đang trả lời qua Zernio · {selectedConversation.accountUsername || selectedConversation.accountId}</p>}
          </CardHeader>
          <CardContent className="flex min-h-0 flex-1 flex-col p-0">
            <div className="min-h-0 flex-1 space-y-3 overflow-auto bg-cream/40 p-4">
              {loadingMessages && messages.length === 0 ? (
                <div className="py-12 text-center text-sm text-muted-foreground"><Loader2 className="mx-auto mb-2 size-5 animate-spin text-primary" />Đang tải tin nhắn...</div>
              ) : !selectedConversationId ? (
                <div className="py-12 text-center text-sm text-muted-foreground">Chọn hội thoại để xem tin nhắn.</div>
              ) : messages.length === 0 ? (
                <div className="py-12 text-center text-sm text-muted-foreground">Chưa có tin nhắn trong hội thoại.</div>
              ) : (
                <>
                  {messageCursor && <Button variant="outline" className="w-full" disabled={loadingMessages} onClick={() => void loadMessages(messageCursor, true)}>Tải tin cũ hơn</Button>}
                  {messages.map((message) => <MessageBubble key={message.id} message={message} />)}
                </>
              )}
            </div>
            <form className="space-y-3 border-t bg-white p-4" onSubmit={handleSend}>
              {attachments.length > 0 && (
                <div className="rounded-xl border bg-cream/50 p-3">
                  <div className="mb-2 text-xs font-medium text-muted-foreground">Media sẵn sàng gửi</div>
                  <div className="flex max-h-32 gap-2 overflow-auto pb-1">
                    {attachments.map((attachment) => (
                      <div key={attachment.id} className="relative size-20 shrink-0 overflow-hidden rounded-lg border bg-white">
                        {attachment.previewUrl ? <img src={attachment.previewUrl} alt={attachment.name} className="size-full object-cover" /> : <div className="flex size-full items-center justify-center text-xs uppercase text-muted-foreground">{attachment.type}</div>}
                        <button type="button" className="absolute right-1 top-1 flex size-6 items-center justify-center rounded-full bg-black/60 text-white" onClick={() => removeAttachment(attachment.id)} aria-label="Xóa media">
                          <X className="size-3.5" />
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              )}
              <div className="flex items-end gap-2">
                <Textarea
                  className="min-h-12 flex-1 resize-none"
                  placeholder="Nhập tin nhắn..."
                  value={draft}
                  onChange={(event) => setDraft(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' && !event.shiftKey) {
                      event.preventDefault()
                      event.currentTarget.form?.requestSubmit()
                    }
                  }}
                  disabled={!selectedConversationId || sending || uploadingAttachment}
                />
                <label className={`inline-flex size-11 shrink-0 cursor-pointer items-center justify-center rounded-xl border border-primary/30 bg-primary/5 text-primary transition hover:bg-primary/10 ${!selectedConversationId || sending || uploadingAttachment ? 'pointer-events-none opacity-50' : ''}`} title="Tải ảnh/media lên S3" aria-label="Tải ảnh/media lên S3">
                  {uploadingAttachment ? <Loader2 className="size-5 animate-spin" /> : <Paperclip className="size-5" />}
                  <input type="file" className="hidden" accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/quicktime,video/webm" disabled={!selectedConversationId || sending || uploadingAttachment} onChange={(event) => { void handleAttachmentUpload(event.target.files); event.target.value = '' }} />
                </label>
                <Button className="size-11 self-end p-0" disabled={!selectedConversationId || sending || uploadingAttachment || (!draft.trim() && attachments.length === 0)} aria-label="Gửi tin nhắn">
                  {sending ? <Loader2 className="size-5 animate-spin" /> : <Send className="size-5" />}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
