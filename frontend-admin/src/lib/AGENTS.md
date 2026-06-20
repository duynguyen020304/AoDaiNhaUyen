<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/lib

## Purpose
Shared utilities that do not belong to a specific domain. Provides the `cn()` class-merge helper, the configured TanStack Query client with auth-boundary handling, and email HTML preview utilities.

## Key Files
| File | Description |
|------|-------------|
| `utils.ts` | `cn(...inputs)` — combines `clsx` + `tailwind-merge` for conditional Tailwind class merging |
| `queryClient.ts` | Configured `QueryClient`: 30 s staleTime, 30 min GC, no window-focus refetch, 401/403 clears cache and calls `authStore.markAnonymous()` |
| `emailPreview.ts` | `buildEmailPreviewDocument()`, `createEmailPreviewUrl()`, `openEmailPreviewInNewTab()` — creates a standalone HTML email preview as a `blob:` URL |

## For AI Agents
### Working In This Directory
- `cn()` is the only class-merge utility in this project — always import it from `@/lib/utils`.
- `queryClient` is a singleton; import it as a named export when you need `invalidateQueries` outside of React components.
- `clearAdminQueryCache()` is exported from `queryClient.ts` — use it (not `queryClient.clear()` directly) in stores.
- `openEmailPreviewInNewTab()` returns `false` if the popup was blocked; show a fallback message to the user.
- Email preview URLs are `blob:` — they are auto-revoked after 60 s via `setTimeout`.

### Common Patterns
- Query key invalidation from outside components: import `queryClient` and call `queryClient.invalidateQueries({ queryKey: queryKeys.X.root })`.
- Auth boundary: `QueryCache.onError` handles 401/403 globally; individual queries do not need to handle these.

## Dependencies
### Internal
- `@/stores/authStore` — lazy-imported in `queryClient.ts` to avoid circular deps (`import('@/stores/authStore')`)
- `@/api/client` — `HttpError` for auth boundary detection

### External
- clsx
- tailwind-merge
- `@tanstack/react-query` (`QueryClient`, `QueryCache`)

<!-- MANUAL: -->
