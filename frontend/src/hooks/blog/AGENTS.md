<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# hooks/blog

## Purpose
TanStack Query hooks for blog content and a product spotlight cross-query. Covers blog list, detail, related posts, categories, tags, and spotlight product fetching for blog post sidebars or inline product promotions.

## Key Files
| File | Description |
|------|-------------|
| `useBlogQueries.ts` | Five query hooks: `useBlogList(params)` (paginated, stale 1min), `useBlogDetail(slug)` (stale 5min), `useRelatedPosts(slug)` (stale 5min), `useBlogCategories()` (stale 30min), `useBlogTags()` (stale 10min) |
| `useProductSpotlight.ts` | `useProductSpotlight(slugs[])` — fetches up to 12 products by slug array using `getProductsBySlugs`. Uses inline key `['products', 'spotlight', normalized]` (not in `queryKeys`). Enabled only when slugs array is non-empty |

## For AI Agents
### Working In This Directory
- All blog hooks use `queryKeys.blog.*` for cache keys.
- `useBlogDetail` and `useRelatedPosts` are both gated with `enabled: Boolean(slug)` — safe to call with undefined.
- `useProductSpotlight` slices input to max 12 slugs and filters empty strings before querying.

### Common Patterns
```ts
const { data: posts } = useBlogList({ category: 'style', page: 1 });
const { data: post } = useBlogDetail(slug);
const { data: spotlight } = useProductSpotlight(['ao-dai-truyen-thong', 'ao-dai-lua-tron']);
```

## Dependencies
### Internal
- `src/api/blog` — `getBlogPosts`, `getBlogPost`, `getRelatedPosts`, `getBlogCategories`, `getBlogTags`
- `src/api/catalog` — `getProductsBySlugs` (used by `useProductSpotlight`)
- `src/lib/queryKeys` — `queryKeys.blog.*`
- `src/types/blog` — `BlogListParams`

### External
- `@tanstack/react-query` — `useQuery`

<!-- MANUAL: -->
