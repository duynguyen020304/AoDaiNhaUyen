<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-07-14 -->

# frontend-admin/src

## Purpose
Admin SPA source. Routes, guarded shell, typed API clients, Zustand stores, Tailwind UI, dashboards, Blog CMS, marketing, AI chat/Hermes, LLM logs, and risk config.

## Key Files
| File | Description |
|------|-------------|
| `main.tsx` | StrictMode -> QueryClientProvider -> FeedbackProvider -> App; imports globals |
| `App.tsx` | Route tree, `AdminRoute`, `GuestRoute`, `AdminLayout` wiring |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `api/` | Fetch clients for auth/admin/blog/dashboard/email/inventory/llm/media |
| `auth/` | Route guards |
| `components/` | Layout, modals, dashboard widgets, AI chat, blog editor, LLM logs, UI primitives |
| `pages/` | Route-level admin screens |
| `stores/` | Zustand stores for auth/products/categories/users/roles/dashboard/blog/email/AI/logs/promos |
| `styles/` | Tailwind v4 globals/theme |
| `types/` | Admin/auth/blog/dashboard/AI/log DTO types |
| `lib/` | Utilities, query helpers, email preview helpers |

## Route Areas
| Area | Pages |
|------|-------|
| Auth | `LoginPage` |
| Core admin | Dashboard, products, categories, users, roles, media, orders, promos |
| Blog CMS | `BlogListPage`, `BlogFormPage` |
| Marketing | `MarketingDashboardPage`, `EmailTemplatesPage`, `SubscribersPage`, `EmailQueuePage` |
| AI/governance | `AiChatPage`, `LlmLogsPage`, `ToolRiskPage` |

## Local Conventions
- Tailwind only; no CSS Modules.
- Stores use `create<State>((set, get) => ({ ... }))` and Vietnamese `error` strings.
- Mutations generally refetch affected lists and invalidate dashboard queries when relevant.
- Use `feedback` provider/components for admin toasts/messages.
- Security/risk pages should keep explicit confirmations for destructive or sensitive actions.
