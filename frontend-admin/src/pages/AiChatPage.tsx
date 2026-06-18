import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { FileText, MessageSquare, PanelLeft, Radio } from 'lucide-react'
import { FullChatArea } from '@/components/ai/FullChatArea'
import { ChatHistorySidebar } from '@/components/ai/ChatHistorySidebar'
import { useAdminAiStore } from '@/stores/adminAiStore'

export function AiChatPage() {
  const { chatId } = useParams<{ chatId: string }>()
  const navigate = useNavigate()
  const [sidebarOpen, setSidebarOpen] = useState(window.innerWidth > 1024)
  const activeConversationId = useAdminAiStore((s) => s.activeConversationId)
  const messages = useAdminAiStore((s) => s.messages)
  const loadConversation = useAdminAiStore((s) => s.loadConversation)

  useEffect(() => {
    function handleResize() {
      if (window.innerWidth > 1024) {
        setSidebarOpen(true)
      }
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  useEffect(() => {
    if (chatId && chatId !== activeConversationId) {
      void loadConversation(chatId)
    }
  }, [activeConversationId, chatId, loadConversation])

  useEffect(() => {
    if (!chatId && activeConversationId && messages.length > 0) {
      navigate(`/admin/hermes/${activeConversationId}`, { replace: true })
    }
  }, [activeConversationId, chatId, messages.length, navigate])

  return (
    <div className="flex flex-col lg:flex-row h-dvh -mx-4 -mb-4 -mt-14 lg:-m-6 overflow-hidden relative bg-white">
      {/* Slim Mobile Header Bar (unifies hamburger menu and history toggle) */}
      <div className="lg:hidden w-full h-14 flex items-center justify-between bg-white border-b border-gray-200/80 px-4 pl-14 shrink-0">
        <span className="text-sm font-bold text-gray-800">AI Trợ lý</span>
        {!sidebarOpen && (
          <button
            onClick={() => setSidebarOpen(true)}
            className="p-1.5 hover:bg-gray-100 rounded-lg text-gray-500 cursor-pointer active:scale-95 transition-transform"
            aria-label="Mở lịch sử chat"
          >
            <PanelLeft className="size-5" />
          </button>
        )}
      </div>

      {/* Main Workspace */}
      <div className="flex flex-1 min-h-0 overflow-hidden relative">
        {/* Mobile Backdrop Overlay */}
        {sidebarOpen && (
          <div
            onClick={() => setSidebarOpen(false)}
            className="lg:hidden fixed inset-0 z-15 bg-black/30 backdrop-blur-xs transition-opacity duration-200"
          />
        )}

        <div
          className={`${
            sidebarOpen ? 'w-72' : 'w-0'
          } transition-all duration-200 overflow-hidden shrink-0 max-lg:fixed max-lg:left-0 max-lg:top-0 max-lg:z-20 max-lg:h-dvh lg:h-full`}
        >
          <ChatHistorySidebar className="w-72" onSelect={() => {
            if (window.innerWidth <= 1024) {
              setSidebarOpen(false)
            }
          }} />
        </div>

        <div className="flex-1 min-w-0 h-full flex flex-col">
          <div className="flex shrink-0 gap-2 border-b border-gray-200 bg-white px-4 py-2">
            <button
              type="button"
              onClick={() => navigate(activeConversationId ? `/admin/hermes/${activeConversationId}` : '/admin/hermes')}
              className="inline-flex items-center gap-2 rounded-lg bg-wine px-3 py-2 text-sm font-medium text-white"
            >
              <MessageSquare className="size-4" />
              Chat Hermes
            </button>
            <button
              type="button"
              onClick={() => navigate('/admin/hermes?tab=reports')}
              className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100"
            >
              <FileText className="size-4" />
              Báo cáo Hermes
            </button>
            <button
              type="button"
              onClick={() => navigate('/admin/hermes?tab=live')}
              className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100"
            >
              <Radio className="size-4" />
              Live Monitor
            </button>
          </div>
          <div className="min-h-0 flex-1">
            <FullChatArea />
          </div>
        </div>
      </div>
    </div>
  )
}
