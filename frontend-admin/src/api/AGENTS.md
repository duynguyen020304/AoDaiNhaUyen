<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-06-08 -->

# frontend-admin/src/api

## Purpose
Fetch-based admin API clients. Wrap backend REST endpoints with typed req/res helpers.

## Where To Look
| File/Area | Use |
|-----------|-----|
| `client.ts` | Base URL, credentials, envelope parsing, errors |
| `auth*` | Admin login/session/logout |
| `admin*` / `inventory*` | Product/category/admin catalog operations |
| `dashboard*` | Dashboard metrics reads |
| `media*` | Upload/list/delete media assets |

## Local Conventions
- Always use shared client helpers; never raw `fetch` in new API modules unless adding client primitive.
- Include cookies: `credentials: 'include'` via shared client.
- Preserve backend envelope shape: `{ success, message, data, errors, timestamp }`.
- Throw/display Vietnamese messages from API when present.
- Keep DTO names aligned with `src/types/` and backend Application DTOs.

## Gotchas
- Admin API base URL from `VITE_API_BASE_URL`, default `http://localhost:5043`.
- Some mutations must refresh Zustand store lists after success.
