<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/queries

## Purpose
TanStack Query v5 option factories, custom hooks, and query key registry for read-heavy admin data. Currently focused on dashboard queries. Most domain state uses Zustand stores; this directory is used where React Query's caching and background-refetch model is a better fit.

## Key Files
| File | Description |
|------|-------------|
| `queryKeys.ts` | Centralized query key registry — all keys are `as const` tuples; `DashboardPeriod` type (7 \| 30 \| 90) defined here |
| `dashboardQueries.ts` | `dashboardQueryOptions` factory object (summary, revenue, ordersByStatus, recentOrders, topProducts, userGrowth); `useDashboardQueries(period)` parallel hook; `usePrefetchDashboard()` hook |
| `invalidateAdminQueries.ts` | `invalidateAdminDashboardQueries()` — called by stores after mutations that affect dashboard data |

## For AI Agents
### Working In This Directory
- Add query keys to `queryKeys.ts` before creating a new query option — never inline key arrays in hooks.
- Use `queryOptions({...})` factory from TanStack Query v5 for type-safe `queryKey` + `queryFn` pairing.
- `useDashboardQueries` uses `useQueries` for parallel fetching — maintain this pattern for new multi-query hooks.
- `invalidateAdminDashboardQueries()` uses the singleton `queryClient` from `@/lib/queryClient`; call it from store mutations, not from components.
- Do not add mutation hooks here — mutations belong in Zustand stores (`src/stores/`).

### Common Patterns
- New query domain: add keys to `queryKeys.ts`, create `<domain>QueryOptions` in a new `<domain>Queries.ts`, export a `use<Domain>Queries()` hook.
- `staleTime` for dashboard data: summary/orders = 60 s, charts = 120 s, recent orders = 30 s. Match similar cadence for new query types.
- GC time is 30 min (`DASHBOARD_GC_TIME`) to keep data warm during a single admin session.

## Dependencies
### Internal
- `@/api/dashboard` — underlying fetch functions
- `@/lib/queryClient` — singleton `queryClient` for `invalidateAdminDashboardQueries`

### External
- `@tanstack/react-query` (`queryOptions`, `useQueries`, `useQueryClient`)

<!-- MANUAL: -->
