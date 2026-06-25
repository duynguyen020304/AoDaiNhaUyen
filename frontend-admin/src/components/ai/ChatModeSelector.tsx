import { Bot } from 'lucide-react'

export function ChatModeSelector() {
  return (
    <div className="flex items-center gap-2" aria-label="Chế độ chat AI">
      <Bot className="size-4 text-wine" />
      <span className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 shadow-sm">
        Generic Chat
      </span>
    </div>
  )
}
