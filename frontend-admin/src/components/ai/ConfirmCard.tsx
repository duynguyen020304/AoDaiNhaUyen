import { useState } from 'react'
import { AlertTriangle, Check } from 'lucide-react'
import { useAdminAiStore } from '@/stores/adminAiStore'
import { useFeedback } from '@/components/ui/feedbackContext'
import type { AiPendingAction } from '@/types/ai'

function formatJsonPreview(raw: string) {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    return raw
  }
}

function splitConfirmationDescription(description: string) {
  const marker = '\nTham số:\n'
  const index = description.indexOf(marker)
  if (index < 0) return { summary: description, payload: null as string | null }

  return {
    summary: description.slice(0, index),
    payload: formatJsonPreview(description.slice(index + marker.length)),
  }
}

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
  const { toast } = useFeedback()
  const [status, setStatus] = useState<'pending' | 'confirmed' | 'rejected'>('pending')
  const { summary, payload } = splitConfirmationDescription(action.description)

  async function handleApprove() {
    const ok = await confirmAction(action.actionId, true)
    if (ok) {
      setStatus('confirmed')
      onStatusChange?.('confirmed')
      if (conversationId) void continueAfterConfirm(conversationId)
    } else {
      toast('Không thể xác nhận hành động. Vui lòng thử lại.', 'error')
    }
  }

  async function handleReject() {
    const ok = await confirmAction(action.actionId, false)
    if (ok) {
      setStatus('rejected')
      onStatusChange?.('rejected')
      if (conversationId) void continueAfterConfirm(conversationId)
    } else {
      toast('Không thể từ chối hành động. Vui lòng thử lại.', 'error')
    }
  }

  if (status !== 'pending') {
    return null
  }

  return (
    <div className="mt-3 bg-amber-50/60 border border-amber-200/60 rounded-xl p-3.5 shadow-sm">
      <div className="mb-3 flex items-start gap-2 text-xs leading-relaxed text-amber-950">
        <AlertTriangle className="mt-0.5 size-4 shrink-0 text-amber-600" />
        <div className="min-w-0 flex-1 space-y-2">
          <div className="font-medium whitespace-pre-wrap break-words">{summary}</div>
          {payload && (
            <pre className="overflow-x-auto whitespace-pre-wrap rounded-lg border border-amber-200 bg-white/80 p-2 text-[11px] font-mono text-amber-950 max-h-56 leading-relaxed">
              {payload}
            </pre>
          )}
        </div>
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
