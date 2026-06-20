import { useEffect, useMemo } from 'react'
import { Loader2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { HermesRawAction, HermesReportDetail } from '@/types/hermes'

interface Props {
  open: boolean
  loading: boolean
  report: HermesReportDetail | null
  onClose: () => void
}

interface ParsedActionsResult {
  actions: HermesRawAction[]
  error: string | null
}

const validMethods = new Set(['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD'])
const validRisks = new Set(['low', 'medium', 'high'])

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

function formatJson(value: unknown) {
  if (value === null || value === undefined) return null
  if (typeof value === 'string') {
    try {
      return JSON.stringify(JSON.parse(value), null, 2)
    } catch {
      return value
    }
  }

  try {
    return JSON.stringify(value, null, 2)
  } catch {
    return String(value)
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function normalizeAction(value: unknown, index: number): HermesRawAction | null {
  if (!isRecord(value)) return null

  const method = typeof value.method === 'string' ? value.method.toUpperCase() : ''
  const path = typeof value.path === 'string' ? value.path.trim() : ''
  const title = typeof value.title === 'string' ? value.title.trim() : ''
  const risk = typeof value.risk === 'string' ? value.risk.toLowerCase() : ''

  if (!title || !validMethods.has(method) || !path.startsWith('/api/admin/') || !validRisks.has(risk)) return null

  return {
    id: typeof value.id === 'string' && value.id.trim() ? value.id.trim() : `local-${index + 1}`,
    actionType: typeof value.actionType === 'string' ? value.actionType : undefined,
    title,
    reason: typeof value.reason === 'string' ? value.reason : undefined,
    risk,
    method,
    path,
    body: value.body,
    executionMode: typeof value.executionMode === 'string' ? value.executionMode : undefined,
  }
}

function parseHermesActions(summary: string | null): ParsedActionsResult {
  if (!summary) return { actions: [], error: null }

  const jsonBlocks = [...summary.matchAll(/```json\s*([\s\S]*?)```/gi)]
  for (const block of jsonBlocks) {
    try {
      const parsed = JSON.parse(block[1].trim())
      if (!isRecord(parsed) || !Array.isArray(parsed.actions)) continue

      const actions = parsed.actions
        .map((action, index) => normalizeAction(action, index))
        .filter((action): action is HermesRawAction => action !== null)

      return actions.length > 0
        ? { actions, error: null }
        : { actions: [], error: 'JSON actions thiếu method/path/title/risk hợp lệ.' }
    } catch {
      return { actions: [], error: 'Không đọc được JSON hành động Hermes.' }
    }
  }

  return { actions: [], error: null }
}

function riskClassName(risk: string) {
  if (risk === 'high') return 'border-red-200 bg-red-50 text-red-700'
  if (risk === 'medium') return 'border-amber-200 bg-amber-50 text-amber-700'
  return 'border-emerald-200 bg-emerald-50 text-emerald-700'
}

function HermesActionCard({ action }: { action: HermesRawAction }) {
  const bodyPreview = formatJson(action.body) ?? '{}'

  return (
    <article className="space-y-3 rounded-xl border border-zinc-200 bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <div className="flex flex-wrap items-center gap-2">
            <h4 className="text-sm font-semibold text-zinc-900">{action.title}</h4>
            <span className={`rounded-full border px-2 py-0.5 text-xs font-semibold ${riskClassName(action.risk)}`}>
              {action.risk}
            </span>
          </div>
          {action.reason ? <p className="text-xs text-zinc-600">{action.reason}</p> : null}
          {action.actionType ? <p className="text-xs text-zinc-500">Loại: {action.actionType}</p> : null}
        </div>
        <span className="rounded-lg border border-blue-200 bg-blue-50 px-3 py-2 text-xs font-semibold text-blue-700">
          Hermes runner tự thực thi bằng X-Hermes-Admin-Key
        </span>
      </div>

      <div className="rounded-lg border border-zinc-200 bg-zinc-950 p-3 font-mono text-xs text-zinc-100">
        <span className="text-emerald-300">{action.method}</span> {action.path}
      </div>

      <details className="rounded-lg border border-zinc-200 bg-zinc-50 p-3">
        <summary className="cursor-pointer text-xs font-semibold text-zinc-700">Xem payload</summary>
        <pre className="mt-3 max-h-56 overflow-auto whitespace-pre-wrap text-xs text-zinc-700">{bodyPreview}</pre>
      </details>
    </article>
  )
}

export function HermesReportDetailDrawer({ open, loading, report, onClose }: Props) {
  const parsedActions = useMemo(() => parseHermesActions(report?.summary ?? null), [report?.summary])

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

              {parsedActions.actions.length > 0 ? (
                <section className="space-y-3">
                  <div>
                    <h3 className="text-sm font-semibold text-zinc-800">Hành động gợi ý</h3>
                    <p className="text-xs text-zinc-500">UI chỉ hiển thị. Hermes runner tự gọi API bằng X-Hermes-Admin-Key.</p>
                  </div>
                  {parsedActions.actions.map((action) => (
                    <HermesActionCard key={action.id ?? `${action.method}:${action.path}:${action.title}`} action={action} />
                  ))}
                </section>
              ) : parsedActions.error ? (
                <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-700">
                  {parsedActions.error}
                </div>
              ) : null}

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
