<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src

## Purpose
Admin SPA source root. Contains the route tree (`App.tsx`), app entry (`main.tsx`), and all feature subdirectories: API clients, route guards, UI components, pages, Zustand stores, query hooks, types, utilities, and global styles.

## Key Files
| File | Description |
|------|-------------|
| `main.tsx` | Entry: StrictMode → QueryClientProvider → FeedbackProvider → App; imports globals.css |
| `App.tsx` | Route tree, wires `AdminRoute`, `GuestRoute`, `AdminLayout` |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `api/` | Typed fetch clients for all backend domains |
| `auth/` | `AdminRoute` and `GuestRoute` guards |
| `components/` | Layout, modals, dashboard widgets, AI chat, blog editor, LLM logs, UI primitives |
| `pages/` | Route-level admin screens |
| `queries/` | TanStack Query option factories and custom hooks |
| `stores/` | Zustand stores for all domain state |
| `styles/` | Tailwind v4 globals and theme |
| `types/` | DTO and domain type definitions |
| `lib/` | `cn` helper, `queryClient`, email preview utilities |
| `assets/` | Static images (hero, logos) |

## Route Areas
| Area | Pages |
|------|-------|
| Auth | `LoginPage` |
| Core admin | Dashboard, Products, Categories, Orders, Promos, Media, Users, Roles |
| Blog CMS | `BlogListPage`, `BlogFormPage` |
| Email marketing | `MarketingDashboardPage`, `EmailTemplatesPage`, `SubscribersPage`, `EmailQueuePage` |
| AI/Hermes | `AiChatPage`, `HermesPage`, `HermesReportsPage`, `HermesMonitorPage`, `HermesLiveMonitorPage` |
| Governance | `ToolRiskPage`, `AiTryOnFeedbackPage` |
| Reviews | `ReviewsPage` |

## For AI Agents
### Working In This Directory
- `main.tsx` is the single provider stack — add providers here, not in `App.tsx`.
- `App.tsx` owns all route declarations; every protected route must nest inside `<AdminRoute>`.
- Tailwind only — no CSS Modules.

### Common Patterns
- Stores use `create<State>((set, get) => ({ ... }))` with Vietnamese `error` strings.
- Mutations generally refetch affected lists and call `invalidateAdminDashboardQueries()` when relevant.
- Use `useFeedback()` (from `FeedbackContext`) for toasts and confirmation dialogs.
- Destructive actions need explicit `confirm()` before executing.

## Dependencies
### Internal
- All subdirs depend on `@/types/*` for shared DTOs
- `@/api/client` is the base for all HTTP calls

### External
- react-router-dom 7 (route tree)
- TanStack Query 5 (`QueryClientProvider` in `main.tsx`)

<!-- MANUAL: -->
