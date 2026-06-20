<!-- Parent: ../../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components/hermes

## Purpose
UI components for the Hermes AI agent monitoring feature. Hermes is a backend AI runner that processes events, generates reports, and emits live SSE updates. These components display Hermes report lists, filters, and full report details.

## Key Files
| File | Description |
|------|-------------|
| `HermesReportsPanel.tsx` | Top-level panel: composes filters + table + detail drawer + stats summary cards; reads `hermesReportStore` directly |
| `HermesReportTable.tsx` | Paginated table of `HermesReportListItem` rows; columns: title, type, severity, source, status, date |
| `HermesReportFilters.tsx` | Filter bar: severity, type, status, source, free-text search; controlled props `filters`/`onChange`/`onReset` |
| `HermesReportDetailDrawer.tsx` | Slide-over drawer showing full `HermesReportDetail`: summary, payload JSON, metadata |

## For AI Agents
### Working In This Directory
- `HermesReportsPanel` is the only component that reads `hermesReportStore` directly; child components receive data as props.
- Filter changes trigger a debounced `fetchReports()` via `useEffect` on `filters` in the panel.
- Severity badge colors: `critical` → red, `high` → orange, `warning` → yellow, `info` → blue.
- Report sources: `hermes_cron` (proactive/scheduled) vs `hermes_agent` (event-driven) — surface this distinction in the UI.
- The detail drawer shows `payloadJson` as formatted JSON; handle `null` gracefully.

### Common Patterns
- Pagination: `setFilters({ page: n })` on the store; `hasNextPage`/`hasPreviousPage` drive button disabled state.
- The panel uses `window.setTimeout(refresh, 250)` debounce on filter effects — preserve this pattern.
- `HermesReportDetailDrawer` uses the `Sheet` primitive from `@/components/ui/sheet`.

## Dependencies
### Internal
- `@/stores/hermesReportStore` — `useHermesReportStore`
- `@/types/hermes` — `HermesReportListItem`, `HermesReportDetail`, `HermesReportFilters`
- `@/components/ui/*` — Button, Sheet, Badge, Table primitives

### External
- lucide-react (FileText, filter icons)

<!-- MANUAL: -->
