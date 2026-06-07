import { useState } from 'react'
import { PanelLeft } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { FullChatArea } from '@/components/ai/FullChatArea'
import { ChatHistorySidebar } from '@/components/ai/ChatHistorySidebar'

export function AiChatPage() {
  const clearConversation = useAdminAiStore((s) => s.clearConversation)
  const [sidebarOpen, setSidebarOpen] = useState(true)

  return (
    <div className="flex h-[calc(100dvh-2.5rem)] lg:h-dvh -m-4 lg:-m-6 overflow-hidden">
      {/* Sidebar toggle — mobile */}
      <button
        onClick={() => setSidebarOpen(!sidebarOpen)}
        className="lg:hidden fixed bottom-20 left-4 z-30 p-2 bg-wine text-white rounded-full shadow-lg"
        aria-label="Lịch sử chat"
      >
        <PanelLeft className="size-5" />
      </button>

      {/* Chat history sidebar */}
      <div
        className={`${
          sidebarOpen ? 'w-72' : 'w-0'
        } transition-all duration-200 overflow-hidden shrink-0 max-lg:absolute max-lg:z-20 max-lg:h-[calc(100dvh-2.5rem)] lg:h-full`}
      >
        <ChatHistorySidebar className="w-72" />
      </div>

      {/* Chat area */}
      <div className="flex-1 min-w-0 h-full">
        <FullChatArea onClear={clearConversation} />
      </div>
    </div>
  )
}
