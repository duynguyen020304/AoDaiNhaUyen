import { Eye, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { HermesReportListItem } from '@/types/hermes'

interface Props {
  items: HermesReportListItem[]
  loading: boolean
  onOpen: (id: string) => void
}

const SEVERITY_BADGE: Record<string, string> = {
  info: 'bg-blue-50 text-blue-700 border-blue-200',
  warning: 'bg-amber-50 text-amber-700 border-amber-200',
  high: 'bg-orange-50 text-orange-700 border-orange-200',
  critical: 'bg-red-50 text-red-700 border-red-200',
}

function sourceLabel(source: string) {
  if (source === 'hermes_cron') return 'Chủ động'
  if (source === 'hermes_agent') return 'Theo sự kiện'
  if (source === 'hermes_chat') return 'Chat admin'
  return source
}

export function HermesReportTable({ items, loading, onOpen }: Props) {
  if (loading) {
    return (
      <div className="flex items-center justify-center rounded-xl border bg-white py-16">
        <Loader2 className="size-6 animate-spin text-primary" aria-label="Đang tải" />
      </div>
    )
  }

  if (items.length === 0) {
    return (
      <div className="rounded-xl border border-dashed bg-white p-10 text-center">
        <p className="text-sm font-medium text-ink">Chưa có báo cáo Hermes.</p>
        <p className="mt-1 text-xs text-muted-foreground">Hermes runner có thể gửi báo cáo qua endpoint callback.</p>
      </div>
    )
  }

  return (
    <div className="overflow-hidden rounded-xl border bg-white">
      <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Thời gian</TableHead>
              <TableHead>Mức độ</TableHead>
              <TableHead>Loại</TableHead>
              <TableHead>Tiêu đề</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Nguồn</TableHead>
              <TableHead className="text-right">Chi tiết</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map((item) => (
              <TableRow key={item.id} tabIndex={0} className="focus:outline-none" onKeyDown={(event) => { if (event.key === 'Enter') onOpen(item.id) }}>
                <TableCell className="whitespace-nowrap text-xs text-muted-foreground">{new Date(item.createdAt).toLocaleString('vi-VN')}</TableCell>
                <TableCell>
                  <span className={`inline-flex rounded-full border px-2 py-0.5 text-xs font-medium ${SEVERITY_BADGE[item.severity] ?? SEVERITY_BADGE.info}`}>
                    {item.severity}
                  </span>
                </TableCell>
                <TableCell className="font-mono text-xs text-muted-foreground">{item.reportType}</TableCell>
                <TableCell className="min-w-72">
                  <div className="text-sm font-medium text-ink">{item.title}</div>
                  <div className="mt-1 line-clamp-2 text-xs text-muted-foreground">{item.summaryPreview}</div>
                </TableCell>
                <TableCell className="text-xs">{item.status}</TableCell>
                <TableCell className="text-xs text-muted-foreground">
                  <span className={item.source === 'hermes_cron' ? 'rounded-full bg-emerald-50 px-2 py-0.5 font-medium text-emerald-700 ring-1 ring-emerald-200' : ''}>
                    {sourceLabel(item.source)}
                  </span>
                </TableCell>
                <TableCell className="text-right">
                  <Button variant="ghost" size="sm" onClick={() => onOpen(item.id)} aria-label="Xem chi tiết báo cáo Hermes">
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
