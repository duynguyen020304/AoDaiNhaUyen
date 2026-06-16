import { Bot, RadioTower } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import type { AdminChatMode } from '@/types/ai'

const MODE_OPTIONS: Array<{ value: AdminChatMode; label: string }> = [
  { value: 'generic', label: 'Generic Chat' },
  { value: 'hermes', label: 'Hermes Agent' },
]

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
  const { chatMode, setChatMode, hermesStatus } = useAdminAiStore()
  const isHermes = chatMode === 'hermes'

  return (
    <div className="flex items-center gap-2">
      {isHermes ? <RadioTower className="size-4 text-wine" /> : <Bot className="size-4 text-wine" />}
      <select
        value={chatMode}
        onChange={(event) => setChatMode(event.target.value as AdminChatMode)}
        className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 shadow-sm focus:border-wine focus:outline-none focus:ring-2 focus:ring-wine/20"
        aria-label="Chọn chế độ chat AI"
      >
        {MODE_OPTIONS.map((option) => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
      {isHermes && (
        <span className={`rounded-full border px-2 py-1 text-xs font-medium ${statusClass(hermesStatus?.status)}`}>
          {statusLabel(hermesStatus?.status)}
        </span>
      )}
    </div>
  )
}
