<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/pages

## Purpose
Route-level admin screens. Each page is responsible for composing stores, query hooks, tables, charts, forms, modals, and `AdminLayout` slots into a complete user-facing view.

## Key Files
| File | Description |
|------|-------------|
| `LoginPage.tsx` | Admin login form; uses `authStore.login()` |
| `DashboardPage.tsx` | Main dashboard: stats cards, revenue/user-growth charts, recent orders, low-stock alerts |
| `ProductListPage.tsx` | Paginated product table with search/filter |
| `ProductFormPage.tsx` | Product create/edit form with image upload |
| `CategoriesPage.tsx` | Category CRUD with `CategoryFormModal` |
| `OrdersPage.tsx` | Order list and status management |
| `PromosPage.tsx` | Promo code CRUD with `PromoFormModal` |
| `MediaPage.tsx` | Media library: upload, list, delete |
| `UsersPage.tsx` | User management with `UserFormModal` |
| `RolesPage.tsx` | Role management with `RoleFormModal` |
| `BlogListPage.tsx` | Blog post list with status/filter |
| `BlogFormPage.tsx` | Blog post editor using `BlockEditor`, with AI draft handoff via sessionStorage |
| `MarketingDashboardPage.tsx` | Marketing metrics overview |
| `EmailTemplatesPage.tsx` | Email template CRUD with `EmailTemplateFormModal` and live preview |
| `EmailQueuePage.tsx` | View and manage queued email sends |
| `SubscribersPage.tsx` | Email subscriber management |
| `AiChatPage.tsx` | Full-page admin AI chat (generic + Hermes mode) |
| `AiTryOnFeedbackPage.tsx` | AI try-on feature feedback review |
| `HermesPage.tsx` | Hermes runner status and overview |
| `HermesReportsPage.tsx` | Hermes-generated reports table with filters |
| `HermesMonitorPage.tsx` | Hermes event monitor with detail view |
| `HermesLiveMonitorPage.tsx` | Live SSE feed of Hermes activity |
| `ToolRiskPage.tsx` | Tool risk/governance configuration |
| `ReviewsPage.tsx` | Customer review moderation |

## For AI Agents
### Working In This Directory
- Pages own layout composition; reusable UI goes in `components/`.
- Use domain stores for mutations and local state; use TanStack Query hooks (`queries/`) for read-heavy dashboard data.
- All protected pages must nest inside `<AdminRoute>` in `App.tsx`.
- Tailwind only; Vietnamese labels, empty states, and error messages.
- Destructive actions require `useFeedback().confirm(...)` before executing.
- Blog AI draft handoff uses `sessionStorage` keyed by `AI_BLOG_DRAFT_STORAGE_KEY`.

### Common Patterns
- Page data load: call `store.fetchX()` in a `useEffect` on mount (or on filter change).
- Modals: controlled by local `useState` boolean or `store.selectedId !== null`.
- Pagination: store holds `filters.page`; UI calls `store.setFilters({ page: n })`.

## Dependencies
### Internal
- `@/stores/*` — domain async state
- `@/queries/*` — TanStack Query hooks (dashboard)
- `@/components/*` — all UI building blocks
- `@/types/*` — page-level DTOs

### External
- react-router-dom (navigation, `useParams`, `useNavigate`)

<!-- MANUAL: -->
