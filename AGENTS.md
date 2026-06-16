<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# AoDaiNhaUyen

## Purpose
Premium Vietnamese áo dài e-commerce platform. Full-stack app: ASP.NET Core 10 backend (clean architecture), React 19 customer SPA, React 19 admin SPA. Features product catalog, cart/checkout, AI try-on, AI stylist chat, Blog/SEO, email marketing, social auth (Google/Facebook/Zalo), admin catalog/order/user/role/media/AI controls.

## Key Files
| File | Description |
|------|-------------|
| `.gitignore` | Ignore rules for .NET, Node, env, agent/tool output |
| `AGENTS.md` | Root agent guidance |
| `.understand-anything/knowledge-graph.json` | Codebase knowledge graph if present |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `backend/` | ASP.NET Core 10 API with clean architecture (see `backend/AGENTS.md`) |
| `frontend/` | Customer React 19 + TypeScript + Vite SPA (see `frontend/AGENTS.md`) |
| `frontend-admin/` | Admin React 19 + Tailwind v4 + Zustand SPA (see `frontend-admin/AGENTS.md`) |
| `.github/` | GitHub Actions deploy workflow (see `.github/AGENTS.md`) |
| `public/` | Root static login assets served at `/` (see `public/AGENTS.md`) |

## Code Map
| Area | Where to look |
|------|---------------|
| API routes | `backend/AoDaiNhaUyen.Api/Controllers/` |
| API DI/config | `backend/AoDaiNhaUyen.Api/Configuration/ServiceRegistration.cs`, `Program.cs` |
| Domain schema | `backend/AoDaiNhaUyen.Domain/Entities/` |
| EF config/migrations | `backend/AoDaiNhaUyen.Infrastructure/Data/` |
| Backend business logic | `backend/AoDaiNhaUyen.Infrastructure/Services/` + `backend/AoDaiNhaUyen.Application/Services/` |
| Customer routes | `frontend/src/App.tsx`, `frontend/src/pages/` |
| Customer data hooks | `frontend/src/hooks/`, `frontend/src/lib/queryKeys.ts` |
| Customer API clients | `frontend/src/api/` |
| Admin routes | `frontend-admin/src/App.tsx`, `frontend-admin/src/pages/` |
| Admin state | `frontend-admin/src/stores/` |
| Admin API clients | `frontend-admin/src/api/` |

## Project Conventions
- UI language: Vietnamese. API messages: Vietnamese.
- API envelope: `{ success, message, data, errors, timestamp }`; avoid `Ok(new { ... })` in controllers.
- Backend dependency flow: Api -> Application + Infrastructure; Infrastructure -> Application + Domain; Domain standalone.
- Customer frontend uses CSS Modules + PostCSS; admin uses Tailwind v4. Do not share UI code between them.
- Use bun in both frontends; never switch to npm if `bun.lock` exists.
- TypeScript/CSS indentation: 2 spaces.
- Conventional commits: `feat(scope): description`.

## Commands
| Area | Command |
|------|---------|
| Backend build | `cd backend && dotnet build` |
| Backend tests | `cd backend/AoDaiNhaUyen.Tests && dotnet test` |
| Customer lint/build | `cd frontend && bun run lint && bun run build` |
| Customer SEO build | `cd frontend && bun run build:seo` |
| Admin lint/build | `cd frontend-admin && bun run lint && bun run build` |
| EF migration | `cd backend/AoDaiNhaUyen.Infrastructure && dotnet ef migrations add <Name> --startup-project ../AoDaiNhaUyen.Api` |

## Gotchas
- `spec.md` is absent/outdated in current tree; trust source + AGENTS.md, not old spec references.
- Backend test project is not in `AoDaiNhaUyen.slnx`; run tests from `backend/AoDaiNhaUyen.Tests/` explicitly.
- No root `.editorconfig`; C# formatting is mixed.
- No frontend test framework; validate UI changes with lint/build + browser/Playwright MCP.
- CI deploy workflow does not run full quality gates before deploy.
- CORS allows any header/method for configured origins and credentials; review before prod hardening.
- `ExceptionHandlingMiddleware` catches unhandled exceptions as 500; controllers map many expected errors manually.
- Root `public/` is separate from `frontend/public/`.

## Dependencies
- Backend: ASP.NET Core 10, EF Core 10, Npgsql/PostgreSQL, JWT Bearer, MailKit, FusionCache/Redis, AWS S3-compatible storage, DotNetEnv, Google Vertex/Gemini.
- Customer frontend: React 19, react-router-dom 7, TanStack Query + persist client, framer-motion, react-helmet-async, Vite 8, TypeScript 6.
- Admin frontend: React 19, Tailwind v4, Zustand 5, TanStack Query, Recharts, Zod, React Markdown.
- CI/CD: GitHub Actions, SSH/Cloudflare Tunnel, rsync, pm2/serve.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
