<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend/src

## Purpose
Customer SPA source: routes, components, API clients, auth contexts, TanStack Query hooks/cache, global styles, and utilities.

## Key Files
| File | Description |
|------|-------------|
| `main.tsx` | App bootstrap: PersistQueryClientProvider -> HelmetProvider -> BrowserRouter -> AuthProvider -> ToastProvider -> App; registers service worker |
| `App.tsx` | Route tree, scroll restore, header/footer visibility, account modal routing |
| `vite-env.d.ts` | Vite client types |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `api/` | Fetch API modules (see `api/AGENTS.md`) |
| `auth/` | Auth contexts/hooks/route guards |
| `components/` | Reusable UI components |
| `hooks/` | TanStack Query domain hooks for auth/blog/cart/catalog/media/user |
| `lib/` | Query client, query keys, query persistence |
| `pages/` | Route-level pages (see `pages/AGENTS.md`) |
| `styles/` | Global CSS tokens/reset/typography/texture/transitions |
| `types/` | Domain TypeScript types |
| `utils/` | Mapping, motion, image conversion, service worker cache helpers |

## Route Map
| Path | Component |
|------|-----------|
| `/` | `HomePage` |
| `/collection` | `CollectionPage` |
| `/products`, `/products/:slug` | `ProductsPage`, `ProductDetailPage` |
| `/accessories` | `AccessoriesPage` |
| `/blog`, `/blog/:slug` | `BlogPage`, `BlogDetailPage` |
| `/ai-tryon` | `AiTryonPage` |
| `/cart` | `CartPage` |
| `/orders/:id` or detail route | `OrderDetailPage` |
| `/login`, `/reset-password` | Auth pages |
| `/auth/google/callback`, `/auth/zalo/callback` | OAuth callbacks |
| `/privacy-policy`, `/data-deletion`, `/unsubscribe`, `*` | Policy/unsubscribe/404 |
| `/account/*` | Protected account modal overlay |

## Local Conventions
- Use domain hooks (`src/hooks/*`) for query/mutation flows; keep raw API calls in `src/api/*`.
- Query keys centralized in `src/lib/queryKeys.ts`.
- API envelope types live in `src/types/api.ts`.
- CSS Modules paired with PascalCase component/page folders.
- Avoid admin imports; customer and admin apps are separate.
