<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-07-14 -->

# frontend-admin/src/api

## Purpose
Typed fetch clients for admin backend endpoints. Shared client handles base URL, cookies, envelope parsing, and errors.

## Module Map
| File | Purpose |
|------|---------|
| `client.ts` | `VITE_API_BASE_URL` fallback, credentials, envelope/pagination helpers |
| `auth.ts` | Admin login/session/logout |
| `admin.ts` | Products/categories/users/roles/promos/admin CRUD helpers |
| `dashboard.ts` | Dashboard stats/charts |
| `inventory.ts` | Stock/inventory endpoints |
| `media.ts` | Admin media upload/list/delete |
| `blog.ts` | Blog CMS list/create/update/publish helpers |
| `emailMarketing.ts` | Templates, subscribers, email jobs, marketing dashboard |
| `llmLogs.ts` | LLM audit logs filters/detail/stats |

## Local Conventions
- Add endpoints here first, then call via stores/pages.
- Keep request/response types aligned with `src/types/*` and backend Application DTOs.
- Use shared client; avoid raw `fetch` except for deliberate upload/client primitive cases.
- Surface backend Vietnamese message text where possible.
- File uploads should use `FormData`; let browser set multipart boundary.
- Paginated endpoints should keep page/pageSize/filter names aligned with backend DTOs.
- Keep admin-only endpoints under `/api/admin/*` unless backend route is intentionally shared.
