import { Bot, RadioTower } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'

function statusLabel(status?: string) {
  if (status === 'running') return 'Running'
  if (status === 'stale') return 'Stale heartbeat'
  if (status === 'offline') return 'Offline'
  return status || 'Unknown'
}

function statusClass(status?: string) {
  if (status === 'running') return 'bg-emerald-50 text-emerald-700 border-emerald-200'
  if (status === 'stale') return 'bg-amber-50 text-amber-700 border-amber-200'
  return 'bg-gray-50 text-gray-600 border-gray-200'
}

export function ChatModeSelector() {
  const { chatMode, hermesStatus } = useAdminAiStore()
  const isHermes = chatMode === 'hermes'

  return (
    <div className="flex items-center gap-2" aria-label="Chế độ chat AI">
      {isHermes ? <RadioTower className="size-4 text-wine" /> : <Bot className="size-4 text-wine" />}
      <span className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 shadow-sm">
        {isHermes ? 'Hermes Agent' : 'Generic Chat'}
      </span>
      {isHermes && (
        <span className={`rounded-full border px-2 py-1 text-xs font-medium ${statusClass(hermesStatus?.status)}`}>
          {statusLabel(hermesStatus?.status)}
        </span>
      )}
    </div>
  )
}
