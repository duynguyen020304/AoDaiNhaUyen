import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronDown, ChevronUp, Bot, User, Terminal, Check, X, AlertTriangle, AlertCircle, FileText, RotateCw } from 'lucide-react'
import Markdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import type { AiMessage, AiToolCall } from '@/types/ai'
import type { AiPhaseStatus } from '@/types/blog'
import { AI_BLOG_DRAFT_STORAGE_KEY } from '@/types/blog'
import { ConfirmCard } from './ConfirmCard'
import { ChartBlock, ChartError, ChartSpecSchema } from './ChartBlock'
import { useAdminAiStore } from '@/stores/adminAiStore'

function toolLabel(name: string): string {
  const labels: Record<string, string> = {
    get_dashboard_summary: '📊 Đọc tổng quan',
    get_revenue: '💰 Đọc doanh thu',
    get_orders_by_status: '📋 Đọc trạng thái đơn hàng',
    get_recent_orders: '🛒 Đọc đơn hàng gần đây',
    get_top_products: '⭐ Đọc top sản phẩm',
    list_products: '📦 Liệt kê sản phẩm',
    get_product: '🔍 Xem sản phẩm',
    create_product: '✨ Tạo sản phẩm',
    update_product: '✏️ Cập nhật sản phẩm',
    delete_product: '🗑️ Xóa sản phẩm',
    toggle_product_status: '🔄 Đổi trạng thái sản phẩm',
    list_categories: '📁 Liệt kê danh mục',
    create_category: '📁 Tạo danh mục',
    update_category: '✏️ Cập nhật danh mục',
    delete_category: '🗑️ Xóa danh mục',
    list_users: '👥 Liệt kê người dùng',
    get_user: '👤 Xem người dùng',
    update_user_status: '🔄 Đổi trạng thái người dùng',
    update_user_role: '🔑 Đổi vai trò người dùng',
  }
  return labels[name] || `🔧 ${name}`
}

interface ToolCallCardProps {
  toolCall: AiToolCall
  status: 'pending' | 'confirmed' | 'rejected' | 'completed' | 'error'
}

function parseToolMeta(toolCall: AiToolCall) {
  const raw = toolCall.result || toolCall.input
  if (!raw) return toolCall.meta

  try {
    const parsed = JSON.parse(raw)
    return parsed?.meta || parsed?.result?.meta || toolCall.meta
  } catch {
    return toolCall.meta
  }
}

function countBlogWords(toolCall: AiToolCall) {
  return toolCall.blogDraft?.content.reduce((total, block) => {
    if ('content' in block && typeof block.content === 'string') {
      return total + block.content.trim().split(/\s+/).filter(Boolean).length
    }
    return total
  }, 0) ?? 0
}

function openBlogDraftEditor(toolCall: AiToolCall, navigate: (to: string) => void) {
  if (!toolCall.blogDraft) return
  sessionStorage.setItem(AI_BLOG_DRAFT_STORAGE_KEY, JSON.stringify(toolCall.blogDraft))
  navigate('/admin/blog/new')
}

function appendClarificationToChat(reply: string) {
  const input = document.querySelector('input[placeholder="Nhập yêu cầu..."]') as HTMLInputElement | null
  if (!input || !reply.trim()) return
  const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')?.set
  setter?.call(input, reply)
  input.dispatchEvent(new Event('input', { bubbles: true }))
  input.focus()
}

function PhaseBadge({ phase }: { phase: AiPhaseStatus }) {
  const tone = phase.status === 'completed'
    ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
    : phase.status === 'pending'
      ? 'bg-gray-50 text-gray-600 border-gray-200'
      : 'bg-amber-50 text-amber-700 border-amber-200'

  return <span className={`rounded-full border px-2 py-0.5 text-[10px] font-medium ${tone}`}>{phase.label}</span>
}

