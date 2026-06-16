<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# pages

## Purpose
Route-level customer screens. Each page is a folder with main component, CSS Module, and local subcomponents/data when needed.

## Page Map
| Page | Purpose |
|------|---------|
| `HomePage/` | Landing page sections |
| `CollectionPage/` | Brand story/gallery |
| `ProductsPage/` | Product listing/filtering |
| `ProductDetailPage/` | Product detail, reviews/comments, purchase actions |
| `AccessoriesPage/` | Accessory catalog |
| `BlogPage/`, `BlogDetailPage/` | Blog list/detail SEO content |
| `AiTryonPage/` | AI try-on workflow: upload, catalog selection, accessories, result |
| `CartPage/` | Cart items/summary/notes |
| `OrderDetailPage/` | Customer order detail |
| `AccountPage/` | Protected account modal: profile, edit, orders, addresses, images |
| `LoginPage/`, `ResetPasswordPage/` | Auth forms |
| `AuthCallbackPage/`, `AuthGoogleCallbackPage/`, `AuthZaloCallbackPage/` | OAuth callbacks |
| `PrivacyPolicyPage/`, `DataDeletionPage/`, `UnsubscribePage/`, `NotFoundPage/` | Static/utility pages |

## Local Conventions
- Keep route composition in `App.tsx`; pages own local layout/data states.
- Use CSS Modules, not Tailwind.
- Use domain hooks for server data; avoid direct `fetch` in pages unless adding a new hook/API module.
- `AccountPage` is modal overlay, not normal route page.
- Blog/detail/product pages should keep SEO tags/schema current via `components/Seo`.
