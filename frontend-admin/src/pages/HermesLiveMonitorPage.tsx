import { useEffect, useMemo, useRef, useState } from 'react'
import { Bot, Radio, Store } from 'lucide-react'
import { HERMES_FEED_SSE_URL, getHermesFeedSnapshot } from '@/api/hermes'
import type { HermesFeedHeartbeat, HermesFeedHermesMessage, HermesFeedItem, HermesFeedSnapshot } from '@/types/hermes'

const statusLabel: Record<string, string> = {
  pending: 'Chờ xử lý',
  processing: 'Đang xử lý',
  completed: 'Hoàn tất',
  failed: 'Lỗi',
  dead: 'Dừng retry',
  cancelled: 'Đã hủy',
}

const statusClass: Record<string, string> = {
  pending: 'bg-amber-50 text-amber-700 ring-amber-200',
  processing: 'bg-blue-50 text-blue-700 ring-blue-200',
  completed: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  failed: 'bg-rose-50 text-rose-700 ring-rose-200',
  dead: 'bg-zinc-900 text-white ring-zinc-700',
  cancelled: 'bg-zinc-100 text-zinc-600 ring-zinc-200',
}
/** Detect raw JSON / tool-call noise. Returns { displayText, rawDetail } — rawDetail is non-null when noise detected. */
function cleanSummary(text: string): { displayText: string; rawDetail: string | null } {
  if (!text) return { displayText: text, rawDetail: null }
  const trimmed = text.trim()
  const looksLikeJson =
    (trimmed.startsWith('{') && trimmed.endsWith('}')) ||
    (trimmed.startsWith('[') && trimmed.endsWith(']')) ||
    trimmed.includes('"type":"function_call"') ||
    trimmed.includes('"type":"function_call_output"') ||
    trimmed.includes('"output":[') ||
    /\b(curl|POST|GET|PUT|DELETE)\s+\/.*HTTP/i.test(trimmed)

  if (looksLikeJson) {
    return { displayText: 'Hermes đã ghi nhận kết quả phân tích.', rawDetail: trimmed }
  }
  return { displayText: trimmed, rawDetail: null }
}


type ConnectionState = 'connecting' | 'live' | 'polling' | 'error'

export function HermesLiveMonitorPanel() {
  const [snapshot, setSnapshot] = useState<HermesFeedSnapshot | null>(null)
  const [connection, setConnection] = useState<ConnectionState>('connecting')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    let source: EventSource | null = null
    let pollTimer: number | null = null
    let reconnectTimer: number | null = null
    let connectTimeout: number | null = null

    function clearConnectTimeout() {
      if (connectTimeout !== null) {
        window.clearTimeout(connectTimeout)
        connectTimeout = null
      }
    }

    function stopPolling() {
      if (pollTimer !== null) {
        window.clearInterval(pollTimer)
        pollTimer = null
      }
    }

    async function poll() {
      try {
        const data = await getHermesFeedSnapshot()
        if (cancelled) return
        setSnapshot(data)
        setConnection('polling')
        setError(null)
      } catch {
        if (cancelled) return
        setConnection('error')
        setError('Không tải được live feed Hermes.')
      }
    }

    function startPolling() {
      if (pollTimer !== null) return
      void poll()
      pollTimer = window.setInterval(() => void poll(), 7000)
    }

    function scheduleReconnect() {
      if (reconnectTimer !== null) return
      reconnectTimer = window.setTimeout(() => {
        reconnectTimer = null
        stopPolling()
        connect()
      }, 30000)
    }

    function connect() {
      if (cancelled) return
      setConnection('connecting')
      source?.close()
      source = new EventSource(HERMES_FEED_SSE_URL, { withCredentials: true })

      connectTimeout = window.setTimeout(() => {
        source?.close()
        clearConnectTimeout()
        startPolling()
        scheduleReconnect()
      }, 5000)

      source.addEventListener('open', () => {
        if (cancelled) return
        clearConnectTimeout()
        stopPolling()
        setConnection('live')
      })

      source.addEventListener('snapshot', (event) => {
        if (cancelled) return
        clearConnectTimeout()
        const data = JSON.parse((event as MessageEvent).data) as HermesFeedSnapshot
        setSnapshot(data)
        setConnection('live')
        setError(null)
      })

      source.addEventListener('error', () => {
        source?.close()
        clearConnectTimeout()
        if (cancelled) return
        startPolling()
        scheduleReconnect()
      })
    }

    connect()

    return () => {
      cancelled = true
      source?.close()
      clearConnectTimeout()
      stopPolling()
      if (reconnectTimer !== null) window.clearTimeout(reconnectTimer)
    }
  }, [])

  const items = useMemo(() => snapshot?.items ?? [], [snapshot?.items])
  const heartbeat = snapshot?.heartbeat ?? null
  const hermesMessages = useMemo(() => flattenHermesMessages(items), [items])

  return (
    <div className="flex h-full min-h-0 flex-col overflow-hidden bg-[#f4f4f5]">
      <header className="shrink-0 border-b border-zinc-200 bg-white px-4 py-4 lg:px-6">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <div className="flex items-center gap-2 text-sm font-semibold text-wine">
              <Radio className="size-4" />
              Monitor toàn cửa hàng
            </div>
            <h1 className="mt-1 text-2xl font-bold tracking-tight text-zinc-950">Hermes Live Monitor</h1>
            <p className="mt-1 text-sm text-zinc-600">Cửa hàng ở bên trái. Hermes Agent phân tích ở bên phải.</p>
          </div>
          <StatusBar connection={connection} heartbeat={heartbeat} count={items.length} generatedAt={snapshot?.generatedAt ?? null} />
        </div>
        {error && <div className="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700" role="alert">{error}</div>}
      </header>

      <main className="grid min-h-0 flex-1 gap-0 lg:grid-cols-2">
        <ChatPanel title="Cửa hàng" subtitle="Sự kiện đang xảy ra" icon={<Store className="size-5" />} tone="store">
          {items.length === 0 ? <EmptyStore /> : items.map((item) => <StoreBubble key={item.eventId} item={item} />)}
        </ChatPanel>

        <ChatPanel title="Hermes Agent" subtitle="Suy nghĩ an toàn và kết quả" icon={<Bot className="size-5" />} tone="agent">
          {hermesMessages.length === 0 ? <EmptyAgent /> : hermesMessages.map((message) => <HermesBubble key={message.key} message={message.message} />)}
        </ChatPanel>
      </main>
    </div>
  )
}

