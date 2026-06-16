<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# Controllers

## Purpose
HTTP presentation layer. Controllers parse/validate requests, shape API envelopes, apply auth/roles, then delegate to Application service interfaces.

## Customer Controllers
| File | Route | Auth | Notes |
|------|-------|------|-------|
| `AuthController.cs` | `api/auth` | Mixed | Register/login/OAuth/refresh/logout/verify/reset/me; writes HttpOnly cookies |
| `ProductsController.cs` | `api/v1/products` | No | Product list/detail with filters |
| `CategoriesController.cs` | `api/v1/categories` | No | Flat + header tree category data |
| `BlogPostsController.cs` | `api/v1/blog*` + admin blog routes | Mixed | Published blog list/detail/categories and admin blog CMS helpers |
| `AiTryOnController.cs` | `api/v1/ai-tryon` | No | Catalog try-on + upload try-on; strict image limits |
| `ChatController.cs` | `api/v1/chat/threads` | Optional | AI stylist threads; supports `stylist_guest` cookie |
| `UserCartController.cs` | `api/users/me/cart` | Yes | Cart CRUD |
| `CheckoutController.cs` | `api/users/me/checkout` | Yes | Place order from cart |
| `UserOrderController.cs` | `api/users/me/orders` | Yes | Order history/detail |
| `UserController.cs` | `api/users/me` | Yes | Profile read/update |
| `UserAddressController.cs` | `api/users/me/addresses` | Yes | Address CRUD |
| `PromoController.cs` | `api/promo` | Mixed | Promo validation/application |
| `ReviewsController.cs` | `api/v1/products/{productId:guid}/reviews` | Mixed | Product review APIs |
| `CommentsController.cs` | `api/v1/products/{productId:guid}/comments` | Mixed | Product comment APIs |
| `MediaController.cs` | `api/v1/media` | Mixed | Media asset access/upload helpers |
| `MarketingController.cs` | `api/marketing` | Mixed | Subscribe/unsubscribe/marketing consent |
| `EventsController.cs` | `api/events` | Mixed | Customer event collection |
| `CacheController.cs` | `api/cache` | No | Cache/version metadata |
| `HealthController.cs` | `health` | No | Health smoke endpoint |

## Admin Controllers
| File | Route | Notes |
|------|-------|-------|
| `AdminProductsController.cs` | `api/admin/products` | Product CRUD/media/status |
| `AdminCategoriesController.cs` | `api/admin/categories` | Category CRUD/tree |
| `AdminOrdersController.cs` | `api/admin/orders` | Order management |
| `AdminUsersController.cs` | `api/admin/users` | User management |
| `AdminRolesController.cs` | `api/admin/roles` | Role management |
| `AdminMediaController.cs` | `api/admin/media` | Admin media upload/list/delete |
| `AdminInventoryController.cs` | `api/admin/inventory` | Stock/low-stock ops |
| `AdminPromosController.cs` | `api/admin/promos` | Promo CRUD |
| `AdminDashboardController.cs` | `api/admin/dashboard` | Stats/charts/top products |
| `AdminMarketingController.cs` | multiple `api/admin/*` | Templates, subscribers, jobs, marketing dashboard |
| `AdminAiController.cs` | `api/admin/ai` | Admin AI chat/actions |
| `AdminHermesController.cs` | `api/admin/hermes` | Hermes control plane |
| `AdminLlmLogsController.cs` | `api/admin/llm-logs` | LLM audit log search/detail |
| `AdminToolRiskController.cs` | `api/admin/tools-risk` | AI tool risk config |

## Local Conventions
- Use `[ApiController]`, `[Route(...)]`, primary constructor DI.
- Use `[Authorize]` / role policy on admin and user routes.
- Authorized user routes extract `ClaimTypes.NameIdentifier`; keep helper consistent.
- Request DTOs may be nested sealed records for controller-only shapes; shared shapes belong in Application DTOs.
- Vietnamese error text in response messages.
