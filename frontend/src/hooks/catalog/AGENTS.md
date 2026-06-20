<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# hooks/catalog

## Purpose
TanStack Query hooks for product catalog data. Covers header navigation categories, paginated product lists, individual product detail, and a parallel multi-category products query for pages that render multiple category sections simultaneously.

## Key Files
| File | Description |
|------|-------------|
| `useCatalogQueries.ts` | `useHeaderCategoriesQuery()` (stale 1hr, gc 24hr), `useProductsQuery(params, enabled)` (stale 30s, gc 5min), `useProductDetailQuery(slug)` (stale 5min), `useCategoryProductsQueries(categories, getParams)` (parallel `useQueries` — one query per category) |

## For AI Agents
### Working In This Directory
- `useHeaderCategoriesQuery` has very long staleTime (1hr) since nav categories rarely change.
- `useProductsQuery` has short staleTime (30s) since product availability/pricing can change frequently.
- `useCategoryProductsQueries` uses `useQueries` to fire N queries in parallel — returns `UseQueryResult<PaginatedProducts>[]` matching the input categories array order.
- `useProductDetailQuery` is gated with `enabled: Boolean(slug)`.

### Common Patterns
```ts
const { data: categories } = useHeaderCategoriesQuery();
const { data: products } = useProductsQuery({ categorySlug: 'ao-dai', page: 1 });
const { data: product } = useProductDetailQuery(slug);

// Parallel per-category fetch
const results = useCategoryProductsQueries(categories, (cat) => ({ categorySlug: cat.slug, limit: 6 }));
```

## Dependencies
### Internal
- `src/api/catalog` — `getHeaderCategories`, `getProducts`, `getProductBySlug`, `GetProductsParams`
- `src/lib/queryKeys` — `queryKeys.categories.header`, `queryKeys.products.*`
- `src/types/catalog` — `HeaderCategoryChild`, `PaginatedProducts`

### External
- `@tanstack/react-query` — `useQuery`, `useQueries`, `UseQueryResult`

<!-- MANUAL: -->
