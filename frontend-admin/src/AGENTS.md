<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-06-08 -->

# frontend-admin/src

## Purpose
Admin SPA source. React 19 + TypeScript + Vite + Tailwind v4 + Zustand. Separate runtime from customer `frontend/src`.

## Key Files
| File | Description |
|------|-------------|
| `main.tsx` | React entry point |
| `App.tsx` | Route tree and admin shell wiring |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `api/` | Fetch clients for admin/auth/dashboard/inventory/media |
| `auth/` | `AdminRoute` and `GuestRoute` route guards |
| `components/` | Admin UI, shadcn-style primitives, AI chat widgets |
| `pages/` | Route pages: dashboard, products, categories, users, roles, media, AI, risk |
| `stores/` | Zustand stores per domain |
| `styles/` | Tailwind v4 global theme |
| `types/` | Admin domain DTO/types |
| `lib/` | Shared helpers (`utils`, class merging, small utilities) |

## For AI Agents
### Local Conventions
- Use `@/*` alias for src imports.
- Use Tailwind utilities; do not add CSS Modules here.
- Store async state in Zustand stores unless page is intentionally direct API (e.g. media patterns).
- UI text Vietnamese.
- Cookie auth uses backend session cookies; fetch with `credentials: 'include'`.

### Anti-Patterns
- Do not import components/styles from customer `frontend/`.
- Do not duplicate API base URL logic outside `api/client.ts`.
- Do not bypass `AdminRoute` for `/admin/*` pages.

### Commands
- `bun run dev` → localhost:5174
- `bun run lint`
- `bun run build`
