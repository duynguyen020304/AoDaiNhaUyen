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

## Repository Structure
| Directory | Purpose |
|-----------|---------|
| `backend/` | ASP.NET Core 10 API with clean architecture (see `backend/AGENTS.md`) |
| `frontend/` | Customer React 19 + TypeScript + Vite SPA (see `frontend/AGENTS.md`) |
| `frontend-admin/` | Admin React 19 + Tailwind v4 + Zustand SPA (see `frontend-admin/AGENTS.md`) |
| `.github/` | GitHub Actions deploy workflow (see `.github/AGENTS.md`) |
| `public/` | Root static login assets served at `/` (see `public/AGENTS.md`) |

## Tech Stack
| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10, EF Core 10, Npgsql/PostgreSQL, JWT Bearer (HttpOnly cookie), FusionCache + Redis L2, MailKit, AWS S3-compatible storage, DotNetEnv, Google Vertex/Gemini |
| Customer frontend | React 19, react-router-dom 7, TanStack Query + persist, CSS Modules + PostCSS, framer-motion, react-helmet-async, Vite 8, TypeScript 6, Bun |
| Admin frontend | React 19, Tailwind v4, Zustand 5, TanStack Query, Recharts, Zod, React Markdown, Vite, Bun |
| CI/CD | GitHub Actions, Infisical secrets, SSH/Cloudflare Tunnel, rsync, pm2/serve |

## For AI Agents

### Quick Start
| Task | Command |
|------|---------|
| Backend build | `cd backend && dotnet build` |
| Backend tests | `cd backend/AoDaiNhaUyen.Tests && dotnet test` |
| Customer lint/build | `cd frontend && bun run lint && bun run build` |
| Customer SEO build | `cd frontend && bun run build:seo` |
| Admin lint/build | `cd frontend-admin && bun run lint && bun run build` |
| EF migration | `cd backend/AoDaiNhaUyen.Infrastructure && dotnet ef migrations add <Name> --startup-project ../AoDaiNhaUyen.Api` |

### Architecture Overview
- Dependency flow: Api -> Application + Infrastructure; Infrastructure -> Application + Domain; Domain standalone.
- API envelope: `{ success, message, data, errors, timestamp }` via `ApiResponseFactory`. Never use raw `Ok(new { ... })`.
- Auth: HttpOnly cookies (access token + refresh token); JWT bearer reads cookie in auth event.
- Customer SPA uses CSS Modules; admin SPA uses Tailwind v4. Do not share UI code between them.
- Backend test project (`AoDaiNhaUyen.Tests`) is excluded from `.slnx`; run `dotnet test` from its directory.

### Code Map
| Area | Where to look |
|------|---------------|
| API routes | `backend/AoDaiNhaUyen.Api/Controllers/` |
| API DI/config | `backend/AoDaiNhaUyen.Api/Configuration/ServiceRegistration.cs`, `Program.cs` |
| Domain entities | `backend/AoDaiNhaUyen.Domain/Entities/` |
| EF config/migrations | `backend/AoDaiNhaUyen.Infrastructure/Data/` |
| Service contracts | `backend/AoDaiNhaUyen.Application/Interfaces/Services/` |
| Service impls | `backend/AoDaiNhaUyen.Infrastructure/Services/` + `backend/AoDaiNhaUyen.Application/Services/` |
| Customer routes | `frontend/src/App.tsx`, `frontend/src/pages/` |
| Customer data hooks | `frontend/src/hooks/`, `frontend/src/lib/queryKeys.ts` |
| Customer API clients | `frontend/src/api/` |
| Admin routes | `frontend-admin/src/App.tsx`, `frontend-admin/src/pages/` |
| Admin state | `frontend-admin/src/stores/` |
| Admin API clients | `frontend-admin/src/api/` |

### Key Conventions
- UI language: Vietnamese. API response messages: Vietnamese.
- Use Bun in both frontends; never switch to npm if `bun.lock` exists.
- TypeScript/CSS indentation: 2 spaces.
- Conventional commits: `feat(scope): description`.
- `spec.md` is absent/outdated; trust source + AGENTS.md.
- No root `.editorconfig`; C# formatting is mixed.
- No frontend test framework; validate UI changes with lint/build + browser/Playwright MCP.
- CI deploy workflow may not run full quality gates before deploy.
- CORS allows any header/method for configured origins with credentials; review before prod hardening.
- `ExceptionHandlingMiddleware` catches unhandled exceptions as 500; controllers map expected errors manually.
- Root `public/` is separate from `frontend/public/`.
- CI deploy workflow fetches secrets from Infisical (development environment only); never commits real secrets.

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->

## Manual Notes
- Facebook Page management lives under `backend/AoDaiNhaUyen.Api/Controllers/FacebookController.cs`, `backend/AoDaiNhaUyen.Infrastructure/Services/FacebookService.cs`, and `frontend-admin/src/pages/FacebookPage.tsx`. Page Access Tokens must stay encrypted via DataProtection, only expose `TokenLast4`, never log raw tokens.
- Facebook Messenger → AI virtual try-on (Hermes-driven): when a customer sends a photo on Messenger, `ZernioService.UpsertMessagesAsync` downloads the inbound image via `IFacebookService.DownloadAttachmentBytesAsync` (Page Access Token) and persists it to S3 under `private/social-inbox/{conversationId}`; the object key is stored on `SocialInboxMessage.StoredImageKey` (+ `StoredImageMimeType`). The Hermes agent (prompted via `HermesEventProcessor.BuildInput`/`BuildBatchInput`) orchestrates the try-on SOP: ask the customer to choose a try-on-eligible garment from `GET /api/admin/ai-tryon/catalog`, then `GET /api/admin/social/messages/{messageId}/image` (1h presigned URL) + `POST /api/admin/ai-tryon/generate` (reuses `ICatalogTryOnService.CreateAsync` → Gemini virtual-try-on), then replies via the existing `POST /api/admin/social/conversations/{conversationId}/messages` with `attachmentType=image`. Kill switch: `SocialInboxSync:DownloadInboundImages` (default true). Migration `20260623194052_AddSocialInboxMessageStoredImage`. No PSID→customer mapping yet; try-on runs unattributed (`UserId`/`GuestKeyHash` null), mirroring the chat path. The test project (`AoDaiNhaUyen.Tests`) now builds; this feature added 10 offline tests covering all 9 success criteria without live Facebook/Hermes/Gemini credentials: `AdminAiTryOnControllerTests` (#7 success, #9 error fallbacks), `ZernioInboundImageTests` (#2 image persistence + #9 non-fatal failure), and `HermesEventProcessorTryOnPromptTests` (#3–#6, #8 — asserts via reflection that `BuildInput`/`BuildBatchInput` for a `social_message_received` event embed the full try-on SOP: `GET /api/admin/ai-tryon/catalog`, `GET /api/admin/social/messages/{id}/image`, `POST /api/admin/ai-tryon/generate`, and the `attachmentType=image` reply instruction). Pre-existing unrelated test issues remain: 1 failing test in `HermesBatchProcessorTests` (fan-out/fan-in `partial` vs `completed` status drift from commit 6b1fe25, not touched by this feature), and this session also repaired pre-existing local compile breaks in `AdminAgentService.cs` (missing `SafeText`/`StripUnsafe` helpers from an in-progress blog-coordinator refactor) plus the matching `AdminAiSecurityTests`/`HermesFeedServiceTests` test helpers.