function ToolCallCard({ toolCall, status }: ToolCallCardProps) {
  const [expanded, setExpanded] = useState(false)
  const navigate = useNavigate()
  const label = toolLabel(toolCall.toolName)

  const meta = parseToolMeta(toolCall)
  const hasMore = meta?.hasMore === true || meta?.completeness === 'partial_page'

  return (
    <div className="bg-gray-50 border border-gray-200/60 rounded-xl p-2.5 text-xs shadow-sm transition-all hover:bg-gray-100/50">
      <button
        onClick={() => setExpanded(!expanded)}
        className="flex items-center justify-between w-full text-left font-medium text-gray-700 hover:text-wine cursor-pointer"
      >
        <span className="flex items-center gap-2">
          {status === 'completed' && <Check className="size-4 text-green-600 shrink-0" />}
          {status === 'confirmed' && <Check className="size-4 text-green-600 shrink-0" />}
          {status === 'rejected' && <X className="size-4 text-red-500 shrink-0" />}
          {status === 'pending' && <AlertTriangle className="size-4 text-amber-500 shrink-0 animate-pulse" />}
          {status === 'error' && <AlertCircle className="size-4 text-red-500 shrink-0" />}
          
          <Terminal className="size-3.5 text-gray-500" />
          {label}
        </span>
        {expanded ? <ChevronUp className="size-3.5" /> : <ChevronDown className="size-3.5" />}
      </button>
      {toolCall.blogClarification && (
        <div className="mt-2 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900 shadow-sm">
          <div className="space-y-2">
            <div className="font-semibold">Cần thêm thông tin để hoàn thiện bài dài và chuẩn SEO</div>
            {toolCall.blogClarification.templateReason && (
              <p className="text-xs leading-relaxed">Template dự kiến: <span className="font-medium">{toolCall.blogClarification.selectedTemplate || 'Auto'}</span> — {toolCall.blogClarification.templateReason}</p>
            )}
            <ul className="list-disc pl-4 text-xs leading-relaxed space-y-1">
              {toolCall.blogClarification.questions.map((question) => <li key={question}>{question}</li>)}
            </ul>
            <div className="flex flex-wrap gap-1">
              {(toolCall.blogClarification.suggestedAnswers || []).map((answer) => (
                <button
                  key={answer}
                  type="button"
                  onClick={() => appendClarificationToChat(answer)}
                  className="rounded-full border border-amber-300 bg-white px-2 py-1 text-[11px] font-medium text-amber-800 hover:bg-amber-100"
                >
                  {answer}
                </button>
              ))}
            </div>
            {toolCall.phases && toolCall.phases.length > 0 && (
              <div className="flex flex-wrap gap-1 pt-1">
                {toolCall.phases.map((phase) => <PhaseBadge key={`${phase.phase}-${phase.label}`} phase={phase} />)}
              </div>
            )}
          </div>
        </div>
      )}
      {toolCall.blogDraft && (
        <div className="mt-2 rounded-xl border border-wine/15 bg-white p-3 text-sm text-gray-700 shadow-sm">
          <div className="flex items-start gap-2">
            <FileText className="mt-0.5 size-4 text-wine shrink-0" />
            <div className="min-w-0 flex-1 space-y-2">
              <div>
                <div className="font-semibold text-gray-900 line-clamp-2">{toolCall.blogDraft.title}</div>
                <p className="mt-1 text-xs leading-relaxed text-gray-600 line-clamp-3">{toolCall.blogDraft.excerpt}</p>
              </div>
              <div className="flex flex-wrap gap-1 text-[11px] text-gray-600">
                <span className="rounded-full bg-gray-100 px-2 py-0.5">{toolCall.blogDraft.content.length} block</span>
                <span className="rounded-full bg-gray-100 px-2 py-0.5">~{countBlogWords(toolCall)} từ</span>
                <span className="rounded-full bg-gray-100 px-2 py-0.5">{toolCall.blogDraft.selectedTemplate || toolCall.blogDraft.template}</span>
                {toolCall.blogDraft.tags.slice(0, 3).map((tag) => (
                  <span key={tag} className="rounded-full bg-wine/5 px-2 py-0.5 text-wine">{tag}</span>
                ))}
              </div>
              {toolCall.blogDraft.templateReason && (
                <div className="text-[11px] leading-relaxed text-gray-600">Template: <span className="font-medium text-gray-800">{toolCall.blogDraft.selectedTemplate || toolCall.blogDraft.template}</span> — {toolCall.blogDraft.templateReason}</div>
              )}
              {toolCall.blogDraft.phases && toolCall.blogDraft.phases.length > 0 && (
                <div className="flex flex-wrap gap-1">
                  {toolCall.blogDraft.phases.map((phase) => <PhaseBadge key={`${phase.phase}-${phase.label}`} phase={phase} />)}
                </div>
              )}
              {toolCall.blogDraft.imagePlan?.featuredPrompt && (
                <div className="rounded-lg border border-gray-200 bg-gray-50 px-2 py-1 text-[11px] text-gray-700">
                  <div className="font-medium text-gray-800">Prompt ảnh nổi bật</div>
                  <div className="mt-1 line-clamp-3 leading-relaxed">{toolCall.blogDraft.imagePlan.featuredPrompt}</div>
                </div>
              )}
              {toolCall.blogDraft.qualityWarnings && toolCall.blogDraft.qualityWarnings.length > 0 && (
                <div className="rounded-lg border border-amber-200 bg-amber-50 px-2 py-1 text-[11px] text-amber-800">
                  {toolCall.blogDraft.qualityWarnings.slice(0, 3).join(' • ')}
                </div>
              )}
              {toolCall.warnings && toolCall.warnings.length > 0 && (
                <div className="rounded-lg border border-amber-200 bg-amber-50 px-2 py-1 text-[11px] text-amber-800">
                  {toolCall.warnings.slice(0, 3).join(' • ')}
                </div>
              )}
              <button
                type="button"
                onClick={() => openBlogDraftEditor(toolCall, navigate)}
                className="inline-flex items-center justify-center rounded-lg bg-wine px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-wine/90 active:scale-95"
              >
                Mở trong trình soạn
              </button>
            </div>
          </div>
        </div>
      )}
      {toolCall.generatedImages && toolCall.generatedImages.length > 0 && (
        <div className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-2">
          {toolCall.generatedImages.slice(0, 4).map((image) => (
            <a
              key={image.url}
              href={image.url}
              target="_blank"
              rel="noopener noreferrer nofollow"
              className="group overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm transition hover:border-wine/30 hover:shadow-md"
            >
              <img
                src={image.url}
                alt={image.alt || image.label || 'Ảnh AI đã tạo'}
                loading="lazy"
                className="aspect-[4/3] w-full object-cover transition duration-300 group-hover:scale-[1.02]"
              />
              <div className="px-2 py-1.5 text-[11px] font-medium text-gray-600 line-clamp-1">
                {image.label || image.kind || 'Ảnh AI đã tạo'}
              </div>
            </a>
          ))}
        </div>
      )}
      {hasMore && (
        <div className="mt-2 rounded-lg border border-amber-200 bg-amber-50 px-2 py-1 text-[11px] font-medium text-amber-800">
          Còn trang khác → dữ liệu hiện tại chưa đầy đủ
        </div>
      )}
      {status === 'error' && toolCall.error && (
        <div className="mt-2 rounded-lg border border-red-200 bg-red-50 px-2 py-1.5 text-[11px] leading-relaxed text-red-800 break-words">
          {toolCall.error}
        </div>
      )}
      {expanded && (
        <div className="mt-2 text-gray-500 font-mono text-[10px] bg-white border border-gray-100 rounded-lg p-2 overflow-x-auto whitespace-pre-wrap max-h-40">
          {toolCall.input}
        </div>
      )}
    </div>
  )
}

