<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-06-08 -->

# frontend-admin/src/pages

## Purpose
Route-level admin screens. Pages compose stores, API clients, tables/forms/modals.

## Page Map
| Page | Purpose |
|------|---------|
| `LoginPage` | Admin login |
| `DashboardPage` | Metrics/overview |
| `ProductListPage` / `ProductFormPage` | Product CRUD |
| `CategoriesPage` | Category management |
| `MediaPage` | Media upload/library |
| `OrdersPage` | Order management |
| `UsersPage` / `RolesPage` | User and role admin |
| `AiChatPage` | Admin AI assistant/chat tools |
| `ToolRiskPage` | Tool risk config/governance |

## Local Conventions
- Pages own layout composition; reusable UI goes in `components/`.
- Use Zustand stores for load/mutate flows.
- Protect admin pages via `AdminRoute` in `App.tsx`.
- Tailwind only; keep design consistent with burgundy/gold theme.
- Vietnamese labels, empty states, errors, confirmations.

## Gotchas
- `LoginPage` uses `GuestRoute`; authenticated admin redirects into admin area.
- `ToolRiskPage` maps backend `ToolRiskConfig`; treat as security/admin governance UI.
- For destructive actions, require explicit confirm modal/prompt.
