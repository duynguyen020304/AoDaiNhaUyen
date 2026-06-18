import { useEffect, useMemo, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Activity, AlertTriangle, Clock, FileText, Radio, ShieldCheck } from 'lucide-react'
import { useParams } from 'react-router-dom'
import { API_BASE_URL } from '@/api/client'
import { getHermesMonitorSnapshot } from '@/api/hermes'
import { queryKeys } from '@/queries/queryKeys'
import type { HermesMonitorSnapshot } from '@/types/hermes'

const statusLabel: Record<string, string> = {
  pending: 'Đang chờ',
  processing: 'Đang xử lý',
  completed: 'Hoàn tất',
  failed: 'Lỗi',
  dead: 'Dừng retry',
  cancelled: 'Đã hủy',
}

const statusClass: Record<string, string> = {
  pending: 'bg-amber-50 text-amber-800 ring-amber-200',
  processing: 'bg-blue-50 text-blue-800 ring-blue-200',
  completed: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  failed: 'bg-rose-50 text-rose-800 ring-rose-200',
  dead: 'bg-zinc-900 text-white ring-zinc-700',
  cancelled: 'bg-zinc-100 text-zinc-700 ring-zinc-200',
}

function fmt(value: string | null | undefined) {
  if (!value) return 'Chưa có'
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'medium' }).format(new Date(value))
}

function shortId(id: string) {
  return id.slice(0, 8)
}

