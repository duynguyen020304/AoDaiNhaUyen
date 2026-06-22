import { useEffect, useMemo, useState } from 'react'
import { Download, Loader2, X } from 'lucide-react'
import Markdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { downloadHermesReportPdf } from '@/api/hermes'
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

// Compact markdown renderer so report summaries read like the chat — clean prose,
// headings, lists and tables instead of raw `##`/`|---|` text in a <pre>.
const markdownComponents = {
  h1: ({ children }: { children?: React.ReactNode }) => (
    <h2 className="mt-4 mb-1.5 text-base font-bold tracking-tight text-zinc-900 first:mt-0">{children}</h2>
  ),
  h2: ({ children }: { children?: React.ReactNode }) => (
    <h3 className="mt-4 mb-1.5 text-sm font-bold tracking-tight text-zinc-900 first:mt-0">{children}</h3>
  ),
  h3: ({ children }: { children?: React.ReactNode }) => (
    <h4 className="mt-3 mb-1 text-sm font-semibold text-zinc-800 first:mt-0">{children}</h4>
  ),
  p: ({ children }: { children?: React.ReactNode }) => (
    <p className="my-1.5 text-sm leading-relaxed text-zinc-700">{children}</p>
  ),
  ul: ({ children }: { children?: React.ReactNode }) => (
    <ul className="my-2 list-disc space-y-1 pl-5 text-sm text-zinc-700">{children}</ul>
  ),
  ol: ({ children }: { children?: React.ReactNode }) => (
    <ol className="my-2 list-decimal space-y-1 pl-5 text-sm text-zinc-700">{children}</ol>
  ),
  li: ({ children }: { children?: React.ReactNode }) => (
    <li className="my-0.5 leading-relaxed">{children}</li>
  ),
  table: ({ children }: { children?: React.ReactNode }) => (
    <div className="my-3 overflow-x-auto rounded-xl border border-zinc-200 bg-white shadow-sm">
      <table className="w-full divide-y divide-zinc-200 text-xs">{children}</table>
    </div>
  ),
  thead: ({ children }: { children?: React.ReactNode }) => <thead className="bg-zinc-50">{children}</thead>,
  tbody: ({ children }: { children?: React.ReactNode }) => <tbody className="divide-y divide-zinc-100">{children}</tbody>,
  th: ({ children }: { children?: React.ReactNode }) => (
    <th className="px-3 py-2 text-left font-semibold uppercase tracking-wider text-zinc-500">{children}</th>
  ),
  td: ({ children }: { children?: React.ReactNode }) => <td className="px-3 py-2 text-zinc-700">{children}</td>,
  // Inline code stays subtle; fenced code blocks are intentionally NOT surfaced in
  // report prose (they are stripped before render), so render any stray code plainly.
  code: ({ children }: { children?: React.ReactNode }) => (
    <code className="rounded-md border border-zinc-200/60 bg-zinc-100 px-1.5 py-0.5 text-xs text-zinc-700">{children}</code>
  ),
  strong: ({ children }: { children?: React.ReactNode }) => (
    <strong className="font-semibold text-zinc-900">{children}</strong>
  ),
  a: ({ children }: { children?: React.ReactNode }) => <span className="text-zinc-700">{children}</span>,
  hr: () => <hr className="my-4 border-zinc-200" />,
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

// Hide machine-facing noise from the human-readable summary: strip fenced ```json```
// action blocks and any now-empty legacy "API đề xuất" heading. Clean prose remains.
function cleanSummary(summary: string | null): string {
  if (!summary) return ''
  return summary
    .replace(/```[a-z]*\s*[\s\S]*?```/gi, '')
    .replace(/^#{1,4}\s*API\s*đề\s*xuất\s*:?\s*$/gimu, '')
    .replace(/\n{3,}/g, '\n\n')
    .trim()
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

function actionsFromContainer(container: unknown): HermesRawAction[] {
  if (!isRecord(container) || !Array.isArray(container.actions)) return []
  return container.actions
    .map((action, index) => normalizeAction(action, index))
    .filter((action): action is HermesRawAction => action !== null)
}

// New contract: executable actions live in payloadJson.actions[]. Legacy reports
// embedded them as a ```json``` block in the summary — fall back to that so old
// reports still render their action cards.
function parseActions(payloadJson: string | null, summary: string | null): ParsedActionsResult {
  if (payloadJson) {
    try {
      const fromPayload = actionsFromContainer(JSON.parse(payloadJson))
      if (fromPayload.length > 0) return { actions: fromPayload, error: null }
    } catch {
      // fall through to legacy summary parsing
    }
  }

  if (summary) {
    const jsonBlocks = [...summary.matchAll(/```json\s*([\s\S]*?)```/gi)]
    for (const block of jsonBlocks) {
      try {
        const parsed = JSON.parse(block[1].trim())
        if (!isRecord(parsed) || !Array.isArray(parsed.actions)) continue
        const actions = actionsFromContainer(parsed)
        return actions.length > 0
          ? { actions, error: null }
          : { actions: [], error: 'JSON actions thiếu method/path/title/risk hợp lệ.' }
      } catch {
        return { actions: [], error: 'Không đọc được JSON hành động Hermes.' }
      }
    }
  }

  return { actions: [], error: null }
}

function riskClassName(risk: string) {
  if (risk === 'high') return 'border-red-200 bg-red-50 text-red-700'
  if (risk === 'medium') return 'border-amber-200 bg-amber-50 text-amber-700'
  return 'border-emerald-200 bg-emerald-50 text-emerald-700'
}

function riskLabel(risk: string) {
  if (risk === 'high') return 'Rủi ro cao'
  if (risk === 'medium') return 'Rủi ro vừa'
  return 'Rủi ro thấp'
}

function HermesActionCard({ action }: { action: HermesRawAction }) {
  return (
    <article className="space-y-2 rounded-xl border border-zinc-200 bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <h4 className="text-sm font-semibold text-zinc-900">{action.title}</h4>
        <span className={`rounded-full border px-2 py-0.5 text-xs font-semibold ${riskClassName(action.risk)}`}>
          {riskLabel(action.risk)}
        </span>
      </div>
      {action.reason ? <p className="text-xs leading-relaxed text-zinc-600">{action.reason}</p> : null}
      <p className="text-xs text-zinc-500">Hermes runner tự xử lý theo quy tắc an toàn đã cấu hình.</p>
    </article>
  )
}

export function HermesReportDetailDrawer({ open, loading, report, onClose }: Props) {
  const [downloadingPdf, setDownloadingPdf] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const cleanedSummary = useMemo(() => cleanSummary(report?.summary ?? null), [report?.summary])
  const parsedActions = useMemo(
    () => parseActions(report?.payloadJson ?? null, report?.summary ?? null),
    [report?.payloadJson, report?.summary],
  )

  useEffect(() => {
    if (!open) return
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, onClose])

  async function handleDownloadPdf() {
    if (!report || downloadingPdf) return
    setDownloadError(null)
    setDownloadingPdf(true)
    try {
      await downloadHermesReportPdf(report.id)
    } catch (err) {
      setDownloadError(err instanceof Error ? err.message : 'Không thể tải PDF báo cáo Hermes.')
    } finally {
      setDownloadingPdf(false)
    }
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label="Chi tiết báo cáo Hermes">
      <button className="absolute inset-0 bg-black/40" onClick={onClose} aria-label="Đóng chi tiết" />
      <aside className="absolute inset-y-0 right-0 flex w-full max-w-3xl flex-col bg-white shadow-xl">
        <header className="flex items-center justify-between gap-3 border-b border-zinc-200 p-4">
          <div>
            <h2 className="text-lg font-semibold text-zinc-900">Chi tiết báo cáo Hermes</h2>
            <p className="text-xs text-zinc-500">Nội dung trình bày thân thiện, không hiển thị dữ liệu kỹ thuật.</p>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => void handleDownloadPdf()} disabled={!report || downloadingPdf}>
              {downloadingPdf ? <Loader2 className="size-4 animate-spin" /> : <Download className="size-4" />}
              Tải PDF
            </Button>
            <Button variant="ghost" size="icon" onClick={onClose} aria-label="Đóng">
              <X className="size-5" />
            </Button>
          </div>
        </header>

        <div className="flex-1 overflow-y-auto p-4">
          {downloadError && (
            <div className="mb-3 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {downloadError}
            </div>
          )}
          {loading ? (
            <div className="flex h-64 items-center justify-center">
              <Loader2 className="size-6 animate-spin text-primary" />
            </div>
          ) : report ? (
            <div className="space-y-5">
              <section className="space-y-1">
                <h3 className="text-base font-semibold text-zinc-900">{report.title}</h3>
                <p className="text-xs text-zinc-500">
                  {report.reportType} · {report.severity} · {report.status} ·{' '}
                  {new Date(report.createdAt).toLocaleString('vi-VN')}
                </p>
              </section>

              <section className="prose prose-sm max-w-none">
                {cleanedSummary ? (
                  <Markdown remarkPlugins={[remarkGfm]} components={markdownComponents}>
                    {cleanedSummary}
                  </Markdown>
                ) : (
                  <p className="text-sm text-zinc-500">Báo cáo chưa có nội dung tóm tắt.</p>
                )}
              </section>

              {parsedActions.actions.length > 0 ? (
                <section className="space-y-3">
                  <div>
                    <h3 className="text-sm font-semibold text-zinc-800">Hành động Hermes đề xuất</h3>
                    <p className="text-xs text-zinc-500">Hermes runner tự gọi API nội bộ; mục này chỉ để theo dõi.</p>
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


            </div>
          ) : (
            <p className="text-sm text-zinc-500">Không có dữ liệu chi tiết.</p>
          )}
        </div>
      </aside>
    </div>
  )
}
