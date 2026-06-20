<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# pages

## Purpose
Route-level customer screens. Each page is a folder with main component, CSS Module, and local subcomponents/data when needed.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `AccessoriesPage/` | Accessory catalog page |
| `AccountPage/` | Protected account modal overlay: profile, edit form, orders, addresses, image history |
| `AiTryonPage/` | AI virtual try-on workflow: photo upload, clothing/accessory panel, pagination, result display |
| `AuthCallbackPage/` | Generic OAuth callback handler |
| `AuthGoogleCallbackPage/` | Google OAuth code exchange callback |
| `AuthZaloCallbackPage/` | Zalo OAuth code exchange callback |
| `BlogDetailPage/` | Individual blog post detail with SEO |
| `BlogPage/` | Blog listing with category/tag filtering |
| `CartPage/` | Cart items, summary, customer notes, checkout entry |
| `CollectionPage/` | Brand story, era sections, gallery |
| `DataDeletionPage/` | Data deletion request static page |
| `HomePage/` | Landing page: hero, collection, products, AI, accessories, materials, store, feedback |
| `LoginPage/` | Email/password + OAuth login form |
| `NotFoundPage/` | 404 fallback page |
| `OrderDetailPage/` | Customer order detail view |
| `PrivacyPolicyPage/` | Privacy policy static page |
| `ProductDetailPage/` | Product detail, variant selection, reviews, comments, add-to-cart |
| `ProductsPage/` | Product listing with filtering by category |
| `ResetPasswordPage/` | Password reset form |
| `UnsubscribePage/` | Email unsubscribe confirmation page |

## Local Conventions
- Keep route composition in `App.tsx`; pages own local layout/data states.
- Use CSS Modules, not Tailwind.
- Use domain hooks for server data; avoid direct `fetch` in pages unless adding a new hook/API module.
- `AccountPage` is modal overlay, not normal route page.
- Blog/detail/product pages should keep SEO tags/schema current via `components/Seo`.