function safeHref(href?: string) {
  if (!href) return undefined
  try {
    const url = new URL(href, window.location.origin)
    return ['http:', 'https:', 'mailto:'].includes(url.protocol) ? href : undefined
  } catch {
    return undefined
  }
}


const STREAM_INTERVAL_MS = 28
const STREAM_MIN_CHARS = 32
const STREAM_MAX_CHARS = 180

function useTokenStream(target: string, enabled: boolean) {
  const [displayText, setDisplayText] = useState(target)
  const latestTargetRef = useRef(target)

  useEffect(() => {
    latestTargetRef.current = target

    const timerId = window.setTimeout(() => {
      if (!enabled) {
        setDisplayText(target)
        return
      }

      setDisplayText((current) => {
        if (!target) return ''
        if (target === current) return current
        if (target.startsWith(current)) return current
        return ''
      })
    }, 0)

    return () => window.clearTimeout(timerId)
  }, [enabled, target])

  useEffect(() => {
    if (!enabled || displayText === target) return

    const timerId = window.setTimeout(() => {
      setDisplayText((current) => {
        const nextTarget = latestTargetRef.current
        if (!nextTarget) return ''
        if (!nextTarget.startsWith(current)) return nextTarget.slice(0, STREAM_MIN_CHARS)

        const remaining = nextTarget.length - current.length
        const chunkSize = Math.min(STREAM_MAX_CHARS, Math.max(STREAM_MIN_CHARS, Math.ceil(remaining / 10)))
        return nextTarget.slice(0, current.length + chunkSize)
      })
    }, STREAM_INTERVAL_MS)

    return () => window.clearTimeout(timerId)
  }, [displayText, enabled, target])

  return displayText
}

