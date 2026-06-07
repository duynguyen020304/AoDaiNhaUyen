import { useState } from 'react'
import { AlertTriangle, Check } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import type { AiPendingAction } from '@/types/ai'

export function ConfirmCard({ action }: { action: AiPendingAction }) {
  const confirmAction = useAdminAiStore((s) => s.confirmAction)
  const continueAfterConfirm = useAdminAiStore((s) => s.continueAfterConfirm)
  const conversationId = useAdminAiStore((s) => s.conversationId)
  const [status, setStatus] = useState<'pending' | 'confirmed' | 'rejected'>('pending')

  async function handleApprove() {
    const ok = await confirmAction(action.actionId, true)
    if (ok) {
      setStatus('confirmed')
      if (conversationId) void continueAfterConfirm(conversationId)
    }
  }

  async function handleReject() {
    const ok = await confirmAction(action.actionId, false)
    if (ok) {
      setStatus('rejected')
      if (conversationId) void continueAfterConfirm(conversationId)
    }
  }

  if (status !== 'pending') {
    return (
      <div className={`mt-2 text-xs font-medium ${status === 'confirmed' ? 'text-green-600' : 'text-red-500'}`}>
        {status === 'confirmed' ? '✅ Đã xác nhận' : '❌ Đã từ chối'}
      </div>
    )
  }

  return (
    <div className="mt-2 bg-amber-50 border border-amber-200 rounded-lg p-2">
      <div className="flex items-start gap-1 text-amber-700 text-xs mb-2">
        <AlertTriangle className="size-3 shrink-0 mt-0.5" />
        <span>{action.description}</span>
      </div>
      <div className="flex gap-2">
        <button
          onClick={handleApprove}
          className="flex items-center gap-1 px-2 py-1 bg-green-600 text-white rounded text-xs hover:bg-green-700"
        >
          <Check className="size-3" />
          Xác nhận
        </button>
        <button
          onClick={handleReject}
          className="px-2 py-1 bg-gray-200 text-gray-700 rounded text-xs hover:bg-gray-300"
        >
          Từ chối
        </button>
      </div>
    </div>
  )
}
