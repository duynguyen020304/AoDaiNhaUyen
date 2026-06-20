<!-- Parent: ../../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components/llm-logs

## Purpose
UI components for the LLM audit log viewer. Displays a searchable, filterable table of AI request/response records with stats cards and a detail drawer for individual log entries.

## Key Files
| File | Description |
|------|-------------|
| `LlmLogTable.tsx` | Paginated table of `LlmAuditLogListItem` rows; columns: source, provider, model, operation, status, latency, tokens, cost, date |
| `LlmLogFilters.tsx` | Filter bar: date range, source, status, provider, model, operation, risk level, tool name, free-text; controlled props `filters`/`onChange`/`onReset` |
| `LlmLogStatsCards.tsx` | Summary stats row: total, success, failed, timeout, average latency, total tokens, estimated cost |
| `LlmLogDetailDrawer.tsx` | Slide-over drawer showing full `LlmAuditLogDetail`: redacted prompt/completion previews, token counts, safety flags, all metadata fields |

## For AI Agents
### Working In This Directory
- `LlmLogsPage` reads `llmLogStore` and passes data down as props; these components do not access the store directly.
- Redacted fields (`promptPreviewRedacted`, `completionPreviewRedacted`) may be `null` — display a placeholder, not an error.
- Risk level badge colors: `high`/`critical` → red, `medium` → yellow, `low` → green, null → gray.
- `safetyFlagsJson`, `inputMetadataJson`, `outputMetadataJson` are JSON strings — pretty-print with `JSON.stringify(JSON.parse(v), null, 2)` with null guard.
- Cost display: Vietnamese locale with VND suffix or USD depending on provider convention.

### Common Patterns
- Stats cards use the same layout as `dashboard/StatsCard` — consider reusing if shape matches.
- Detail drawer uses the `Sheet` primitive from `@/components/ui/sheet`.
- Pagination driven by `llmLogStore.setFilters({ page: n })` in `LlmLogsPage`.

## Dependencies
### Internal
- `@/types/llmLogs` — `LlmAuditLogListItem`, `LlmAuditLogDetail`, `LlmAuditLogFilters`, `LlmAuditLogStats`
- `@/components/ui/*` — Button, Sheet, Badge, Table, Input, Select primitives

### External
- lucide-react (filter, info icons)

<!-- MANUAL: -->
