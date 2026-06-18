import { useEffect } from 'react'
import { Loader2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { HermesReportDetail } from '@/types/hermes'

interface Props {
  open: boolean
  loading: boolean
  report: HermesReportDetail | null
  onClose: () => void
}

function Field({ label, value }: { label: string; value: unknown }) {
  return (
    <div className="rounded-lg bg-zinc-50 p-3">
      <dt className="text-xs font-medium text-zinc-500">{label}</dt>
      <dd className="mt-1 break-all text-sm text-zinc-900">{value ? String(value) : '—'}</dd>
    </div>
  )
}

function SafeText({ title, value }: { title: string; value: string | null }) {
  return (
    <section className="space-y-2">
      <h3 className="text-sm font-semibold text-zinc-800">{title}</h3>
      <pre className="max-h-72 overflow-auto whitespace-pre-wrap rounded-lg border border-zinc-200 bg-zinc-50 p-3 text-xs text-zinc-700">
        {value || '—'}
      </pre>
    </section>
  )
}

function formatJson(value: string | null) {
  if (!value) return null
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

export function HermesReportDetailDrawer({ open, loading, report, onClose }: Props) {
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
    <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label="Chi tiết báo cáo Hermes">
      <button className="absolute inset-0 bg-black/40" onClick={onClose} aria-label="Đóng chi tiết" />
      <aside className="absolute inset-y-0 right-0 flex w-full max-w-3xl flex-col bg-white shadow-xl">
        <header className="flex items-center justify-between border-b border-zinc-200 p-4">
          <div>
            <h2 className="text-lg font-semibold text-zinc-900">Chi tiết báo cáo Hermes</h2>
            <p className="text-xs text-zinc-500">Nội dung render dạng text an toàn, không HTML.</p>
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
          ) : report ? (
            <div className="space-y-5">
              <dl className="grid gap-3 md:grid-cols-2">
                <Field label="ID" value={report.id} />
                <Field label="Correlation ID" value={report.correlationId} />
                <Field label="Loại" value={report.reportType} />
                <Field label="Mức độ" value={report.severity} />
                <Field label="Trạng thái" value={report.status} />
                <Field label="Nguồn" value={report.source} />
                <Field label="Run ID" value={report.runId} />
                <Field label="Thời gian" value={new Date(report.createdAt).toLocaleString('vi-VN')} />
              </dl>

              <SafeText title={report.title} value={report.summary} />
              <SafeText title="Payload JSON" value={formatJson(report.payloadJson)} />
            </div>
          ) : (
            <p className="text-sm text-zinc-500">Không có dữ liệu chi tiết.</p>
          )}
        </div>
      </aside>
    </div>
  )
}
