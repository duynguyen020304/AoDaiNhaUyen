<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-18 | Updated: 2026-07-14 -->

# frontend-admin

## Purpose
React 19 + TypeScript 6 + Vite 8 admin SPA. Separate from customer `frontend/`. Uses Tailwind CSS v4, Zustand stores, TanStack Query, hand-rolled shadcn-style primitives, dashboards/charts, Blog CMS, email marketing, AI/Hermes/admin audit tooling.

## Key Files
| File | Description |
|------|-------------|
| `package.json` | Bun scripts/deps |
| `vite.config.ts` | React + Tailwind plugins, `@/*` alias, port 5174 |
| `tsconfig.app.json` | TS config; `@/*` alias; permissive unused settings; `erasableSyntaxOnly` |
| `eslint.config.js` | Flat ESLint config |
| `src/styles/globals.css` | Tailwind v4 theme/colors |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `src/` | Admin app source (see `src/AGENTS.md`) |
| `src/api/` | Admin API clients |
| `src/auth/` | `AdminRoute`, `GuestRoute` |
| `src/components/` | Admin layout, tables/forms/modals, AI/blog/dashboard/log UI, primitives |
| `src/pages/` | Admin route pages |
| `src/stores/` | Zustand domain stores |
| `src/styles/` | Tailwind global theme |
| `src/types/` | Admin DTO/type definitions |
| `src/lib/` | Shared helpers, query helpers, email preview/utils |

## Key Differences from Customer SPA
| Aspect | frontend-admin | frontend |
|--------|----------------|----------|
| Styling | Tailwind CSS v4 | CSS Modules + PostCSS |
| State | Zustand + some TanStack Query | TanStack Query hooks + Context |
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

## Local Conventions
- Use `@/*` imports.
- All `/admin/*` routes must be wrapped by `AdminRoute` + `AdminLayout`.
- Cookie auth; shared API client uses `credentials: 'include'`.
- Zustand stores own async state unless page intentionally uses TanStack Query/direct API.
- Vietnamese admin labels/errors/confirmations.
- Do not import from customer `frontend/`.

## Dependencies
- React 19, react-router-dom 7, Zustand 5, TanStack Query 5.
- Tailwind v4, `@tailwindcss/vite`, class-variance-authority, clsx, tailwind-merge, tw-animate-css.
- Recharts, lucide-react, Zod, react-markdown, remark-gfm.
