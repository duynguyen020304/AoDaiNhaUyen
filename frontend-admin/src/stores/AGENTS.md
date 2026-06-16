<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-07-14 -->

# frontend-admin/src/stores

## Purpose
Zustand stores for admin async/domain state. Stores call `src/api/*`, track loading/errors, and expose mutation actions to pages/components.

## Store Map
| Store | Purpose |
|-------|---------|
| `authStore` | Admin session/login/logout/bootstrap |
| `productStore` | Product list/detail/create/update/delete/media |
| `categoryStore` | Category CRUD/tree/list |
| `userStore` | User management |
| `roleStore` | Roles/permissions |
| `dashboardStore` | Dashboard metrics |
| `promoStore` | Promo CRUD/validation admin state |
| `blogStore` | Blog CMS list/editor/publish state |
| `emailMarketingStore` | Templates, subscribers, email queue/dashboard |
| `adminAiStore` | Admin AI/Hermes chat and action state |
| `llmLogStore` | LLM a

## Gotchas
- `MediaPage` may still use direct API calls; do not force every existing path into stores during unrelated edits.
- Keep store state serializable; avoid storing React nodes/classes/functions except actions.
- If API shape changes, update `src/types/*`, API client, then store in same change.
