import { useEffect, useRef, useState, useLayoutEffect } from 'react'
import { FileText, Loader2, Paperclip, X } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { MessageBubble } from './MessageBubble'
import { ChatInput } from './ChatInput'
import { EmptyChat } from './EmptyChat'
import {
  CONVERSATION_MEDIA_ACCEPT,
  ConversationMediaModal,
  loadConversationMedia,
  persistConversationMedia,
  uploadConversationMediaFiles,
  type ConversationMediaItem,
} from './ConversationMediaModal'

function buildAttachmentPrompt(items: ConversationMediaItem[]) {
  if (items.length === 0) return ''

  const lines = items.map((item, index) => {
    if (item.publicUrl) {
      return `${index + 1}. ${item.fileName} (${item.contentType}) - URL: ${item.publicUrl}`
    }
    return `${index + 1}. ${item.fileName} (${item.contentType}) - Chưa có URL public do upload lỗi/chưa hoàn tất.`
  })

  return `\n\nMedia đính kèm:\n${lines.join('\n')}\n\nGhi chú: Ảnh/video/PDF/Excel/CSV đều được upload lên S3 nếu có URL. Ảnh có thể xem trực tiếp từ URL. Với PDF/Excel/CSV có URL, AI phải ưu tiên gọi tool read_uploaded_document để trích xuất nội dung trước khi trả lời.`
}

