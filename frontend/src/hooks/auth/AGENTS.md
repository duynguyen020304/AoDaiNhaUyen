<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# hooks/auth

## Purpose
Auth-related TanStack Query hooks and bootstrap utilities. Provides the current-user query used by `AuthContext` to establish session on app mount.

## Key Files
| File | Description |
|------|-------------|
| `useAuthQueries.ts` | Exports `bootstrapCurrentUser()` — an async function that tries `getCurrentUser()` then falls back to `refreshSession()`, returning `AuthUser \| null`. Exports `useCurrentUserQuery()` — a query hook with `staleTime: 5min`, `gcTime: 30min`, `retry: false` |

## For AI Agents
### Working In This Directory
- `bootstrapCurrentUser` is called directly by `AuthContext` (outside React Query) during app mount, and also wrapped as a query via `useCurrentUserQuery`.
- The query key is `queryKeys.auth.me` (`['auth', 'me']`).
- Auth queries are excluded from localStorage persistence by `queryPersist.ts` (`shouldDehydrateQuery` blocks `scope === 'auth'`).

### Common Patterns
```ts
import { useCurrentUserQuery } from '../hooks/auth/useAuthQueries';
const { data: user, isLoading } = useCurrentUserQuery();
```

## Dependencies
### Internal
- `src/api/auth` — `getCurrentUser()`, `refreshSession()`
- `src/lib/queryKeys` — `queryKeys.auth.me`
- `src/types/auth` — `AuthUser`

### External
- `@tanstack/react-query` — `useQuery`

<!-- MANUAL: -->
