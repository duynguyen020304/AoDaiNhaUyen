<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# lib

## Purpose
TanStack Query infrastructure: the shared `QueryClient` instance, the centralized query key factory, and the localStorage persistence layer. These three files are the foundation that all domain hooks depend on.

## Key Files
| File | Description |
|------|-------------|
| `queryClient.ts` | Exports the singleton `queryClient`. Defaults: `retry: 1`, `refetchOnWindowFocus: false`, `staleTime: 30s`, `gcTime: 30min`, `networkMode: 'online'`. Mutations have `retry: 0` |
| `queryKeys.ts` | Exports the `queryKeys` const object — the single source of truth for all query key arrays. Domains: `auth.me`, `categories.header`, `products.list(params)/detail(slug)`, `blog.list/detail/related/tags/categories`, `cart.current`, `addresses.list`, `orders.list`, `user.profile`, `media.myImages`, `chat.threads/thread(id)`, `aiTryOn.catalog(params)` |
| `queryPersist.ts` | Exports `queryPersister` (localStorage persister, key `aodai.customer.query-cache`), `clearPersistedQueryCache()`, `shouldDehydrateQuery(queryKey)` (excludes auth/cart/user/orders/addresses/media from persistence), `pruneUnsafePersistedCache(client)` |

## For AI Agents
### Working In This Directory
- Always add new query key shapes to `queryKeys.ts` — never hardcode key arrays in hook files.
- When adding a new domain, add it to `shouldDehydrateQuery` exclusion list if the data is user-private (session-bound).
- `queryClient` is imported by `main.tsx` for `PersistQueryClientProvider` and by `serviceWorkerCache.ts` for cache invalidation on version change.
- `queryPersist.ts` uses synchronous localStorage — safe for SSR-free Vite SPA, but do not use `window` references in SSR contexts.

### Common Patterns
```ts
// Add a new domain key
export const queryKeys = {
  // ...existing...
  newDomain: {
    all: ['new-domain'] as const,
    list: (params: Params) => ['new-domain', 'list', params] as const,
    detail: (id: string) => ['new-domain', 'detail', id] as const,
  },
} as const;

// Exclude private data from persistence
return scope !== 'auth' && scope !== 'new-domain' && /* existing exclusions */;
```

## Dependencies
### Internal
- `src/api/catalog` — `GetProductsParams` type (used in `queryKeys.ts` for typed product list key)

### External
- `@tanstack/react-query` — `QueryClient`
- `@tanstack/query-sync-storage-persister` — `createSyncStoragePersister`
- `@tanstack/react-query-persist-client` — `PersistedClient`, `Persister`

<!-- MANUAL: -->
