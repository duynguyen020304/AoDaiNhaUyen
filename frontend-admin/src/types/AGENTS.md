<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/types

## Purpose
TypeScript type definitions for all admin domain DTOs, API envelope shapes, and store state interfaces. These types are the contract between the backend API responses, the API client layer, and the frontend stores/components.

## Key Files
| File | Description |
|------|-------------|
| `api.ts` | `ApiError`, `ApiEnvelope<T>`, `PaginatedApiEnvelope<T>` — base response envelope shapes used by `client.ts` |
| `auth.ts` | `AuthUser` (id, email, roles[]), `AuthStatus` (`'loading' \| 'authenticated' \| 'anonymous'`) |
| `admin.ts` | Product, Category, User, Role, Order, Promo, Media, Inventory DTOs |
| `blog.ts` | Blog post, block content types, `AiBlogDraft`, `AI_BLOG_DRAFT_STORAGE_KEY` |
| `dashboard.ts` | Dashboard summary, revenue series, orders-by-status, top products, user growth DTOs; `DashboardPeriod` |
| `ai.ts` | `AiMessage`, `AiToolCall`, `AiToolResultMeta`, `AiPendingAction`, `AiSuggestion`, `AiChatRequest`, `AdminChatMode`, `HermesStatus`, conversation types |
| `hermes.ts` | `HermesReportListItem`, `HermesReportDetail`, `HermesReportFilters`, `HermesEventListItem`, `HermesEventFilters`, monitor link/snapshot types, feed types |

## For AI Agents
### Working In This Directory
- Types must stay aligned with backend Application DTOs — when the backend changes a field, update here first, then the API client, then the store.
- `PaginatedApiEnvelope<T>` extends `ApiEnvelope<T>` with `hasNextPage`, `hasPreviousPage`, `totalPage`, `totalItem`.
- `T` in `PaginatedApiEnvelope<T[]>` is always an array type (e.g. `HermesReportListItem[]`).
- `AuthStatus` drives the entire auth guard logic in `src/auth/` — do not add new values without updating `AdminRoute` and `GuestRoute`.
- `AiBlogDraft` and `AI_BLOG_DRAFT_STORAGE_KEY` live in `blog.ts` because the draft is a blog artifact, even though AI generates it.
- Filter types (e.g. `HermesReportFilters`) include `page` and `pageSize` as required fields — stores initialize them and merge partials via `setFilters()`.

### Common Patterns
- New domain: create `<domain>.ts` here, export all DTOs, import in `src/api/<domain>.ts` and `src/stores/<domain>Store.ts`.
- Optional filter fields use `?:` (not `| undefined` union) to match backend query param behavior.
- Avoid `any` — use `unknown` for untyped JSON fields (e.g. `payloadJson: string | null`, parse at use site).

## Dependencies
### Internal
- `blog.ts` imports from `ai.ts` (`AiBlogDraft` reference in `ai.ts` imports back from `blog.ts` — be aware of this cross-import)

### External
- TypeScript only; no runtime dependencies

<!-- MANUAL: -->
