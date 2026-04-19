<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# frontend

## Purpose
React 19 + TypeScript + Vite SPA for the Ao Dai Nha Uyen e-commerce platform. Uses framer-motion for animations, react-router-dom v7 for routing, CSS Modules with PostCSS for styling. No Tailwind -- raw CSS via PostCSS (nested, autoprefixer, cssnano).

## Key Files
| File | Description |
|------|-------------|
| `package.json` | Dependencies and scripts (bun as package manager) |
| `vite.config.ts` | Vite build configuration with react plugin, PostCSS, and envPrefix |
| `tsconfig.json` | TypeScript configuration |
| `eslint.config.js` | ESLint flat config |
| `postcss.config.js` | PostCSS with nested, autoprefixer, cssnano |
| `index.html` | HTML entry point |
| `.env.example` | Environment variable template |
| `bun.lock` | Bun lockfile -- prefer bun over npm |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `src/` | Application source code (see `src/AGENTS.md`) |
| `public/` | Static assets served at root (see `public/AGENTS.md`) |

## For AI Agents
### Working In This Directory
- Use **bun** as package manager (bun.lock exists): `bun install`, `bun run dev`
- `bun run dev` starts dev server on localhost:5173
- `bun run build` runs TypeScript checks (`tsc -b`) then Vite production build
- `bun run lint` runs ESLint flat config
- CSS Modules (*.module.css) for component styling -- no Tailwind
- Design tokens in `src/styles/variables.css` (burgundy, gold, cream palette)
- All UI text is in Vietnamese
- Vite env prefix: `VITE_` and `PUBLIC_`

### Testing Requirements
- No test framework configured yet
- Validate with `bun run lint` + `bun run build`
- Visual changes: validate in browser via Playwright MCP

### Common Patterns
- PascalCase component folders: `components/Header/Header.tsx` + `Header.module.css`
- Pages in `pages/<Name>/<Name>.tsx`
- API modules in `api/` with shared `client.ts` (fetch-based, not axios)
- Types in `types/` per domain
- framer-motion for all animations (reusable variants in `utils/motion.ts`)

## Dependencies
### External
- React 19.2, react-dom 19.2, react-router-dom 7.14
- framer-motion 12 (animations)
- Vite 8, TypeScript 6, PostCSS 8, CSSNano 7
- ESLint 9 with flat config
