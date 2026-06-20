<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/api

## Purpose
Typed fetch clients for all admin backend endpoints. `client.ts` provides the shared HTTP foundation with cookie auth, 401 refresh retry, envelope parsing, and `HttpError`. All other modules are domain-specific wrappers that call `request()` or `requestPaginated()`.

## Key Files
| File | Description |
|------|-------------|
| `client.ts` | `API_BASE_URL`, `HttpError`, `request<T>()`, `requestPaginated<T>()`, 401 refresh retry |
| `auth.ts` | Admin login, current user, session refresh, logout |
| `admin.ts` | Products, categories, users, roles, promos, and general admin CRUD |
| `dashboard.ts` | Dashboard stats, revenue, orders-by-status, recent orders, top products, user growth |
| `inventory.ts` | Stock and inventory endpoints |
| `media.ts` | Admin media upload, list, delete |
| `blog.ts` | Blog CMS list, create, update, publish |
| `emailMarketing.ts` | Email templates, subscribers, email jobs, marketing dashboard |
| `hermes.ts` | Hermes reports, events, SSE feed URL, monitor links, monitor snapshots |

## For AI Agents
### Working In This Directory
- Always use `request<T>()` for single-resource responses; `requestPaginated<T>()` for paginated lists.
- For file uploads use raw `fetch` with `FormData` — `client.ts` correctly skips `Content-Type` for `FormData`.
- Backend error messages are in Vietnamese; surface them via `HttpError.message`.
- Add endpoints here first, then call via stores or React Query hooks in pages.
- Keep request/response types aligned with `src/types/*` and backend Application DTOs.
- Admin-only routes live under `/api/admin/*`; public routes under `/api/public/*`.
- `HERMES_FEED_SSE_URL` in `hermes.ts` is a raw URL string for EventSource (not a `request()` call).

### Common Patterns
- Paginated filter params are cleaned with a `cleanParams()` helper (strip nulls/empty strings).
- 401 on non-auth endpoints triggers a single shared refresh via `refreshSession()`; requests dedup via `refreshSessionPromise`.
- `HttpError` carries `.status`, `.errors` (ApiError[]), and `.requestInfo` for debugging.

## Dependencies
### Internal
- `@/types/api` — `ApiEnvelope`, `PaginatedApiEnvelope`, `ApiError`
- `@/types/*` — domain-specific DTOs used in function signatures

### External
- Native `fetch` only; no third-party HTTP library

<!-- MANUAL: -->
