import { useState } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'
import type { AiMessage, AiToolCall } from '@/types/ai'
import { ConfirmCard } from './ConfirmCard'

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

export function MessageBubble({ message }: { message: AiMessage }) {
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
          />
        )}
      </div>
    </div>
  )
}
