<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# AoDaiNhaUyen

## Purpose
Premium Vietnamese áo dài e-commerce platform. Full-stack application with an ASP.NET Core 10 backend (clean architecture) and a React 19 + TypeScript + Vite frontend. Features AI try-on, chat widget, cart/checkout, social auth (Google/Facebook), and product catalog management.

## Key Files
| File | Description |
|------|-------------|
| `spec.md` | MVP specification with pages, routes, design system, and component inventory |
| `.gitignore` | Git ignore rules |
| `AGENTS.md` | This file — AI-readable project documentation |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `backend/` | ASP.NET Core 10 API with clean architecture (see `backend/AGENTS.md`) |
| `frontend/` | React 19 + TypeScript + Vite SPA (see `frontend/AGENTS.md`) |
| `.github/` | GitHub Actions CI/CD workflows (see `.github/AGENTS.md`) |
| `public/` | Root-level static assets served at `/` (see `public/AGENTS.md`) |

## For AI Agents

### Working In This Directory
- Monorepo structure: `backend/` (.NET) and `frontend/` (React) are independent
- Backend uses .NET 10 with clean architecture (Api → Application → Domain → Infrastructure)
- Frontend uses Vite with bun as package manager; **never switch to npm if bun.lock exists**
- All UI language is Vietnamese; API messages are in Vietnamese
- API responses must use the standard envelope: `{ success, message, data, errors, timestamp }`
- Never return raw anonymous objects from controllers (no `Ok(new { data })`)

### Testing Requirements
- Frontend: `npm run lint` + `npm run build` + visual validation via Playwright MCP for UI changes
- Backend: `dotnet test` from `backend/`
- For persisted data/backend changes, validate with `psql` against the PostgreSQL database

### Common Patterns
- Frontend components: PascalCase folders under `src/components/<Name>/`, paired with CSS Modules
- Design tokens in `src/styles/variables.css` — never duplicate constants
- Two-space indentation for TypeScript and CSS
- Conventional Commit messages with optional scope: `feat(section): description`

### Commit Guidelines
- Short imperative messages, optionally with Conventional Commit scope
- Only commit complete, validated work
- Review diff before committing; avoid unrelated changes
- Screenshots/recordings for visual PR changes

## Dependencies

### External
- **Backend**: ASP.NET Core 10, EF Core 10, JWT Bearer, MailKit, DotNetEnv
- **Frontend**: React 19, react-router-dom 7, framer-motion, Vite 8, TypeScript 6
- **Database**: PostgreSQL
- **CI/CD**: GitHub Actions with SSH/Cloudflare Tunnel deployment

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
