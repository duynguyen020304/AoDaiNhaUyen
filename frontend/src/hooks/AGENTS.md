<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# hooks

## Purpose
TanStack Query (v5) domain hooks for all server state in the customer SPA. Queries fetch and cache data; mutations write data and keep the cache consistent via `setQueryData` + `invalidateQueries`. All hooks import query keys from `src/lib/queryKeys.ts` and call API functions from `src/api/`.

## Key Files
| File | Description |
|------|-------------|
| (none at root) | All hooks are organized into domain subdirectories below |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `auth/` | Current-user query and session bootstrap helper |
| `blog/` | Blog list, detail, related posts, categories, tags queries; product spotlight query |
| `cart/` | Cart query and add/update/remove/clear mutations |
| `catalog/` | Header categories, product list, product detail, and parallel category-products queries |
| `media/` | User uploaded images (AI try-on history) query |
| `user/` | User profile, addresses, orders queries; profile/address/order mutations |

## For AI Agents
### Working In This Directory
- Add new domain hooks in the matching subdirectory (e.g., cart hooks go in `cart/`).
- Use `queryKeys` from `src/lib/queryKeys.ts` for all `queryKey` values — never hardcode arrays.
- Mutations always call `setQueryData` in `onSuccess` for optimistic update, then `invalidateQueries` in `onSettled` for consistency.
- `enabled` parameter on queries gates fetching to authenticated/ready state; pass `false` when prerequisites are missing.

### Common Patterns
```ts
// Query hook
export function useXxxQuery(param: string) {
  return useQuery({
    queryKey: queryKeys.xxx.detail(param),
    queryFn: () => xxxApi.getXxx(param),
    enabled: Boolean(param),
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
  });
}

// Mutation hook
export function useXxxMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: XxxPayload) => xxxApi.createXxx(payload),
    onSuccess: (data) => queryClient.setQueryData(queryKeys.xxx.detail(data.id), data),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.xxx.all }),
  });
}
```

## Dependencies
### Internal
- `src/api/*` — raw fetch functions called by query/mutation functions
- `src/lib/queryKeys.ts` — centralized query key factory
- `src/types/*` — TypeScript types for payloads and responses
- `src/utils/cartMapping.ts` — cart asset URL normalization (used by cart hooks)

### External
- `@tanstack/react-query` — `useQuery`, `useMutation`, `useQueries`, `useQueryClient`

<!-- MANUAL: -->
