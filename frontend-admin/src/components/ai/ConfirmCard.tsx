import { useState } from 'react'
import { AlertTriangle, Check, X } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import type { AiPendingAction } from '@/types/ai'

export function ConfirmCard({
  action,
  onStatusChange,
}: {
  action: AiPendingAction
  onStatusChange?: (status: 'confirmed' | 'rejected') => void
}) {
  const confirmAction = useAdminAiStore((s) => s.confirmAction)
  const continueAfterConfirm = useAdminAiStore((s) => s.continueAfterConfirm)
  const conversationId = useAdminAiStore((s) => s.conversationId)
  const [status, setStatus] = useState<'pending' | 'confirmed' | 'rejected'>('pending')

  async function handleApprove() {
    const ok = await confirmAction(action.actionId, true)
    if (ok) {
      setStatus('confirmed')
      onStatusChange?.('confirmed')
      if (conversationId) void continueAfterConfirm(conversationId)
    }
  }

  async function handleReject() {
    const ok = await confirmAction(action.actionId, false)
    if (ok) {
      setStatus('rejected')
      onStatusChange?.('rejected')
      if (conversationId) void continueAfterConfirm(conversationId)
    }
  }

  if (status !== 'pending') {
    return null
  }

  return (
    <div className="mt-3 bg-amber-50/60 border border-amber-200/60 rounded-xl p-3.5 shadow-sm">
      <div className="flex items-start gap-2 text-amber-850 text-xs mb-3 leading-relaxed">
        <AlertTriangle className="size-4 shrink-0 text-amber-600 mt-0.5" />
        <span className="font-medium">{action.description}</span>
      </div>
      <div className="flex gap-2 justify-end">
        <button
          onClick={handleReject}
          className="px-3 py-1.5 bg-white border border-gray-200 text-gray-700 rounded-lg text-xs font-medium hover:bg-gray-50 transition-colors shadow-sm cursor-pointer"
        >
          Từ chối
        </button>
        <button
          onClick={handleApprove}
          className="flex items-center gap-1 px-3 py-1.5 bg-green-600 text-white rounded-lg text-xs font-medium hover:bg-green-700 transition-colors shadow-sm cursor-pointer"
        >
          <Check className="size-3.5" />
          Xác nhận
        </button>
      </div>
    </div>
  )
}
