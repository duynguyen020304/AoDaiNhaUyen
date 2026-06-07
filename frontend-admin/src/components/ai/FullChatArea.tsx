import { useState, useRef, useEffect } from 'react'
import { Bot, X, Loader2 } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { MessageBubble } from './MessageBubble'
import { ChatInput } from './ChatInput'
import { EmptyChat } from './EmptyChat'

interface FullChatAreaProps {
  onClear: () => void
}

export function FullChatArea({ onClear }: FullChatAreaProps) {
  const { messages, isLoading, sendMessage } = useAdminAiStore()
  const [input, setInput] = useState('')
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [messages, isLoading])

  async function handleSend() {
    if (!input.trim() || isLoading) return
    const msg = input
    setInput('')
    await sendMessage({ message: msg })
  }

  function handleSuggestionClick(message: string) {
    setInput(message)
    // Fire-and-forget the suggestion
    sendMessage({ message })
  }

  const hasMessages = messages.length > 0

  return (
    <div className="flex flex-col h-[calc(100dvh-65px)]">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-white shrink-0">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-wine/10 rounded-lg">
            <Bot className="size-5 text-wine" />
          </div>
          <div>
            <h1 className="text-lg font-semibold text-gray-800">Trợ lý AI Admin</h1>
            <p className="text-xs text-gray-500">Sẵn sàng hỗ trợ bạn</p>
          </div>
        </div>
        {hasMessages && (
          <button
            onClick={onClear}
            className="flex items-center gap-1 px-3 py-1.5 text-xs text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
            aria-label="Xóa hội thoại"
          >
            <X className="size-3" />
            Xóa hội thoại
          </button>
        )}
      </div>

      {/* Messages or Empty State */}
      {hasMessages ? (
        <div ref={scrollRef} className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
          {messages.map((msg) => (
            <MessageBubble key={msg.id} message={msg} />
          ))}
          {isLoading && (
            <div className="flex justify-center py-2">
              <Loader2 className="size-4 animate-spin text-gray-400" />
            </div>
          )}
        </div>
      ) : (
        <EmptyChat onSuggestionClick={handleSuggestionClick} isLoading={isLoading} />
      )}

      {/* Input — always visible */}
      <div className="border-t border-gray-200 p-4 bg-white shrink-0">
        <div className="max-w-3xl mx-auto">
          <ChatInput
            value={input}
            onChange={setInput}
            onSend={handleSend}
            isLoading={isLoading}
            placeholder="Nhập yêu cầu..."
          />
        </div>
      </div>
    </div>
  )
}
