import { useNavigate } from 'react-router-dom'
import { Activity, FileText, MessageSquare } from 'lucide-react'
import { HermesEventsPanel } from '@/components/hermes/HermesEventsPanel'

export function HermesEventsPage() {
  const navigate = useNavigate()

  return (
    <div className="flex h-dvh -mx-4 -mb-4 -mt-14 flex-col overflow-hidden bg-white lg:-m-6">
      <div className="flex shrink-0 gap-2 border-b border-gray-200 bg-white px-4 py-2">
        <button
          type="button"
          onClick={() => navigate('/admin/ai-chat')}
          className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100"
        >
          <MessageSquare className="size-4" />
          Chat Hermes
        </button>
        <button
          type="button"
          onClick={() => navigate('/admin/hermes-reports')}
          className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100"
        >
          <FileText className="size-4" />
          Báo cáo Hermes
        </button>
        <button
          type="button"
          className="inline-flex items-center gap-2 rounded-lg bg-wine px-3 py-2 text-sm font-medium text-white"
          aria-current="page"
        >
          <Activity className="size-4" />
          Event Hermes
        </button>
      </div>
      <div className="min-h-0 flex-1">
        <HermesEventsPanel />
      </div>
    </div>
  )
}