const markdownComponents = {
  h1: ({ children }: { children?: React.ReactNode }) => (
    <h1 className="text-lg font-bold text-gray-900 mt-4 mb-2 first:mt-0 tracking-tight">{children}</h1>
  ),
  h2: ({ children }: { children?: React.ReactNode }) => (
    <h2 className="text-base font-bold text-gray-900 mt-4 mb-2 first:mt-0 tracking-tight">{children}</h2>
  ),
  h3: ({ children }: { children?: React.ReactNode }) => (
    <h3 className="text-sm font-semibold text-gray-800 mt-3 mb-1 first:mt-0">{children}</h3>
  ),
  p: ({ children }: { children?: React.ReactNode }) => (
    <p className="my-1.5 leading-relaxed text-gray-700">{children}</p>
  ),
  ul: ({ children }: { children?: React.ReactNode }) => (
    <ul className="my-2 pl-5 list-disc space-y-1 text-gray-700">{children}</ul>
  ),
  ol: ({ children }: { children?: React.ReactNode }) => (
    <ol className="my-2 pl-5 list-decimal space-y-1 text-gray-700">{children}</ol>
  ),
  li: ({ children }: { children?: React.ReactNode }) => (
    <li className="my-0.5 leading-relaxed">{children}</li>
  ),
  table: ({ children }: { children?: React.ReactNode }) => (
    <div className="my-3 max-w-full overflow-x-auto rounded-xl border border-gray-200 bg-white shadow-sm">
      <table className="min-w-max w-full divide-y divide-gray-200 text-xs">{children}</table>
    </div>
  ),
  thead: ({ children }: { children?: React.ReactNode }) => (
    <thead className="bg-gray-50">{children}</thead>
  ),
  tbody: ({ children }: { children?: React.ReactNode }) => (
    <tbody className="divide-y divide-gray-100">{children}</tbody>
  ),
  th: ({ children }: { children?: React.ReactNode }) => (
    <th className="whitespace-nowrap px-3 py-2 text-left font-semibold uppercase tracking-wider text-gray-500">{children}</th>
  ),
  td: ({ children }: { children?: React.ReactNode }) => (
    <td className="whitespace-nowrap px-3 py-2 text-gray-700">{children}</td>
  ),
  code: ({ children, className }: { children?: React.ReactNode; className?: string }) => {
    const match = /language-(\w+)/.exec(className || '')
    const language = match ? match[1] : ''
    const codeContent = String(children ?? '').replace(/\n$/, '').trim()

    // Declarative chart block: AI emits ```recharts { ... } ```. Validate with
    // Zod before rendering — unknown keys are stripped, never forwarded.
    if (language === 'recharts' || language === 'chart') {
      try {
        const parsed = JSON.parse(codeContent)
        const result = ChartSpecSchema.safeParse(parsed)
        if (result.success) {
          return (
            <div className="my-3">
              <ChartBlock spec={result.data} />
            </div>
          )
        }
        return (
          <ChartError
            reason={`Cấu trúc JSON không hợp lệ: ${result.error.issues.map((i) => i.path.join('.') || i.message).join('; ')}`}
            raw={codeContent}
          />
        )
      } catch (err) {
        return <ChartError reason={`JSON không hợp lệ: ${String(err)}`} raw={codeContent} />
      }
    }

    if (match) {
      return (
        <pre className="bg-gray-950 text-gray-100 p-4 rounded-xl my-3 text-xs overflow-x-auto font-mono shadow-inner border border-gray-850">
          <code className={className}>{String(children ?? '').replace(/\n$/, '')}</code>
        </pre>
      )
    }
    return (
      <code className="bg-gray-100 text-wine px-1.5 py-0.5 rounded-md text-xs font-mono border border-gray-200/60 font-semibold">{children}</code>
    )
  },
  a: ({ href, children }: { href?: string; children?: React.ReactNode }) => {
    const safe = safeHref(href)
    if (!safe) return <span className="text-gray-700">{children}</span>
    return <a href={safe} target="_blank" rel="noopener noreferrer nofollow" className="text-wine font-medium underline underline-offset-2 hover:text-wine/80">{children}</a>
  },
  blockquote: ({ children }: { children?: React.ReactNode }) => (
    <blockquote className="border-l-4 border-wine/40 pl-4 italic text-gray-500 my-3 bg-gray-50 py-1 pr-2 rounded-r-lg">{children}</blockquote>
  ),
  strong: ({ children }: { children?: React.ReactNode }) => (
    <strong className="font-semibold text-gray-900">{children}</strong>
  ),
  hr: () => <hr className="my-4 border-gray-200" />,
}

