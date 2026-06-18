import { request, requestPaginated } from '@/api/client'
import type { PaginatedApiEnvelope } from '@/types/api'
import type { CreateHermesMonitorLinkRequest, HermesEventFilters, HermesEventListItem, HermesMonitorLink, HermesMonitorSnapshot, HermesReportDetail, HermesReportFilters, HermesReportListItem } from '@/types/hermes'

function cleanParams(filters: Record<string, unknown>) {
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

export async function getHermesEvents(filters: Partial<HermesEventFilters>): Promise<PaginatedApiEnvelope<HermesEventListItem[]>> {
  const query = cleanParams(filters)
  return requestPaginated<HermesEventListItem[]>(`/api/admin/hermes/events${query ? `?${query}` : ''}`)
}

export async function createHermesMonitorLink(input: CreateHermesMonitorLinkRequest): Promise<HermesMonitorLink> {
  return request<HermesMonitorLink>('/api/admin/hermes/monitor-links', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function getHermesMonitorSnapshot(token: string): Promise<HermesMonitorSnapshot> {
  return request<HermesMonitorSnapshot>(`/api/public/hermes/monitor/${encodeURIComponent(token)}`)
}
