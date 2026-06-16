import { useState, useRef, useEffect } from 'react'
import { Loader2 } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { MessageBubble } from './MessageBubble'
import { ChatInput } from './ChatInput'
import { EmptyChat } from './EmptyChat'
import { ChatModeSelector } from './ChatModeSelector'

export function FullChatArea() {
  const { messages, isLoading, sendMessage, chatMode } = useAdminAiStore()
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

  async function handleSuggestionClick(message: string) {
    setInput('')
    await sendMessage({ message })
  }

  const hasMessages = messages.length > 0

  return (
    <div className="flex flex-col h-full relative bg-white">
      <div className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-3">
        <div>
          <h2 className="text-sm font-semibold text-gray-900">Trợ lý AI Admin</h2>
          <p className="text-xs text-gray-500">
            {chatMode === 'hermes' ? 'Hermes Agent tự động quản trị, có heartbeat.' : 'Chat AI admin mặc định.'}
          </p>
        </div>
        <ChatModeSelector />
      </div>

      {/* Messages or Empty State */}
      {hasMessages ? (
        <div ref={scrollRef} className="flex-1 overflow-y-auto px-6 py-6 scroll-smooth">
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