export function MessageBubble({ message }: { message: AiMessage }) {
  const isUser = message.role === 'user'
  const retryLast = useAdminAiStore((s) => s.retryLast)
  const isLoading = useAdminAiStore((s) => s.isLoading)
  const streamedContent = useTokenStream(message.content, !isUser)
  const isStreamingContent = !isUser && streamedContent !== message.content

  // Derive status from the pendingAction inside the message (persisted in store)
  const actionStatus = message.pendingAction
    ? (message.pendingAction.status || 'pending')
    : 'completed'

  // A tool call with an error renders with the error status instead of the message-level one.
  const toolStatusFor = (tc: AiToolCall): 'pending' | 'confirmed' | 'rejected' | 'completed' | 'error' =>
    tc.error ? 'error' : actionStatus

  // Show the text bubble only when there's user text, bot text, or a pending confirmation.
  // An errored assistant turn renders its own separate error banner below (not a text bubble),
  // so we don't render an empty white bubble here.
  const showTextBubble = isUser
    || message.content.trim() !== ''
    || actionStatus === 'pending'
    || message.status === 'error'

  return (
    <div className={`flex gap-3 ${isUser ? 'justify-end' : 'justify-start'}`}>
      {/* Bot avatar */}
      {!isUser && (
        <div className="size-8 rounded-xl bg-wine/10 border border-wine/20 flex items-center justify-center shrink-0 shadow-sm mt-0.5">
          <Bot className="size-4.5 text-wine" />
        </div>
      )}

      <div className={`flex min-w-0 flex-col gap-1.5 ${isUser ? 'max-w-[85%] items-end' : 'w-[calc(100%-3rem)] max-w-full items-start'}`}>
        {/* Tool calls rendered as clean badges on top of text response for flow sync */}
        {message.toolCalls && message.toolCalls.length > 0 && (
          <div className="w-full space-y-1.5 mt-1 max-w-md">
            {message.toolCalls.map((tc, i) => (
              <ToolCallCard key={i} toolCall={tc} status={toolStatusFor(tc)} />
            ))}
          </div>
        )}

        {/* Inline error banner with retry when the assistant turn failed terminally. */}
        {!isUser && message.status === 'error' && (
          <div className="flex flex-col gap-2 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 shadow-sm rounded-tl-none max-w-md">
            <div className="flex items-start gap-2">
              <AlertCircle className="mt-0.5 size-4 shrink-0 text-red-500" />
              <span className="leading-relaxed">{message.error || 'Lượt xử lý AI thất bại.'}</span>
            </div>
            <button
              type="button"
              onClick={() => { void retryLast() }}
              disabled={isLoading}
              className="inline-flex items-center justify-center gap-1.5 self-start rounded-lg bg-red-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-red-700 active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <RotateCw className={`size-3.5 ${isLoading ? 'animate-spin' : ''}`} />
              Thử lại
            </button>
          </div>
        )}

        {showTextBubble && (
          <div
            className={`rounded-2xl px-4 py-3 text-sm shadow-sm transition-all ${
              isUser
                ? 'bg-wine text-white rounded-tr-none whitespace-pre-wrap'
                : 'w-full min-w-0 overflow-hidden bg-white border border-gray-100 text-gray-800 rounded-tl-none shadow-sm'
            }`}
          >
            {isUser ? (
              message.content
            ) : (
              <div className="prose prose-sm max-w-none overflow-hidden">
                <Markdown remarkPlugins={[remarkGfm]} components={markdownComponents}>
                  {streamedContent}
                </Markdown>
                {isStreamingContent && <span className="ml-1 inline-block h-3 w-1 animate-pulse rounded-full bg-wine/50 align-middle" aria-hidden="true" />}
              </div>
            )}

            {message.pendingAction && (
              <ConfirmCard
                action={message.pendingAction}
              />
            )}
          </div>
        )}
      </div>

      {/* User avatar */}
      {isUser && (
        <div className="size-8 rounded-xl bg-gray-200 border border-gray-350 flex items-center justify-center shrink-0 shadow-sm mt-0.5">
          <User className="size-4.5 text-gray-650" />
        </div>
      )}
    </div>
  )
}
