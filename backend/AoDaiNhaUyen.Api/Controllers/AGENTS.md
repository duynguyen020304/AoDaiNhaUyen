<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Controllers

## Purpose
ASP.NET Core API controllers. Thin controllers: handle HTTP concerns (routing, request parsing, response formatting), delegate business logic to Application-layer services via DI.

## Files
| File | Route | Auth | Description |
|------|-------|------|-------------|
| `AiTryOnController.cs` | `api/v1/ai-tryon` | No | AI virtual try-on: GET catalog, POST generates try-on image. Validates image uploads (8MB max, no GIF), supports garment selection by product ID or uploaded image, up to 3 accessory images |
| `AuthController.cs` | `api/auth` | Mixed | Full auth: register, login, Google/Facebook OAuth, refresh, logout, email verification (GET redirect), forgot-password, reset-password, /me (authorized). Manages HttpOnly cookies for tokens |
| `CacheController.cs` | `api/cache` | No | Cache metadata endpoint exposing cache/version state to clients |
| `CategoriesController.cs` | `api/v1/categories` | No | Category listing: GET returns flat list, GET header returns tree for navigation |
| `ChatController.cs` | `api/v1/chat/threads` | Optional | AI stylist chat: list/create threads, get thread detail, send messages with image attachments, execute in-chat try-on. Supports anonymous guests via `stylist_guest` cookie |
| `CheckoutController.cs` | `api/users/me/checkout` | Yes | Order placement from cart. Creates order with shipping address and items |
| `HealthController.cs` | `health` | No | Health check returning `{ status: "ok", timestampUtc }` |
| `ProductsController.cs` | `api/v1/products` | No | Product catalog: paginated listing with filters (categorySlug, productType, featured, size), detail by slug |
| `UserAddressController.cs` | `api/users/me/addresses` | Yes | User address CRUD: list, create, delete addresses |
| `UserCartController.cs` | `api/users/me/cart` | Yes | Shopping cart: get cart, add item, update item quantity, remove item, clear cart |
| `UserController.cs` | `api/users/me` | Yes | User profile: GET profile, PUT update profile |
| `UserOrderController.cs` | `api/users/me/orders` | Yes | User order history: paginated listing with order items |

## For AI Agents
### Working In This Directory
- All controllers use primary constructor dependency injection
- All responses go through `ApiResponseFactory.Success/Failure/PaginatedSuccess`
- Authorized controllers extract userId from `ClaimTypes.NameIdentifier` via `GetCurrentUserId()` helper
- Request records defined as nested sealed records inside controllers (e.g., `AuthController.RegisterRequest`)
- Vietnamese error messages in responses (e.g., "Khong tim thay du lieu", "Dang ky that bai")
- Image uploads use `IFormFile` with content type validation (must be image/*, no GIF)
- ChatController unique: supports authenticated and anonymous access via guest cookie