<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-18 | Updated: 2026-06-18 -->

# frontend-admin

## Purpose
React 19 + TypeScript 6 + Vite 8 admin panel SPA for Ao Dai Nha Uyen. Uses Tailwind CSS v4, Zustand 5 for state, hand-rolled shadcn-style UI primitives. Separate from customer-facing `frontend/` — different stack, port, and deployment target.

## Key Files
| File | Description |
|------|-------------|
| `package.json` | Dependencies + scripts (bun package manager) |
| `vite.config.ts` | Vite config with react + tailwindcss plugins, port 5174 |
| `tsconfig.app.json` | TS config — `@/*` alias → `./src/*`, `noUnusedLocals: false` |
| `eslint.config.js` | ESLint flat config |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `src/` | Admin app source and local guidance (see `src/AGENTS.md`) |
| `src/api/` | Fetch-based API client (same pattern as `frontend/`) |
| `src/auth/` | AdminRoute (admin role gate) + GuestRoute |
| `src/components/` | Admin components + hand-rolled shadcn-style primitives (see `src/components/AGENTS.md`) |
| `src/pages/` | Route pages: dashboard, products, categories, users, roles, media, AI/risk (see `src/pages/AGENTS.md`) |
| `src/stores/` | Zustand stores per domain (see `src/stores/AGENTS.md`) |
| `src/styles/` | `globals.css` with Tailwind theme (burgundy/gold palette) |
| `src/types/` | TypeScript domain types |

## For AI Agents

### Key Differences from `frontend/` (customer SPA)
| Aspect | frontend-admin | frontend |
|--------|---------------|----------|
| CSS | **Tailwind CSS v4** | CSS Modules + PostCSS |
| State | **Zustand 5** (direct fetch in actions) | React Context (no Zustand) |
| UI lib | Hand-rolled shadcn-style | Custom CSS Module components |
| Port | **5174** | 5173 |
| Tests | None | None |
| framer-motion | No | Yes |

### Working In This Directory
- Use **bun**: `bun install`, `bun run dev`, `bun run build`, `bun run lint`
- `bun run dev` starts on `localhost:5174`
- All routes under `/admin/*` protected by `AdminRoute` (checks admin role)
- `/login` protected by `GuestRoute` (redirects to `/admin/products` if authenticated admin)
- API base URL: `VITE_API_BASE_URL` env var (defaults to `http://localhost:5043`)
- Cookie-based auth (same backend, `credentials: 'include'`)
- UI text in Vietnamese
- 2-space indentation

### Store Pattern
- Zustand `create<State>((set, get) => ({...}))`
- Each store manages own `loading`/`error` state
- Mutations refetch list after success
- Error messages in Vietnamese
- MediaPage uses direct API calls (no dedicated store)

### Commands
| Command | Description |
|---------|-------------|
| `bun run dev` | Dev server on port 5174 |
| `bun run build` | Type-check (`tsc -b`) + Vite production build |
| `bun run lint` | ESLint check |

### Gotchas
- No test framework configured
- `noUnusedLocals: false` + `noUnusedParameters: false` — permissive TS
- No shared UI components with customer `frontend/` — separate codebases
- `tsconfig.app.json` uses `erasableSyntaxOnly: true` (no const enums, no namespaces with runtime code)
