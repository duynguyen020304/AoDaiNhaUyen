<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend

## Purpose
Customer React 19 + TypeScript + Vite SPA for áo dài e-commerce. Uses react-router-dom v7, TanStack Query with persistence, framer-motion, react-helmet-async SEO, CSS Modules + PostCSS. No Tailwind here.

## Key Files
| File | Description |
|------|-------------|
| `package.json` | Scripts/dependencies; bun package manager |
| `vite.config.ts` | React plugin, service-worker no-store middleware, env prefixes, build config |
| `tsconfig.json` / `tsconfig.app.json` | TypeScript config |
| `eslint.config.js` | ESLint flat config |
| `postcss.config.js` | nested/autoprefixer/cssnano |
| `index.html` | HTML entry |
| `.env.example` | Env template |
| `bun.lock` | Canonical lockfile |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `src/` | App source (see `src/AGENTS.md`) |
| `public/` | Vite static assets (separate from root `public/`) |
| `scripts/` | SEO/prerender helper scripts |

## Commands
| Command | Description |
|---------|-------------|
| `bun run dev` | Vite dev server on localhost:5173 |
| `bun run lint` | ESLint |
| `bun run build` | `tsc -b` + Vite prod build |
| `bun run build:seo` | Build then run `scripts/pre-render.mjs` |
| `bun run preview` | Preview built app |

## Local Conventions
- CSS Modules only for components/pages; use `src/styles/variables.css` tokens.
- UI text Vietnamese.
- API modules use shared fetch client; no axios.
- Server state via TanStack Query hooks in `src/hooks/`; persisted query client in `src/lib/`.
- SEO/head/meta via `react-helmet-async` and `components/Seo`.
- Framer-motion variants live in `src/utils/motion.ts`.

## Gotchas
- Both `bun.lock` and `package-lock.json` may exist; bun is canonical.
- No test framework configured; validate with lint/build and visual/browser checks.
- Customer frontend differs from admin: no Tailwind, no Zustand.
- Service worker cache logic exists; avoid stale `/sw.js` behavior when touching PWA/cache code.

## Dependencies
- React 19, react-dom 19, react-router-dom 7.
- TanStack Query v5 + query persist client.
- framer-motion, react-helmet-async, FontAwesome.
- Vite 8, TypeScript 6, PostCSS/CSSNano, ESLint flat config.
