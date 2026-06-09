import { useEffect } from 'react'
import { Loader2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { LlmAuditLogDetail } from '@/types/llmLogs'

interface Props {
  open: boolean
  loading: boolean
  log: LlmAuditLogDetail | null
  onClose: () => void
}

function Field({ label, value }: { label: string; value: unknown }) {
  return (
    <div className="rounded-lg bg-zinc-50 p-3 dark:bg-zinc-950">
      <dt className="text-xs font-medium text-zinc-500">{label}</dt>
      <dd className="mt-1 break-all text-sm text-zinc-900 dark:text-zinc-100">{value ? String(value) : '—'}</dd>
    </div>
  )
}

function SafeText({ title, value }: { title: string; value: string | null }) {
  return (
    <section className="space-y-2">
      <h3 className="text-sm font-semibold text-zinc-800 dark:text-zinc-100">{title}</h3>
      <pre className="max-h-56 overflow-auto whitespace-pre-wrap rounded-lg border border-zinc-200 bg-zinc-50 p-3 text-xs text-zinc-700 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-200">
        {value || '—'}
      </pre>
    </section>
  )
}

export function LlmLogDetailDrawer({ open, loading, log, onClose }: Props) {
  useEffect(() => {
    if (!open) return
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, onClose])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label="Chi tiết nhật ký LLM">
      <button className="absolute inset-0 bg-black/40" onClick={onClose} aria-label="Đóng chi tiết" />
      <aside className="absolute inset-y-0 right-0 flex w-full max-w-3xl flex-col bg-white shadow-xl dark:bg-zinc-900">
        <header className="flex items-center justify-between border-b border-zinc-200 p-4 dark:border-zinc-800">
          <div>
            <h2 className="text-lg font-semibold text-zinc-900 dark:text-zinc-50">Chi tiết nhật ký LLM</h2>
            <p className="text-xs text-zinc-500">Dữ liệu nhạy cảm đã được ẩn trước khi lưu.</p>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} aria-label="Đóng">
            <X className="size-5" />
          </Button>
        </header>

        <div className="flex-1 overflow-y-auto p-4">
          {loading ? (
            <div className="flex h-64 items-center justify-center">
              <Loader2 className="size-6 animate-spin text-primary" />
            </div>
          ) : log ? (
            <div className="space-y-5">
              <dl className="grid gap-3 md:grid-cols-2">
                <Field label="Request ID" value={log.requestId} />
                <Field label="Correlation ID" value={log.correlationId} />
                <Field label="Nguồn" value={log.source} />
                <Field label="Trạng thái" value={log.status} />
                <Field label="Provider / Model" value={`${log.provider}${log.model ? ` / ${log.model}` : ''}`} />
                <Field label="Operation / Tool" value={`${log.operation}${log.toolName ? ` / ${log.toolName}` : ''}`} />
                <Field label="Latency" value={log.latencyMs ? `${log.latencyMs}ms` : null} />
                <Field label="Token" value={log.totalTokens} />
                <Field label="Actor" value={log.actorUserId} />
                <Field label="Rủi ro" value={log.riskLevel} />
                <Field label="Thread" value={log.threadId} />
                <Field label="Conversation" value={log.conversationId} />
              </dl>

              <SafeText title="Prompt đã redacted" value={log.promptPreviewRedacted} />
              <SafeText title="Output đã redacted" value={log.completionPreviewRedacted} />
              <SafeText title="Input metadata" value={log.inputMetadataJson} />
              <SafeText title="Output metadata" value={log.outputMetadataJson} />
              <SafeText title="Safety flags" value={log.safetyFlagsJson} />
            </div>
          ) : (
            <p className="text-sm text-zinc-500">Không có dữ liệu chi tiết.</p>
          )}
        </div>
      </aside>
    </div>
  )
}
