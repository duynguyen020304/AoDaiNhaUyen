<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/auth

## Purpose
React Router route guards. Protects admin routes from unauthenticated access and redirects authenticated admins away from the login page.

## Key Files
| File | Description |
|------|-------------|
| `AdminRoute.tsx` | Renders a spinner while `authStore.status === 'loading'`; redirects to `/login` if anonymous or user lacks the `admin` role; otherwise renders `<Outlet>` |
| `GuestRoute.tsx` | Returns null while loading; redirects authenticated admins to `/admin/products`; otherwise renders `<Outlet>` |

## For AI Agents
### Working In This Directory
- Both guards read `status` and `user` from `useAuthStore` (Zustand).
- Role check is `user.roles.includes('admin')` — a string array membership test.
- `AdminRoute` shows a full-screen `Loader2` spinner during the bootstrap phase; `GuestRoute` renders nothing (avoids flash).
- Do not add business logic here; these files should stay thin guards only.

### Common Patterns
- Wrap all `/admin/*` route subtrees with `<AdminRoute>` in `App.tsx`.
- Wrap `/login` with `<GuestRoute>` in `App.tsx`.
- Auth state bootstrap happens in `main.tsx` or `App.tsx` via `authStore.bootstrap()`.

## Dependencies
### Internal
- `@/stores/authStore` — `useAuthStore`, `AuthStatus`, `AuthUser`

### External
- react-router-dom (`Navigate`, `Outlet`)
- lucide-react (`Loader2`)

<!-- MANUAL: -->
