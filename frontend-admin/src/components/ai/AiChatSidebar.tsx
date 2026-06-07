import { useState, useRef, useEffect } from 'react'
import { Bot, X, Send, Loader2, AlertTriangle, Check, ChevronDown, ChevronUp } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import type { AiMessage, AiToolCall, AiPendingAction } from '@/types/ai'
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

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [messages, isLoading])

  if (!isOpen) return null

  async function handleSend() {
    if (!input.trim() || isLoading) return
    const msg = input
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
    <aside className="fixed right-0 top-0 h-dvh w-96 bg-white border-l border-gray-200 flex flex-col z-50 shadow-xl">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 bg-wine text-white">
        <div className="flex items-center gap-2">
          <Bot className="size-5" />
          <span className="font-semibold">Trợ lý AI Admin</span>
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
      <div ref={scrollRef} className="flex-1 overflow-y-auto p-4 space-y-4">
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

function MessageBubble({ message }: { message: AiMessage }) {
  const isUser = message.role === 'user'

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[85%] rounded-xl px-3 py-2 text-sm whitespace-pre-wrap ${
          isUser
            ? 'bg-wine text-white rounded-tr-sm'
            : 'bg-gray-100 text-gray-800 rounded-tl-sm'
        }`}
      >
        {message.content}
        {message.toolCalls && message.toolCalls.length > 0 && (
          <div className="mt-2 space-y-1">
            {message.toolCalls.map((tc, i) => (
              <ToolCallCard key={i} toolCall={tc} />
            ))}
          </div>
        )}
        {message.pendingAction && (
          <ConfirmCard
            action={message.pendingAction}
            messageId={message.id}
          />
        )}
      </div>
    </div>
  )
}

function ToolCallCard({ toolCall }: { toolCall: AiToolCall }) {
  const [expanded, setExpanded] = useState(false)
  const label = toolLabel(toolCall.toolName)

  return (
    <div className="bg-white/60 rounded-lg p-2 text-xs">
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex items-center gap-1 text-wine font-medium w-full text-left"
      >
        {expanded ? <ChevronUp className="size-3" /> : <ChevronDown className="size-3" />}
        {label}
      </button>
      {expanded && (
        <div className="mt-1 text-gray-500 truncate">
          {truncate(toolCall.input, 100)}
        </div>
      )}
    </div>
  )
}

function ConfirmCard({ action }: { action: AiPendingAction; messageId: string }) {
  const confirmAction = useAdminAiStore((s) => s.confirmAction)
  const [status, setStatus] = useState<'pending' | 'confirmed' | 'rejected'>('pending')

  async function handleApprove() {
    const ok = await confirmAction(action.actionId, true)
    if (ok) setStatus('confirmed')
  }

  async function handleReject() {
    const ok = await confirmAction(action.actionId, false)
    if (ok) setStatus('rejected')
  }

  if (status !== 'pending') {
    return (
      <div className={`mt-2 text-xs font-medium ${status === 'confirmed' ? 'text-green-600' : 'text-red-500'}`}>
        {status === 'confirmed' ? '✅ Đã xác nhận' : '❌ Đã từ chối'}
      </div>
    )
  }

  return (
    <div className="mt-2 bg-amber-50 border border-amber-200 rounded-lg p-2">
      <div className="flex items-start gap-1 text-amber-700 text-xs mb-2">
        <AlertTriangle className="size-3 shrink-0 mt-0.5" />
        <span>{action.description}</span>
      </div>
      <div className="flex gap-2">
        <button
          onClick={handleApprove}
          className="flex items-center gap-1 px-2 py-1 bg-green-600 text-white rounded text-xs hover:bg-green-700"
        >
          <Check className="size-3" />
          Xác nhận
        </button>
        <button
          onClick={handleReject}
          className="px-2 py-1 bg-gray-200 text-gray-700 rounded text-xs hover:bg-gray-300"
        >
          Từ chối
        </button>
      </div>
    </div>
  )
}

function toolLabel(name: string): string {
  const labels: Record<string, string> = {
    get_dashboard_summary: '📊 Đọc tổng quan',
    get_revenue: '💰 Đọc doanh thu',
    get_orders_by_status: '📋 Đọc trạng thái đơn hàng',
    get_recent_orders: '🛒 Đọc đơn hàng gần đây',
    get_top_products: '⭐ Đọc top sản phẩm',
    list_products: '📦 Liệt kê sản phẩm',
    get_product: '🔍 Xem sản phẩm',
    create_product: '✨ Tạo sản phẩm',
    update_product: '✏️ Cập nhật sản phẩm',
    delete_product: '🗑️ Xóa sản phẩm',
    toggle_product_status: '🔄 Đổi trạng thái sản phẩm',
    list_categories: '📁 Liệt kê danh mục',
    create_category: '📁 Tạo danh mục',
    update_category: '✏️ Cập nhật danh mục',
    delete_category: '🗑️ Xóa danh mục',
    list_users: '👥 Liệt kê người dùng',
    get_user: '👤 Xem người dùng',
    update_user_status: '🔄 Đổi trạng thái người dùng',
    update_user_role: '🔑 Đổi vai trò người dùng',
  }
  return labels[name] || `🔧 ${name}`
}

function truncate(text: string, len: number): string {
  return text.length > len ? text.slice(0, len) + '...' : text
}
