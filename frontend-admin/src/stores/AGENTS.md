<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/stores

## Purpose
Zustand stores for all admin domain async state. Each store calls `src/api/*` functions, tracks loading/error state, and exposes mutation actions consumed by pages and components. Stores are the primary state layer; TanStack Query is used only for dashboard read queries.

## Key Files
| File | Description |
|------|-------------|
| `authStore.ts` | Admin session state: `status` (`loading`/`authenticated`/`anonymous`), `user`, `bootstrap()`, `login()`, `logout()`, `markAnonymous()` |
| `adminAiStore.ts` | AI chat state: SSE streaming, tool calls, confirmations, chat history, Hermes mode, conversation persistence (localStorage + server) |
| `hermesReportStore.ts` | Hermes reports list, filters, pagination, detail open/close; race-condition-safe via `requestSeq` |
| `llmLogStore.ts` | LLM audit log list, filters, stats, pagination, detail open/close |
| `blogStore.ts` | Blog CMS list, editor/draft state, publish actions |
| `categoryStore.ts` | Category CRUD and tree/list state |
| `dashboardStore.ts` | Dashboard metrics (legacy; most reads moved to `queries/`) |
| `emailMarketingStore.ts` | Email templates, subscribers, email queue, marketing dashboard |
| `productStore.ts` | Product list, detail, create/update/delete, media attachment |
| `promoStore.ts` | Promo code CRUD and validation state |
| `roleStore.ts` | Role list and permissions |
| `userStore.ts` | User list, create/update/delete |

## For AI Agents
### Working In This Directory
- Stores follow `create<State>((set, get) => ({ ... }))` pattern.
- Error strings are always in Vietnamese.
- Keep store state serializable — no React nodes, classes, or functions except action methods.
- Paginated stores hold a `filters` object; mutations call `setFilters({ page: 1 })` to reset on filter change.
- Detail open/close pattern: `selectedId` + `selectedLog`/`selectedReport` + `loadingDetail`; close sets all to null/false.
- `adminAiStore` uses raw `fetch` with `ReadableStream` for SSE (not `request()`); handles `text`, `tool_call`, `tool_result`, `confirmation`, `conversation`, and `error` chunk types.
- After API shape changes: update `src/types/*`, then `src/api/*`, then the store — in that order.

### Common Patterns
- Mutations invalidate related TanStack Query keys via `invalidateAdminDashboardQueries()` when dashboard data is affected.
- `authStore.markAnonymous()` is called by `queryClient.ts` on 401/403 query errors.
- `adminAiStore` persists conversation history to both localStorage (offline fallback) and server (`/api/admin/ai/conversations`).

## Dependencies
### Internal
- `@/api/*` — all network calls
- `@/types/*` — state and action type shapes
- `@/lib/queryClient` — `clearAdminQueryCache()` in auth store

### External
- zustand 5

<!-- MANUAL: -->
