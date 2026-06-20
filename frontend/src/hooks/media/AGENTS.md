<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# hooks/media

## Purpose
TanStack Query hooks for user media assets. Currently covers the AI try-on image history shown in the AccountPage image history tab.

## Key Files
| File | Description |
|------|-------------|
| `useMediaQueries.ts` | `useMyImagesQuery(page, pageSize, sourceType?)` — paginated query for the authenticated user's uploaded/generated images. Query key extends `queryKeys.media.myImages` with `{ page, pageSize, sourceType }`. `staleTime: 5min` |

## For AI Agents
### Working In This Directory
- Media queries are excluded from localStorage persistence (`shouldDehydrateQuery` blocks `scope === 'media'`).
- `sourceType` is optional; pass `'ai-tryon'` to filter to AI try-on results only.
- The base key `queryKeys.media.myImages` is `['media', 'my-images']`; the full key appends the pagination params object.

### Common Patterns
```ts
const { data: images } = useMyImagesQuery(page, 12, 'ai-tryon');
```

## Dependencies
### Internal
- `src/api/media` — `getMyImages(page, pageSize, sourceType)`
- `src/lib/queryKeys` — `queryKeys.media.myImages`

### External
- `@tanstack/react-query` — `useQuery`

<!-- MANUAL: -->
