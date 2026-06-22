import { useState, useRef, useLayoutEffect } from 'react'
import { Loader2 } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { MessageBubble } from './MessageBubble'
import { ChatInput } from './ChatInput'
import { EmptyChat } from './EmptyChat'

export function FullChatArea() {
  const { messages, isLoading, sendMessage, chatMode } = useAdminAiStore()
  const [input, setInput] = useState('')
  const scrollRef = useRef<HTMLDivElement>(null)

  const shouldStickToBottomRef = useRef(true)

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
    if (!input.trim() || isLoading) return
    const msg = input
    shouldStickToBottomRef.current = true
    setInput('')
    await sendMessage({ message: msg })
  }

  async function handleSuggestionClick(message: string) {
    shouldStickToBottomRef.current = true
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
