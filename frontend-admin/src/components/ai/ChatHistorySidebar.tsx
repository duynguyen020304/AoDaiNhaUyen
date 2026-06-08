import { useEffect } from 'react'
import { MessageSquare, Plus, Trash2, Calendar, X } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'

function formatTime(iso: string): string {
  const d = new Date(iso)
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffMins = Math.floor(diffMs / 60_000)
  if (diffMins < 1) return 'Vừa xong'
  if (diffMins < 60) return `${diffMins} phút trước`
  const diffHours = Math.floor(diffMins / 60)
  if (diffHours < 24) return `${diffHours} giờ trước`
  const diffDays = Math.floor(diffHours / 24)
  if (diffDays < 7) return `${diffDays} ngày trước`
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
}

interface ChatHistorySidebarProps {
  className?: string
  onSelect?: () => void
}

export function ChatHistorySidebar({ className = '', onSelect }: ChatHistorySidebarProps) {
  const conversations = useAdminAiStore((s) => s.conversations)
  const activeConversationId = useAdminAiStore((s) => s.activeConversationId)
  const loadConversation = useAdminAiStore((s) => s.loadConversation)
  const deleteConversation = useAdminAiStore((s) => s.deleteConversation)
  const newConversation = useAdminAiStore((s) => s.newConversation)
  const fetchConversations = useAdminAiStore((s) => s.fetchConversations)

  useEffect(() => {
    void fetchConversations()
  }, [fetchConversations])

  return (
    <div className={`flex flex-col h-full bg-white border-r border-gray-200/80 shadow-sm ${className}`}>
      {/* New chat action + Mobile close button */}
      <div className="p-4 border-b border-gray-100 flex items-center gap-2">
        <button
          onClick={() => {
            newConversation()
            onSelect?.()
          }}
          className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-semibold text-wine bg-wine/5 border border-wine/25 hover:bg-wine hover:text-white rounded-xl transition-all duration-200 shadow-sm active:scale-98 cursor-pointer"
        >
          <Plus className="size-4" />
          Cuộc trò chuyện mới
        </button>
        {onSelect && (
          <button
            onClick={onSelect}
            className="lg:hidden p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-xl cursor-pointer transition-colors shrink-0"
            aria-label="Đóng lịch sử"
          >
            <X className="size-5" />
          </button>
        )}
      </div>

      {/* History section header */}
      <div className="px-4 pt-4 pb-2 flex items-center gap-1.5 text-[10px] font-bold text-gray-400 uppercase tracking-widest">
        <Calendar className="size-3" />
        <span>Lịch sử trò chuyện</span>
      </div>

      {/* Conversation list */}
      <div className="flex-1 overflow-y-auto px-2 pb-4 space-y-1">
        {conversations.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-48 text-gray-400 px-4">
            <MessageSquare className="size-8 mb-2 text-gray-300 opacity-60" />
            <p className="text-xs text-center font-medium">Chưa có cuộc trò chuyện nào</p>
          </div>
        ) : (
          conversations.map((convo) => {
            const isActive = activeConversationId === convo.id
            return (
              <div
                key={convo.id}
                className={`group relative flex items-center gap-3 px-3.5 py-3 rounded-xl cursor-pointer transition-all duration-150 border ${
                  isActive
                    ? 'bg-wine text-white border-wine/10 shadow-sm'
                    : 'bg-white hover:bg-gray-50 border-transparent text-gray-700'
                }`}
                onClick={() => {
                  void loadConversation(convo.id)
                  onSelect?.()
                }}
              >
                <MessageSquare className={`size-4 shrink-0 ${isActive ? 'text-white/80' : 'text-gray-400'}`} />
                <div className="flex-1 min-w-0 pr-4">
                  <p className={`text-sm font-medium truncate ${isActive ? 'text-white' : 'text-gray-800'}`}>
                    {convo.title}
                  </p>
                  <p className={`text-[10px] mt-0.5 ${isActive ? 'text-white/60' : 'text-gray-450'}`}>
                    {formatTime(convo.updatedAt)}
                  </p>
                </div>
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    void deleteConversation(convo.id)
                  }}
                  className={`absolute right-2.5 p-1 rounded-lg opacity-0 group-hover:opacity-100 transition-all duration-150 shrink-0 hover:bg-black/5 ${
                    isActive ? 'text-white/80 hover:text-white hover:bg-white/10' : 'text-gray-400 hover:text-red-500'
                  }`}
                  aria-label="Xóa cuộc trò chuyện"
                >
                  <Trash2 className="size-3.5" />
                </button>
              </div>
            )
          })
        )}
      </div>
    </div>
  )
}
