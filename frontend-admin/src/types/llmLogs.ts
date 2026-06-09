export interface LlmAuditLogListItem {
  id: string
  requestId: string
  correlationId: string
  traceId: string | null
  actorUserId: string | null
  actorRole: string | null
  source: string
  provider: string
  model: string | null
  operation: string
  toolName: string | null
  riskLevel: string | null
  requiresConfirmation: boolean
  status: string
  errorCode: string | null
  latencyMs: number | null
  totalTokens: number | null
  estimatedCost: number | null
  createdAt: string
  promptPreviewRedacted: string | null
  completionPreviewRedacted: string | null
}

export interface LlmAuditLogDetail extends LlmAuditLogListItem {
  conversationId: string | null
  threadId: string | null
  messageId: string | null
  adminActionId: string | null
  userGeneratedImageId: string | null
  ipHash: string | null
  userAgentHash: string | null
  actionType: string | null
  approvedByUserId: string | null
  approvedAt: string | null
  startedAt: string
  completedAt: string | null
  promptTokens: number | null
  completionTokens: number | null
  inputMetadataJson: string | null
  outputMetadataJson: string | null
  safetyFlagsJson: string | null
  redactionVersion: string
  retainUntil: string
}

export interface LlmAuditLogStats {
  total: number
  success: number
  failed: number
  timeout: number
  averageLatencyMs: number
  totalTokens: number
  estimatedCost: number
}

export interface LlmAuditLogFilters {
  page: number
  pageSize: number
  from?: string
  to?: string
  source?: string
  status?: string
  provider?: string
  model?: string
  operation?: string
  riskLevel?: string
  toolName?: string
  actorUserId?: string
  threadId?: string
  conversationId?: string
  requestId?: string
  q?: string
  sortBy: string
  sortDir: 'asc' | 'desc'
}
