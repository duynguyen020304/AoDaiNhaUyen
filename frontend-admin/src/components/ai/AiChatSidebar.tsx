import { useState, useRef, useLayoutEffect } from 'react'
import { Bot, X, Send, Loader2 } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { MessageBubble } from './MessageBubble'
import { Button } from '@/components/ui/button'

export function AiChatSidebar() {
  const {
    isOpen,
    messages,
    isLoading,
    toggle,
    sendMessage,
  } = useAdminAiStore()

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

  if (!isOpen) return null

  async function handleSend() {
    if (!input.trim() || isLoading) return
    const msg = input
    shouldStickToBottomRef.current = true
    setInput('')
    await sendMessage({ message: msg })
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  return (
    <aside className="fixed right-0 top-0 h-dvh w-[min(820px,100vw)] bg-white border-l border-gray-200 flex flex-col z-50 shadow-xl">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 bg-wine text-white">
        <div className="flex items-center gap-2">
          <Bot className="size-5" />
          <div>
            <span className="font-semibold">Trợ lý AI Admin</span>
            <p className="text-xs text-white/70">Generic Chat</p>
          </div>
        </div>
        <button
          onClick={toggle}
          className="p-1 rounded hover:bg-white/10 transition-colors"
          aria-label="Đóng"
        >
          <X className="size-5" />
        </button>
      </div>

      {/* Messages */}
      <div ref={scrollRef} onScroll={handleScroll} className="flex-1 overflow-y-auto overflow-x-hidden p-6 space-y-4 [overflow-anchor:none]">
        {messages.length === 0 && (
          <div className="text-center text-gray-400 mt-8">
            <Bot className="size-12 mx-auto mb-3 opacity-30" />
            <p className="text-sm">Hỏi tôi về doanh thu, sản phẩm, đơn hàng hoặc người dùng.</p>
            <p className="text-xs mt-1">Ví dụ: &quot;Xem báo cáo doanh thu tuần này&quot;</p>
          </div>
        )}
        {messages.map((msg) => (
          <MessageBubble key={msg.id} message={msg} />
        ))}
        {isLoading && messages.length > 0 && (
          <div className="flex justify-center py-2">
            <Loader2 className="size-4 animate-spin text-gray-400" />
          </div>
        )}
      </div>

      {/* Input */}
      <div className="border-t border-gray-200 p-3">
        <div className="flex gap-2">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Nhập yêu cầu..."
            disabled={isLoading}
            className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-wine/30 disabled:opacity-50"
          />
          <Button
            size="sm"
            onClick={handleSend}
            disabled={isLoading || !input.trim()}
            className="bg-wine hover:bg-wine/90 text-white shrink-0"
          >
            <Send className="size-4" />
          </Button>
        </div>
      </div>
    </aside>
  )
}
