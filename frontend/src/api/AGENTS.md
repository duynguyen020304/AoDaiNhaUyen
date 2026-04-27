<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# api

## Purpose
API client modules for backend talk. All modules use shared `request()` and `requestPaginated()` helpers from `client.ts` (fetch-based, not axios). Responses use `ApiEnvelope<T>` shape in `types/api.ts`.

## Key Files
| File | Description |
|------|-------------|
| `client.ts` | Shared fetch client: resolve regional API base URL, provide `request<T>()` and `requestPaginated<T>()` helpers, set JSON content type, unwrap `ApiEnvelope`. Also export `resolveAssetUrl()` for relative-to-absolute URL conversion and `API_BASE_URL` constant |
| `auth.ts` | Auth: `login()`, `register()`, `logout()`, `refreshSession()`, `getCurrentUser()`, `forgotPassword()`, `resetPassword()`, `googleLogin()`, `facebookLogin()`, `buildGoogleAuthorizeUrl()`, `buildFacebookAuthorizeUrl()`. OAuth client IDs from `PUBLIC_GOOGLE_CLIENT_ID` / `PUBLIC_FACEBOOK_CLIENT_ID` env vars |
| `cart.ts` | Shopping cart CRUD: `getCart()`, `addCartItem()`, `updateCartItem()`, `removeCartItem()`, `clearCart()`. All return `Cart` type |
| `catalog.ts` | Product catalog: `getHeaderCategories()` for header nav, `getProducts()` with filters (categorySlug, productType, featured, size, page, pageSize). Return paginated results |
| `checkout.ts` | Order placement: `checkout()` accepts `CheckoutPayload` (address or addressId, note, paymentMethod) and returns `CheckoutResult` with order details |
| `user.ts` | User profile management: `getUserProfile()`, `updateProfile()`, `getAddresses()`, `createAddress()`, `deleteAddress()`, `getOrders()` |
| `aiTryon.ts` | AI virtual try-on: `getAiTryOnCatalog()` returns garments and accessories, `submitAiTryOn()` sends person image + garment selection as FormData |
| `chat.ts` | Chat with AI assistant: `listChatThreads()`, `createChatThread()`, `getChatThread()`, `sendChatMessage()` (with file attachments via FormData), `executeChatTryOn()`. Messages include structured payloads for recommendations and try-on triggers |

## For AI Agents
### API Base URL Resolution
Client resolves API base URL by priority:
1. Regional URL based on hostname: `aodainhauyen.io.vn` -> `https://api-hk1.aodainhauyen.io.vn`, `backup.aodainhauyen.io.vn` -> `https://api-us1.aodainhauyen.io.vn`
2. `VITE_API_BASE_URL` or `PUBLIC_BACKEND_DOMAIN` env var
3. Fallback: `http://localhost:5043`

### Request Pattern
All API functions follow pattern:
```ts
export function resourceName(): Promise<ReturnType> {
  return request<ReturnType>('/api/path', { method: 'POST', body: JSON.stringify(payload) });
}
```
- `request<T>()` unwraps `ApiEnvelope<T>` and returns `T` (the `data` field)
- `requestPaginated<T>()` returns full `PaginatedApiEnvelope<T>` including pagination metadata
- All requests include `credentials: 'include'` for cookie-based auth
- FormData used for file uploads (aiTryon, chat attachments)
- Error messages in Vietnamese