<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-07-14 -->

# frontend-admin/src/pages

## Purpose
Route-level admin screens. Pages compose stores, API clients, tables, charts, forms, modals, and admin layout slots.

## Page Map
| Page | Purpose |
|------|---------|
| `LoginPage` | Admin login |
| `DashboardPage` | Metrics, charts, low stock, recent orders |
| `ProductListPage`, `ProductFormPage` | Product CRUD |
| `CategoriesPage` | Category CRUD |
| `OrdersPage` | Order management |
| `PromosPage` | Promo code CRUD |
| `MediaPage` | Media library/upload/delete |
| `UsersPage`, `RolesPage` | User/role admin |
| `BlogListPage`, `BlogFormPage` | Blog CMS listing/editor/preview |
| `MarketingDashboardPage` | Marketing metrics |
| `EmailTemplatesPage`, `EmailQueuePage` | Email templates and queued sends |
| `SubscribersPage` | Subscriber management |
| `AiChatPage` | Admin AI/Hermes chat tools |
| `LlmLogsPage` | LLM audit log search/detail |
| `ToolRiskPage` | Tool risk/governance config |

## Local Conventions
- Pages own layout composition; reusable UI goes in `components/`.
- Use stores for domain state; use TanStack Query where existing query hooks already own reads.
- Protected admin pages must stay behind `AdminRoute` in `App.tsx`.
- Tailwind only; Vietnamese labels/errors/empty states.
- Destructive actions need explicit confirmation.
