<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components

## Purpose
All admin UI components: app shell layout, domain-specific forms/modals/tables, feature panels, and shared UI primitives. Components are presentational where possible; pages and stores own data loading.

## Key Files
| File | Description |
|------|-------------|
| `AdminLayout.tsx` | Top-level shell: sidebar nav, header, AI chat button, `<Outlet>` |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `admin/` | CRUD modals and forms for users, roles, categories, promos, email templates |
| `ai/` | Admin AI chat sidebar, message bubbles, chat history, mode selector, confirm cards |
| `blog/` | Block editor and blog preview components |
| `dashboard/` | Dashboard widget cards: stats, charts, tables, alerts |
| `hermes/` | Hermes report table, filters, detail drawer, reports panel |
| `ui/` | shadcn-style primitives: Button, Card, Input, Label, Badge, Select, Sheet, Table, Textarea, Checkbox, Feedback |

## For AI Agents
### Working In This Directory
- Use `cn()` from `@/lib/utils` for conditional class merging (clsx + tailwind-merge).
- Import primitives from `@/components/ui/` before creating new visual variants.
- Components must not make direct API calls; pages/stores handle data fetching.
- Use `useFeedback()` from `@/components/ui/feedbackContext` for toasts and confirm dialogs.
- Tailwind v4 utilities only — no CSS Modules, no inline style objects for theming.
- Accessibility: buttons need `aria-label`, modals need `role="dialog"` and focus management, tables need meaningful headings.

### Common Patterns
- Modal overlays use a `ModalOverlay` wrapper from `admin/ModalOverlay.tsx` for consistent backdrop/z-index.
- Tables are server-paginated; pagination controls live in the parent panel/page, not inside table components.
- Feature panels (e.g. `HermesReportsPanel`) compose filters + table + detail drawer into a single self-contained unit.
- Chart components use Recharts; receive data as props from `DashboardPage`.

## Dependencies
### Internal
- `@/stores/*` — all domain data
- `@/lib/utils` — `cn()`
- `@/types/*` — prop type shapes

### External
- lucide-react (icons)
- Recharts (charts in `dashboard/`)
- react-markdown + remark-gfm (blog preview, AI message rendering)

<!-- MANUAL: -->
