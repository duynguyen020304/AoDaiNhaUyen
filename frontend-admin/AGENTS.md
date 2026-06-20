<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin

## Purpose
React 19 + TypeScript + Vite admin SPA. Separate from the customer `frontend/`. Uses Tailwind CSS v4, Zustand stores, TanStack Query v5, hand-rolled shadcn-style UI primitives, dashboards/charts, Blog CMS, email marketing, AI chat (generic + Hermes agent), and tool-risk governance.

## Key Files
| File | Description |
|------|-------------|
| `package.json` | Bun scripts and dependencies |
| `vite.config.ts` | React + Tailwind plugins, `@/*` alias, port 5174 |
| `tsconfig.app.json` | TS config; `@/*` alias; `erasableSyntaxOnly` |
| `eslint.config.js` | Flat ESLint config |
| `index.html` | SPA entry point |
| `src/styles/globals.css` | Tailwind v4 theme/colors |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `src/` | Admin app source (see `src/AGENTS.md`) |
| `src/api/` | Typed fetch clients for admin backend |
| `src/auth/` | `AdminRoute`, `GuestRoute` guards |
| `src/components/` | Layout, modals, dashboard widgets, AI chat, blog editor, LLM logs, UI primitives |
| `src/pages/` | Route-level admin screens |
| `src/queries/` | TanStack Query hooks and query keys |
| `src/stores/` | Zustand domain stores |
| `src/styles/` | Tailwind v4 global theme |
| `src/types/` | Admin DTO/type definitions |
| `src/lib/` | Shared helpers, query client, email preview |

## Key Differences from Customer SPA
| Aspect | frontend-admin | frontend |
|--------|----------------|----------|
| Styling | Tailwind CSS v4 | CSS Modules + PostCSS |
| State | Zustand + TanStack Query | TanStack Query + Context |
| Port | 5174 | 5173 |
| UI kit | Local shadcn-style primitives | Custom CSS Module components |
| Features | Admin CRUD, Blog CMS, email marketing, AI governance | Customer shopping/content/AI try-on |

## Commands
| Command | Description |
|---------|-------------|
| `bun run dev` | Dev server on localhost:5174 |
| `bun run lint` | ESLint |
| `bun run build` | `tsc -b` + Vite build |
| `bun run preview` | Preview build |

## For AI Agents
### Working In This Directory
- Always use `@/*` imports (maps to `src/`).
- All `/admin/*` routes must be wrapped by `AdminRoute` + `AdminLayout` in `App.tsx`.
- Cookie-based auth; shared API client uses `credentials: 'include'`.
- Zustand stores own async state unless a page intentionally uses TanStack Query hooks.
- Vietnamese strings for admin labels, errors, and confirmations.
- Do not import from customer `frontend/`.

### Common Patterns
- New pages: add route in `App.tsx`, create file in `src/pages/`, wire to a store or query hook.
- New API: add to `src/api/`, types to `src/types/`, then call from store or page.
- Destructive actions require `useFeedback().confirm(...)` before executing.

## Dependencies
### Internal
- Depends on backend at `VITE_API_BASE_URL` (default `http://localhost:5043`)

### External
- React 19, react-router-dom 7, Zustand 5, TanStack Query 5
- Tailwind v4, `@tailwindcss/vite`, class-variance-authority, clsx, tailwind-merge, tw-animate-css
- Recharts, lucide-react, Zod, react-markdown, remark-gfm

<!-- MANUAL: -->
