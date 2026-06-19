import { useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { FileText, MessageSquare, PanelLeft, Radio } from 'lucide-react'
import { FullChatArea } from '@/components/ai/FullChatArea'
import { ChatHistorySidebar } from '@/components/ai/ChatHistorySidebar'
import { HermesReportsPanel } from '@/components/hermes/HermesReportsPanel'
import { HermesLiveMonitorPanel } from '@/pages/HermesLiveMonitorPage'
import { useAdminAiStore } from '@/stores/adminAiStore'

type HermesTab = 'chat' | 'reports' | 'live'

const tabButtonBase = 'inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-wine focus-visible:ring-offset-2'

export function HermesPage() {
  const { chatId } = useParams<{ chatId: string }>()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [sidebarOpen, setSidebarOpen] = useState(window.innerWidth > 1024)
  const activeConversationId = useAdminAiStore((s) => s.activeConversationId)
  const messages = useAdminAiStore((s) => s.messages)
  const loadConversation = useAdminAiStore((s) => s.loadConversation)

  const tabParam = searchParams.get('tab')
  const activeTab: HermesTab = useMemo(() => {
    if (chatId) return 'chat'
    if (tabParam === 'reports' || tabParam === 'live') return tabParam
    return 'chat'
  }, [chatId, tabParam])

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
    if (!chatId && activeTab === 'chat' && activeConversationId && messages.length > 0) {
      navigate(`/admin/hermes/${activeConversationId}`, { replace: true })
    }
  }, [activeConversationId, activeTab, chatId, messages.length, navigate])

  function switchTab(tab: HermesTab) {
    if (tab === 'chat') {
      navigate(activeConversationId ? `/admin/hermes/${activeConversationId}` : '/admin/hermes')
      return
    }
    navigate(`/admin/hermes?tab=${tab}`)
  }

  return (
    <div className="flex h-dvh -mx-4 -mb-4 -mt-14 flex-col overflow-hidden bg-white lg:-m-6">
      <div className="lg:hidden flex h-14 shrink-0 items-center justify-between border-b border-gray-200/80 bg-white px-4 pl-14">
        <span className="text-sm font-bold text-gray-800">Hermes</span>
        {activeTab === 'chat' && !sidebarOpen && (
          <button
            type="button"
            onClick={() => setSidebarOpen(true)}
            className="rounded-lg p-1.5 text-gray-500 transition-transform hover:bg-gray-100 active:scale-95"
            aria-label="Mở lịch sử chat"
          >
            <PanelLeft className="size-5" />
          </button>
        )}
      </div>

      <div className="flex shrink-0 gap-2 overflow-x-auto border-b border-gray-200 bg-white px-4 py-2" role="tablist" aria-label="Khu vực Hermes">
        <HermesTabButton active={activeTab === 'chat'} onClick={() => switchTab('chat')} icon={<MessageSquare className="size-4" />}>
          Chat Hermes
        </HermesTabButton>
        <HermesTabButton active={activeTab === 'reports'} onClick={() => switchTab('reports')} icon={<FileText className="size-4" />}>
          Báo cáo Hermes
        </HermesTabButton>
        <HermesTabButton active={activeTab === 'live'} onClick={() => switchTab('live')} icon={<Radio className="size-4" />}>
          Live Monitor
        </HermesTabButton>
      </div>

      <div className="min-h-0 flex-1 overflow-hidden">
        {activeTab === 'chat' && (
          <HermesChatView sidebarOpen={sidebarOpen} onSidebarOpenChange={setSidebarOpen} />
        )}
        {activeTab === 'reports' && <HermesReportsPanel />}
        {activeTab === 'live' && <HermesLiveMonitorPanel />}
      </div>
    </div>
  )
}

function HermesTabButton({ active, onClick, icon, children }: { active: boolean; onClick: () => void; icon: ReactNode; children: ReactNode }) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={`${tabButtonBase} ${active ? 'bg-wine text-white' : 'text-gray-600 hover:bg-gray-100'}`}
    >
      {icon}
      {children}
    </button>
  )
}

function HermesChatView({ sidebarOpen, onSidebarOpenChange }: { sidebarOpen: boolean; onSidebarOpenChange: (open: boolean) => void }) {
  return (
    <div className="flex h-full min-h-0 overflow-hidden">
      {sidebarOpen && (
        <button
          type="button"
          aria-label="Đóng lịch sử chat"
          onClick={() => onSidebarOpenChange(false)}
          className="fixed inset-0 z-15 bg-black/30 backdrop-blur-xs transition-opacity duration-200 lg:hidden"
        />
      )}

      <div
        className={`${
          sidebarOpen ? 'w-72' : 'w-0'
        } max-lg:top-0 max-lg:left-0 max-lg:z-20 max-lg:h-dvh transition-all duration-200 overflow-hidden shrink-0 max-lg:fixed lg:h-full`}
      >
        <ChatHistorySidebar
          className="w-72"
          onSelect={() => {
            if (window.innerWidth <= 1024) {
              onSidebarOpenChange(false)
            }
          }}
        />
      </div>

      <div className="min-w-0 flex-1">
        <FullChatArea />
      </div>
    </div>
  )
}
