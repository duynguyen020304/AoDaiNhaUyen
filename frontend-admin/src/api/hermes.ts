import { request, requestPaginated } from '@/api/client'
import type { PaginatedApiEnvelope } from '@/types/api'
import type { HermesReportDetail, HermesReportFilters, HermesReportListItem } from '@/types/hermes'

function cleanParams(filters: Partial<HermesReportFilters>) {
  const params = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value))
  })
  return params.toString()
}

export async function getHermesReports(filters: Partial<HermesReportFilters>): Promise<PaginatedApiEnvelope<HermesReportListItem[]>> {
  const query = cleanParams(filters)
  return requestPaginated<HermesReportListItem[]>(`/api/admin/hermes/reports${query ? `?${query}` : ''}`)
}

export async function getHermesReport(id: string): Promise<HermesReportDetail> {
  return request<HermesReportDetail>(`/api/admin/hermes/reports/${id}`)
}
