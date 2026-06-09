import { Eye, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { LlmAuditLogListItem } from '@/types/llmLogs'

interface Props {
  items: LlmAuditLogListItem[]
  loading: boolean
  onOpen: (id: string) => void
}

const STATUS_BADGE: Record<string, string> = {
  success: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  failed: 'bg-red-50 text-red-700 border-red-200',
  timeout: 'bg-amber-50 text-amber-700 border-amber-200',
  cancelled: 'bg-zinc-100 text-zinc-700 border-zinc-200',
  started: 'bg-blue-50 text-blue-700 border-blue-200',
}

export function LlmLogTable({ items, loading, onOpen }: Props) {
  if (loading) {
    return (
      <div className="flex items-center justify-center rounded-xl border border-zinc-200 bg-white py-16 dark:border-zinc-800 dark:bg-zinc-900">
        <Loader2 className="size-6 animate-spin text-primary" aria-label="Đang tải" />
      </div>
    )
  }

  if (items.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-zinc-300 bg-white p-10 text-center dark:border-zinc-700 dark:bg-zinc-900">
        <p className="text-sm font-medium text-zinc-700 dark:text-zinc-200">Không có log nào khớp bộ lọc.</p>
        <p className="mt-1 text-xs text-zinc-500">Thử mở rộng thời gian hoặc bỏ bớt điều kiện lọc.</p>
      </div>
    )
  }

  return (
    <div className="overflow-hidden rounded-xl border border-zinc-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
      <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Thời gian</TableHead>
              <TableHead>Nguồn</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Model</TableHead>
              <TableHead>Operation / Tool</TableHead>
              <TableHead>Latency</TableHead>
              <TableHead>Token</TableHead>
              <TableHead>Request</TableHead>
              <TableHead className="text-right">Chi tiết</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map((item) => (
              <TableRow
                key={item.id}
                tabIndex={0}
                className="hover:bg-zinc-50 focus:bg-zinc-50 focus:outline-none dark:hover:bg-zinc-900/70"
                onKeyDown={(event) => {
                  if (event.key === 'Enter') onOpen(item.id)
                }}
              >
                <TableCell className="whitespace-nowrap text-xs text-zinc-500">{new Date(item.createdAt).toLocaleString('vi-VN')}</TableCell>
                <TableCell className="text-sm font-medium">{item.source}</TableCell>
                <TableCell>
                  <span className={`inline-flex rounded-full border px-2 py-0.5 text-xs font-medium ${STATUS_BADGE[item.status] ?? STATUS_BADGE.started}`}>
                    {item.status}
                  </span>
                </TableCell>
                <TableCell className="text-xs text-zinc-500">{item.provider}{item.model ? ` / ${item.model}` : ''}</TableCell>
                <TableCell className="text-xs">
                  <div className="font-medium text-zinc-800 dark:text-zinc-100">{item.operation}</div>
                  <div className="text-zinc-500">{item.toolName || '—'}</div>
                </TableCell>
                <TableCell>{item.latencyMs ? `${item.latencyMs}ms` : '—'}</TableCell>
                <TableCell>{item.totalTokens ?? '—'}</TableCell>
                <TableCell className="font-mono text-xs text-zinc-500">{item.requestId.slice(0, 10)}…</TableCell>
                <TableCell className="text-right">
                  <Button variant="ghost" size="sm" onClick={() => onOpen(item.id)} aria-label="Xem chi tiết log">
                    <Eye className="size-4" />
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