export function FullChatArea() {
  const {
    messages,
    isLoading,
    sendMessage,
    conversationId,
    activeConversationId,
    conversationSuggestions,
    isLoadingConversationSuggestions,
    fetchConversationSuggestions,
  } = useAdminAiStore()
  const [input, setInput] = useState('')
  const [mediaModalOpen, setMediaModalOpen] = useState(false)
  const [attachedMedia, setAttachedMedia] = useState<ConversationMediaItem[]>([])
  const [mediaError, setMediaError] = useState<string | null>(null)
  const uploadInputRef = useRef<HTMLInputElement>(null)
  const scrollRef = useRef<HTMLDivElement>(null)

  const shouldStickToBottomRef = useRef(true)

  useEffect(() => {
    if (conversationId || activeConversationId) {
      void fetchConversationSuggestions()
    }
  }, [activeConversationId, conversationId, fetchConversationSuggestions])

  useLayoutEffect(() => {
    const node = scrollRef.current
    if (!node || !shouldStickToBottomRef.current) return

    const frameId = window.requestAnimationFrame(() => {
      node.scrollTop = node.scrollHeight
    })

    return () => window.cancelAnimationFrame(frameId)
  }, [messages, isLoading])

  function handleScroll() {
    const node = scrollRef.current
    if (!node) return
    shouldStickToBottomRef.current = node.scrollHeight - node.scrollTop - node.clientHeight < 120
  }

  async function handleSend() {
    if ((!input.trim() && attachedMedia.length === 0) || isLoading) return
    const msg = `${input || 'Hãy xử lý các media đính kèm.'}${buildAttachmentPrompt(attachedMedia)}`
    shouldStickToBottomRef.current = true
    setInput('')
    setAttachedMedia([])
    await sendMessage({ message: msg })
  }

  async function handleSuggestionClick(message: string) {
    shouldStickToBottomRef.current = true
    setInput('')
    await sendMessage({ message })
  }

  async function handleUploadFiles(files: FileList | null) {
    if (!files || files.length === 0) return
    setMediaError(null)
    const { items, errors } = await uploadConversationMediaFiles(files)
    if (items.length > 0) {
      setAttachedMedia((current) => {
        const next = [...items, ...current.filter((item) => !items.some((uploaded) => uploaded.id === item.id))]
        return next
      })
    }
    if (errors.length > 0) setMediaError(errors.join('\n'))
    if (uploadInputRef.current) uploadInputRef.current.value = ''
  }

  function handleDeleteStoredMedia(id: string) {
    setAttachedMedia((current) => current.filter((item) => item.id !== id))
    persistConversationMedia(loadConversationMedia().filter((item) => item.id !== id))
  }

  const hasMessages = messages.length > 0

  return (
    <div className="flex flex-col h-full relative bg-white">
      <div className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-3">
        <div>
          <h2 className="text-sm font-semibold text-gray-900">Trợ lý AI Admin</h2>
          <p className="text-xs text-gray-500">Chat AI admin mặc định.</p>
        </div>
        <button
          type="button"
          onClick={() => setMediaModalOpen(true)}
          className="inline-flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50"
          aria-label="Mở media cuộc trò chuyện"
        >
          <Paperclip className="size-4" />
          Kho media
          {attachedMedia.length > 0 && <span className="rounded-full bg-wine px-2 py-0.5 text-xs text-white">{attachedMedia.length}</span>}
        </button>
      </div>

      {/* Messages or Empty State */}
      {hasMessages ? (
        <div ref={scrollRef} onScroll={handleScroll} className="flex-1 overflow-y-auto px-6 py-6 [overflow-anchor:none]">
          <div className="max-w-[95%] mx-auto space-y-6">
            {messages.map((msg) => (
              <MessageBubble key={msg.id} message={msg} />
            ))}
            {isLoading && (
              <div className="flex justify-center py-2">
                <Loader2 className="size-5 animate-spin text-wine/50" />
              </div>
            )}
          </div>
        </div>
      ) : (
        <EmptyChat onSuggestionClick={handleSuggestionClick} isLoading={isLoading} />
      )}

      {/* Input — always visible */}
      <div className="border-t border-gray-200 p-4 bg-white shrink-0">
        <div className="max-w-[95%] mx-auto">
          {hasMessages && (conversationSuggestions.length > 0 || isLoadingConversationSuggestions) && (
            <div className="mb-3 grid gap-2 sm:grid-cols-2">
              {isLoadingConversationSuggestions && conversationSuggestions.length === 0 ? (
                <span className="text-xs text-gray-400">Đang gợi ý tin nhắn...</span>
              ) : (
                conversationSuggestions.slice(0, 4).map((suggestion) => (
                  <button
                    key={suggestion}
                    type="button"
                    onClick={() => setInput(suggestion)}
                    disabled={isLoading}
                    className="rounded-2xl border border-gray-200 bg-gray-50 px-3 py-2 text-left text-xs text-gray-700 transition hover:border-wine/30 hover:bg-wine/5 disabled:opacity-50"
                  >
                    {suggestion}
                  </button>
                ))
              )}
            </div>
          )}
          <input
            ref={uploadInputRef}
            type="file"
            multiple
            accept={CONVERSATION_MEDIA_ACCEPT}
            className="hidden"
            onChange={(event) => void handleUploadFiles(event.target.files)}
          />
          {mediaError && <p className="mb-2 whitespace-pre-line text-xs text-red-600">{mediaError}</p>}
          {attachedMedia.length > 0 && (
            <div className="mb-3 flex flex-wrap gap-2">
              {attachedMedia.map((item) => (
                <span key={item.id} className="inline-flex max-w-full items-center gap-2 rounded-full border border-wine/20 bg-wine/5 px-3 py-1.5 text-xs text-wine">
                  <FileText className="size-3.5 shrink-0" />
                  <span className="truncate">{item.fileName}</span>
                  <button type="button" onClick={() => setAttachedMedia((current) => current.filter((media) => media.id !== item.id))} aria-label="Bỏ đính kèm">
                    <X className="size-3.5" />
                  </button>
                </span>
              ))}
            </div>
          )}
          <ChatInput
            value={input}
            onChange={setInput}
            onSend={handleSend}
            isLoading={isLoading}
            placeholder="Nhập yêu cầu..."
            onUploadClick={() => uploadInputRef.current?.click()}
            uploadCount={attachedMedia.length}
          />
        </div>
      </div>
      <ConversationMediaModal
        open={mediaModalOpen}
        selectedUrls={attachedMedia.map((item) => item.publicUrl).filter(Boolean)}
        onClose={() => setMediaModalOpen(false)}
        onDelete={handleDeleteStoredMedia}
        onApply={(items) => {
          setAttachedMedia(items)
          setMediaModalOpen(false)
        }}
      />
    </div>
  )
}
