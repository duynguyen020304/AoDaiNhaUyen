<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-06-08 -->

# frontend-admin/src/stores

## Purpose
Zustand domain stores for admin server/state orchestration.

## Store Map
| Store | Purpose |
|-------|---------|
| `authStore` | Admin session/login/logout/current user |
| `productStore` | Product list/detail mutations |
| `categoryStore` | Category CRUD/list state |
| `userStore` | User/admin management |
| `roleStore` | Roles/permissions state |
| `dashboardStore` | Dashboard metrics |
| `adminAiStore` | Admin AI chat/tool state |

## Local Conventions
- Pattern: `create<State>((set, get) => ({ ... }))`.
- Track `loading` + `error` per store.
- Mutations refetch affected lists or patch local state predictably.
- Error strings shown to admin → Vietnamese.
- Keep API calls in stores for admin app; customer frontend uses different patterns.

## Anti-Patterns
- No Redux/Context for domain state.
- No silent catch; set `error` or rethrow for caller toast.
- Avoid cross-store circular calls; share helpers via API/types when possible.
