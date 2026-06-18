export interface HermesReportListItem {
  id: string
  reportType: string
  severity: 'info' | 'warning' | 'high' | 'critical' | string
  title: string
  summaryPreview: string
  source: string
  correlationId: string | null
  runId: string | null
  status: string
  createdAt: string
}

export interface HermesReportDetail {
  id: string
  reportType: string
  severity: string
  title: string
  summary: string
  payloadJson: string | null
  source: string
  correlationId: string | null
  runId: string | null
  status: string
  createdAt: string
}

export interface HermesReportFilters {
  page: number
  pageSize: number
  severity?: string
  type?: string
  status?: string
  q?: string
}
