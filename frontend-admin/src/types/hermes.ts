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
  source?: string
  q?: string
}

export interface HermesEventListItem {
  id: string
  eventType: string
  aggregateType: string
  aggregateId: string
  status: string
  attempts: number
  maxAttempts: number
  lastError: string | null
  correlationId: string | null
  idempotencyKey: string | null
  occurredAt: string
  scheduledAt: string
  processedAt: string | null
  createdAt: string
}

export interface HermesEventFilters {
  page: number
  pageSize: number
  status?: string
  eventType?: string
  aggregateType?: string
  q?: string
}

export interface CreateHermesMonitorLinkRequest {
  scopeType: 'event'
  scopeId: string
  expiresInHours?: number
}

export interface HermesMonitorLink {
  id: string
  url: string
  token: string
  scopeType: string
  scopeId: string
  expiresAt: string
  revokedAt: string | null
  accessCount: number
  createdAt: string
}

export interface HermesFeedItem {
  eventId: string
  storeMessage: string
  storeTime: string
  eventType: string
  eventStatus: string
  hermesMessages: HermesFeedHermesMessage[]
  runStatus: string | null
}

export interface HermesFeedHermesMessage {
  kind: 'thinking' | 'step' | 'report' | 'error' | string
  title: string | null
  summary: string
  time: string
  status: string | null
  severity: string | null
}

export interface HermesFeedSnapshot {
  items: HermesFeedItem[]
  heartbeat: HermesFeedHeartbeat | null
  generatedAt: string
}

export interface HermesFeedHeartbeat {
  runnerName: string
  status: string
  activeJobs: number
  recordedAt: string
}

export interface HermesMonitorSnapshot {
  link: {
    id: string
    scopeType: string
    scopeId: string
    expiresAt: string
    revokedAt: string | null
    lastAccessedAt: string | null
    accessCount: number
  }
  event: {
    id: string
    eventType: string
    aggregateType: string
    aggregateId: string
    status: string
    attempts: number
    maxAttempts: number
    safeError: string | null
    correlationId: string | null
    occurredAt: string
    scheduledAt: string
    processedAt: string | null
    createdAt: string
  }
  runs: Array<{
    id: string
    status: string
    trigger: string
    promptSummary: string
    resultSummary: string | null
    safeError: string | null
    startedAt: string
    completedAt: string | null
  }>
  traceSteps: Array<{
    id: string
    runId: string | null
    eventOutboxId: string | null
    kind: string
    title: string
    summary: string
    status: string
    startedAt: string
    completedAt: string | null
    durationMs: number | null
    safePayloadJson: string | null
    safeError: string | null
  }>
  reports: Array<{
    id: string
    reportType: string
    severity: string
    title: string
    summary: string
    source: string
    correlationId: string | null
    runId: string | null
    status: string
    createdAt: string
  }>
  heartbeat: {
    runnerName: string
    status: string
    model: string | null
    gatewayStatus: string | null
    activeJobs: number
    safeLastError: string | null
    recordedAt: string
  } | null
  thinkingSummary: string
  generatedAt: string
}