export function HermesMonitorPage() {
  const { token = '' } = useParams()
  const queryClient = useQueryClient()
  const [streamState, setStreamState] = useState<'connecting' | 'online' | 'closed' | 'fallback'>('connecting')
  const monitorKey = useMemo(() => queryKeys.hermes.monitor(token), [token])

  const snapshotQuery = useQuery({
    queryKey: monitorKey,
    queryFn: () => getHermesMonitorSnapshot(token),
    enabled: token.length > 0,
    refetchInterval: streamState === 'fallback' ? 7_000 : false,
    retry: 1,
  })

  useEffect(() => {
    if (!token) return
    const source = new EventSource(`${API_BASE_URL}/api/public/hermes/monitor/${encodeURIComponent(token)}/stream`)

    source.addEventListener('open', () => setStreamState('online'))
    source.addEventListener('snapshot', (event) => {
      const data = JSON.parse((event as MessageEvent).data) as HermesMonitorSnapshot
      queryClient.setQueryData(monitorKey, data)
      setStreamState('online')
    })
    source.addEventListener('completed', () => {
      setStreamState('closed')
      source.close()
    })
    source.addEventListener('error', () => {
      setStreamState('fallback')
      source.close()
    })

    return () => source.close()
  }, [monitorKey, queryClient, token])

  if (snapshotQuery.isLoading) {
    return <MonitorShell><Skeleton /></MonitorShell>
  }

  if (snapshotQuery.isError || !snapshotQuery.data) {
    return (
      <MonitorShell>
        <div className="mx-auto max-w-2xl rounded-2xl border border-rose-200 bg-white p-8 text-center shadow-sm">
          <AlertTriangle className="mx-auto size-10 text-rose-600" />
          <h1 className="mt-4 text-2xl font-bold text-zinc-950">Link giám sát không hợp lệ</h1>
          <p className="mt-2 text-sm text-zinc-600">Token có thể đã hết hạn, bị thu hồi hoặc không tồn tại.</p>
        </div>
      </MonitorShell>
    )
  }

  const snapshot = snapshotQuery.data
  const timeline = buildTimeline(snapshot)

  return (
    <MonitorShell>
      <header className="rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-mono text-xs font-semibold text-zinc-500">#{shortId(snapshot.event.id)}</span>
              <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ${statusClass[snapshot.event.status] ?? 'bg-zinc-100 text-zinc-700 ring-zinc-200'}`}>
                {statusLabel[snapshot.event.status] ?? snapshot.event.status}
              </span>
              <span className="inline-flex items-center gap-1 rounded-full bg-zinc-100 px-2.5 py-1 text-xs font-semibold text-zinc-700">
                <Radio className="size-3" />
                {streamState === 'online' ? 'Live' : streamState === 'fallback' ? 'Polling' : streamState === 'closed' ? 'Đã đóng' : 'Đang kết nối'}
              </span>
            </div>
            <h1 className="mt-3 text-2xl font-bold tracking-tight text-zinc-950 lg:text-3xl">Giám sát Hermes: {snapshot.event.eventType}</h1>
            <p className="mt-2 max-w-3xl text-sm text-zinc-600">Trang công khai đọc-only. Nội dung đã che dữ liệu nhạy cảm, không hiển thị suy nghĩ thô của mô hình.</p>
          </div>
          <div className="rounded-xl bg-zinc-50 p-3 text-sm text-zinc-700 ring-1 ring-zinc-200">
            <div className="flex items-center gap-2 font-semibold"><Clock className="size-4" />Hết hạn</div>
            <div className="mt-1">{fmt(snapshot.link.expiresAt)}</div>
          </div>
        </div>
      </header>

      <main className="grid gap-5 lg:grid-cols-[minmax(0,1.4fr)_minmax(320px,0.8fr)]">
        <section className="rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2">
            <Activity className="size-5 text-wine" />
            <h2 className="text-lg font-bold text-zinc-950">Timeline xử lý</h2>
          </div>
          <div className="mt-5 space-y-4">
            {timeline.map((step, index) => (
              <article key={`${step.title}-${index}`} className="relative pl-8">
                <div className="absolute left-0 top-1.5 size-3 rounded-full bg-wine ring-4 ring-wine/10" />
                {index < timeline.length - 1 && <div className="absolute left-[5px] top-5 h-full w-px bg-zinc-200" />}
                <div className="rounded-xl border border-zinc-100 bg-zinc-50 p-4">
                  <div className="flex flex-col gap-1 md:flex-row md:items-start md:justify-between">
                    <div>
                      <h3 className="font-semibold text-zinc-950">{step.title}</h3>
                      <p className="mt-1 text-sm text-zinc-600">{step.summary}</p>
                    </div>
                    <span className="text-xs text-zinc-500">{fmt(step.time)}</span>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </section>

        <aside className="space-y-5">
          <InfoCard title="Tóm tắt quá trình xử lý" icon={<ShieldCheck className="size-5" />}>
            <p className="text-sm leading-6 text-zinc-700">{snapshot.thinkingSummary}</p>
          </InfoCard>

          <InfoCard title="Event" icon={<Activity className="size-5" />}>
            <dl className="space-y-2 text-sm">
              <Row label="Aggregate" value={`${snapshot.event.aggregateType} / ${snapshot.event.aggregateId}`} />
              <Row label="Retry" value={`${snapshot.event.attempts}/${snapshot.event.maxAttempts}`} />
              <Row label="Correlation" value={snapshot.event.correlationId ?? 'Không có'} />
              <Row label="Tạo lúc" value={fmt(snapshot.event.createdAt)} />
              <Row label="Xử lý lúc" value={fmt(snapshot.event.processedAt)} />
            </dl>
          </InfoCard>

          <InfoCard title="Runner" icon={<Radio className="size-5" />}>
            {snapshot.heartbeat ? (
              <dl className="space-y-2 text-sm">
                <Row label="Runner" value={snapshot.heartbeat.runnerName} />
                <Row label="Trạng thái" value={snapshot.heartbeat.status} />
                <Row label="Gateway" value={snapshot.heartbeat.gatewayStatus ?? 'Không rõ'} />
                <Row label="Jobs" value={String(snapshot.heartbeat.activeJobs)} />
                <Row label="Heartbeat" value={fmt(snapshot.heartbeat.recordedAt)} />
              </dl>
            ) : <p className="text-sm text-zinc-600">Chưa có heartbeat.</p>}
          </InfoCard>

          <InfoCard title="Báo cáo cuối" icon={<FileText className="size-5" />}>
            {snapshot.reports.length === 0 ? (
              <p className="text-sm text-zinc-600">Chưa có báo cáo.</p>
            ) : (
              <div className="space-y-3">
                {snapshot.reports.map((report) => (
                  <article key={report.id} className="rounded-xl bg-zinc-50 p-3 ring-1 ring-zinc-100">
                    <div className="text-sm font-semibold text-zinc-950">{report.title}</div>
                    <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-zinc-700">{report.summary}</p>
                  </article>
                ))}
              </div>
            )}
          </InfoCard>
        </aside>
      </main>
    </MonitorShell>
  )
}

function MonitorShell({ children }: { children: React.ReactNode }) {
  return <div className="min-h-dvh bg-zinc-100 px-4 py-6 text-zinc-950 lg:px-8"><div className="mx-auto max-w-7xl space-y-5">{children}</div></div>
}

function InfoCard({ title, icon, children }: { title: string; icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-center gap-2 text-zinc-950">
        <span className="text-wine">{icon}</span>
        <h2 className="font-bold">{title}</h2>
      </div>
      {children}
    </section>
  )
}

function Row({ label, value }: { label: string; value: string }) {
  return <div className="grid grid-cols-[96px_minmax(0,1fr)] gap-3"><dt className="text-zinc-500">{label}</dt><dd className="break-words font-medium text-zinc-800">{value}</dd></div>
}

function Skeleton() {
  return <div className="space-y-5"><div className="h-36 animate-pulse rounded-2xl bg-white" /><div className="grid gap-5 lg:grid-cols-[minmax(0,1.4fr)_minmax(320px,0.8fr)]"><div className="h-96 animate-pulse rounded-2xl bg-white" /><div className="h-96 animate-pulse rounded-2xl bg-white" /></div></div>
}

function buildTimeline(snapshot: HermesMonitorSnapshot) {
  const base = [
    {
      title: 'Event vào outbox',
      summary: `${snapshot.event.eventType} cho ${snapshot.event.aggregateType}.`,
      time: snapshot.event.createdAt,
    },
    ...snapshot.traceSteps.map((step) => ({ title: step.title, summary: step.summary, time: step.startedAt })),
    ...snapshot.runs.map((run) => ({
      title: `Run Hermes ${statusLabel[run.status] ?? run.status}`,
      summary: run.resultSummary || run.safeError || run.promptSummary,
      time: run.startedAt,
    })),
    ...snapshot.reports.map((report) => ({ title: `Báo cáo: ${report.title}`, summary: report.summary, time: report.createdAt })),
  ]

  return base.sort((a, b) => new Date(a.time).getTime() - new Date(b.time).getTime())
}
