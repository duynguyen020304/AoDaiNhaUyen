import { request, requestPaginated } from '@/api/client'
import type { PaginatedApiEnvelope } from '@/types/api'
import type { LlmAuditLogDetail, LlmAuditLogFilters, LlmAuditLogListItem, LlmAuditLogStats } from '@/types/llmLogs'

function cleanParams(filters: Partial<LlmAuditLogFilters>) {
  const params = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value))
  })
  return params.toString()
}

export async function getLlmLogs(filters: Partial<LlmAuditLogFilters>): Promise<PaginatedApiEnvelope<LlmAuditLogListItem[]>> {
  const query = cleanParams(filters)
  return requestPaginated<LlmAuditLogListItem[]>(`/api/admin/llm-logs${query ? `?${query}` : ''}`)
}

export async function getLlmLog(id: string): Promise<LlmAuditLogDetail> {
  return request<LlmAuditLogDetail>(`/api/admin/llm-logs/${id}`)
}

export async function getLlmLogStats(filters: Partial<LlmAuditLogFilters>): Promise<LlmAuditLogStats> {
  const query = cleanParams(filters)
  return request<LlmAuditLogStats>(`/api/admin/llm-logs/stats${query ? `?${query}` : ''}`)
}
