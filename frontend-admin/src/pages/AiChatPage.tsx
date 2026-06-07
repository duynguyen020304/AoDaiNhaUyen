import { useAdminAiStore } from '@/stores/adminAiStore'
import { FullChatArea } from '@/components/ai/FullChatArea'

export function AiChatPage() {
  const clearConversation = useAdminAiStore((s) => s.clearConversation)

  return (
    <div className="h-full">
      <FullChatArea onClear={clearConversation} />
    </div>
  )
}