function StatusBar({ connection, heartbeat, count, generatedAt }: { connection: ConnectionState; heartbeat: HermesFeedHeartbeat | null; count: number; generatedAt: string | null }) {
  const live = connection === 'live'
  return (
    <div className="flex flex-wrap items-center gap-2 text-xs font-semibold text-zinc-700">
      <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 ring-1 ${live ? 'bg-emerald-50 text-emerald-700 ring-emerald-200' : connection === 'error' ? 'bg-rose-50 text-rose-700 ring-rose-200' : 'bg-amber-50 text-amber-700 ring-amber-200'}`}>
        <span className={`size-2 rounded-full ${live ? 'bg-emerald-500' : connection === 'error' ? 'bg-rose-500' : 'bg-amber-500'}`} />
        {live ? 'Trực tiếp' : connection === 'polling' ? 'Đang tải' : connection === 'error' ? 'Mất kết nối' : 'Đang kết nối'}
      </span>
      <span className="rounded-full bg-zinc-100 px-2.5 py-1 ring-1 ring-zinc-200">{count} sự kiện gần nhất</span>
      <span className="rounded-full bg-zinc-100 px-2.5 py-1 ring-1 ring-zinc-200">Trợ lý AI: {heartbeat?.status ?? 'chưa rõ'}</span>
      <span className="rounded-full bg-zinc-100 px-2.5 py-1 ring-1 ring-zinc-200">Cập nhật: {formatTime(generatedAt)}</span>
    </div>
  )
}

function ChatPanel({ title, subtitle, icon, tone, children }: { title: string; subtitle: string; icon: React.ReactNode; tone: 'store' | 'agent'; children: React.ReactNode }) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const node = ref.current
    if (!node) return
    node.scrollTop = node.scrollHeight
  }, [children])

  return (
    <section className={`flex min-h-0 flex-col border-zinc-200 ${tone === 'agent' ? 'border-t bg-[#eef2ff] lg:border-l lg:border-t-0' : 'bg-white'}`}>
      <div className="flex shrink-0 items-center justify-between border-b border-zinc-200 bg-white/90 px-4 py-3 backdrop-blur lg:px-5">
        <div className="flex items-center gap-3">
          <div className={`flex size-10 items-center justify-center rounded-full ${tone === 'agent' ? 'bg-indigo-600 text-white' : 'bg-wine text-white'}`}>{icon}</div>
          <div>
            <h2 className="font-bold text-zinc-950">{title}</h2>
            <p className="text-xs text-zinc-500">{subtitle}</p>
          </div>
        </div>
      </div>
      <div ref={ref} className="min-h-0 flex-1 space-y-4 overflow-y-auto px-4 py-5 lg:px-6">
        {children}
      </div>
    </section>
  )
}

function StoreBubble({ item }: { item: HermesFeedItem }) {
  return (
    <article className="flex items-start gap-3">
      <div className="mt-1 flex size-9 shrink-0 items-center justify-center rounded-full bg-wine/10 text-lg" aria-hidden="true">🏪</div>
      <div className="max-w-[86%] rounded-3xl rounded-tl-md bg-zinc-100 px-4 py-3 text-zinc-900 shadow-sm ring-1 ring-zinc-200">
        <p className="text-sm font-semibold leading-6">{item.storeMessage}</p>
        <div className="mt-2 flex flex-wrap items-center gap-2">
          <span className={`rounded-full px-2 py-0.5 text-[11px] font-semibold ring-1 ${statusClass[item.eventStatus] ?? 'bg-zinc-50 text-zinc-600 ring-zinc-200'}`}>
            {statusLabel[item.eventStatus] ?? item.eventStatus}
          </span>
          <span className="text-[11px] font-medium text-zinc-500">{relativeTime(item.storeTime)}</span>
        </div>
      </div>
    </article>
  )
}

function HermesBubble({ message }: { message: HermesFeedHermesMessage }) {
  const tone = getHermesTone(message)
  const { displayText, rawDetail } = cleanSummary(message.summary)
  return (
    <article className="flex items-start justify-end gap-3">
      <div className={`max-w-[90%] rounded-3xl rounded-tr-md px-4 py-3 shadow-sm ring-1 ${tone.className}`}>
        <div className="mb-1 flex items-center gap-2">
          <span className="text-base" aria-hidden="true">{tone.icon}</span>
          <span className="text-xs font-bold uppercase tracking-wide text-current/70">{tone.label}</span>
        </div>
        {message.title && <h3 className="text-sm font-bold leading-6">{message.title}</h3>}
        <p className="whitespace-pre-wrap text-sm leading-6">{displayText}</p>
        {rawDetail && (
          <details className="mt-2">
            <summary className="cursor-pointer text-xs font-medium text-current/60 hover:text-current/80">Chi tiết kỹ thuật</summary>
            <pre className="mt-1 max-h-40 overflow-auto rounded-lg bg-black/5 p-2 text-[11px] leading-4 whitespace-pre-wrap">{rawDetail}</pre>
          </details>
        )}
        <div className="mt-2 flex flex-wrap items-center justify-end gap-2 text-[11px] font-medium text-current/60">
          <span>Hermes · {relativeTime(message.time)}</span>
        </div>
      </div>
      <div className="mt-1 flex size-9 shrink-0 items-center justify-center rounded-full bg-indigo-600 text-white" aria-hidden="true">
        <Bot className="size-4" />
      </div>
    </article>
  )
}

function EmptyStore() {
  return (
    <div className="flex h-full min-h-80 flex-col items-center justify-center text-center text-zinc-500">
      <Store className="mb-3 size-12 text-zinc-300" />
      <p className="font-semibold text-zinc-700">Chưa có event cửa hàng.</p>
      <p className="mt-1 text-sm">Khi khách đặt hàng hoặc admin thay đổi dữ liệu, bong bóng sẽ xuất hiện tại đây.</p>
    </div>
  )
}

function EmptyAgent() {
  return (
    <div className="flex h-full min-h-80 flex-col items-center justify-center text-center text-zinc-500">
      <Bot className="mb-3 size-12 text-indigo-200" />
      <p className="font-semibold text-zinc-700">Hermes chưa có phân tích.</p>
      <p className="mt-1 text-sm">Khi worker xử lý event, suy nghĩ an toàn và báo cáo sẽ hiện tại đây.</p>
    </div>
  )
}

function getHermesTone(message: HermesFeedHermesMessage) {
  if (message.kind === 'error') {
    return { icon: '⚠️', label: 'Lỗi', className: 'bg-rose-50 text-rose-900 ring-rose-200' }
  }
  if (message.kind === 'report') {
    if (message.severity === 'critical' || message.severity === 'high') return { icon: '📊', label: 'Báo cáo', className: 'bg-rose-50 text-rose-950 ring-rose-200' }
    if (message.severity === 'warning') return { icon: '📊', label: 'Báo cáo', className: 'bg-amber-50 text-amber-950 ring-amber-200' }
    return { icon: '📊', label: 'Báo cáo', className: 'bg-emerald-50 text-emerald-950 ring-emerald-200' }
  }
  if (message.kind === 'thinking') {
    return { icon: '🤔', label: 'Đang suy nghĩ', className: 'bg-white text-zinc-900 ring-indigo-100' }
  }
  return { icon: '•', label: 'Bước xử lý', className: 'bg-white text-zinc-800 ring-zinc-200' }
}

function flattenHermesMessages(items: HermesFeedItem[]) {
  return items.flatMap((item) => item.hermesMessages.map((message, index) => ({
    key: `${item.eventId}-${message.time}-${message.kind}-${index}`,
    item,
    message,
  })))
}

function relativeTime(value: string | null | undefined) {
  if (!value) return 'chưa rõ'
  const time = new Date(value).getTime()
  if (Number.isNaN(time)) return 'chưa rõ'
  const diff = Math.max(0, Date.now() - time)
  const minutes = Math.floor(diff / 60_000)
  if (minutes < 1) return 'vừa xong'
  if (minutes < 60) return `${minutes} phút trước`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} giờ trước`
  const days = Math.floor(hours / 24)
  return `${days} ngày trước`
}

function formatTime(value: string | null) {
  if (!value) return 'chưa có'
  return new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }).format(new Date(value))
}


export function HermesLiveMonitorPage() {
  return <HermesLiveMonitorPanel />
}
