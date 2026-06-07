import { useState, useEffect } from 'react'
import { PanelLeft } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { FullChatArea } from '@/components/ai/FullChatArea'
import { ChatHistorySidebar } from '@/components/ai/ChatHistorySidebar'

export function AiChatPage() {
  const clearConversation = useAdminAiStore((s) => s.clearConversation)
  const [sidebarOpen, setSidebarOpen] = useState(window.innerWidth > 1024)

  useEffect(() => {
    function handleResize() {
      if (window.innerWidth > 1024) {
        setSidebarOpen(true)
      }
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

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

        {/* Chat history sidebar */}
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

        {/* Chat area */}
        <div className="flex-1 min-w-0 h-full">
          <FullChatArea onClear={clearConversation} />
        </div>
      </div>
    </div>
  )
}
