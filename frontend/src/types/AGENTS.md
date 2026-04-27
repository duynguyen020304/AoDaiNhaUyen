<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# types

## Purpose
TypeScript type definitions grouped by domain. Shared across API modules, components, pages. Types mirror backend API response shapes.

## Key Files
| File | Description |
|------|-------------|
| `api.ts` | Core API envelope types: `ApiError` (code + message), `ApiEnvelope<T>` (success, message, data, errors, timestamp), `PaginatedApiEnvelope<T>` (extends ApiEnvelope with hasNextPage, hasPreviousPage, totalPage, totalItem) |
| `auth.ts` | Auth types: `AuthUser` (id, fullName, email, avatarUrl, roles), `AuthStatus` union type (`'loading' \| 'authenticated' \| 'anonymous'`) |
| `address.ts` | Address types: `UserAddress` (full address with id, recipient info, province/district/ward/addressLine, isDefault), `CreateAddressPayload` (for creating new addresses) |
| `cart.ts` | Cart types: `CartItem` (id, variantId, productId, productName, slug, sku, variantName, size, color, imageUrl, price, salePrice, quantity, lineTotal), `Cart` (id, userId, totalItemCount, subtotal, items), `AddCartItemPayload` (variantId, quantity), `UpdateCartItemPayload` (quantity) |
| `catalog.ts` | Catalog types: `HeaderCategory` with nested `HeaderCategoryChild` (id, name, slug, sortOrder), `ProductListItem` (full product info with pricing and images), `PaginatedProducts` |
| `order.ts` | Order types: `OrderItem` (line item details), `UserOrder` (full order with address, status, amounts, timestamps, items, payment status), `PaginatedUserOrders` |
| `user.ts` | User profile types: `UserProfile` (id, fullName, email, phone, dateOfBirth, gender, avatarUrl, status), `UpdateProfilePayload` (fullName, phone, dateOfBirth, gender) |

## For AI Agents
### Type Dependency Graph
- `api.ts` is foundation: defines `ApiEnvelope` and `PaginatedApiEnvelope` used by all API modules
- `catalog.ts` and `order.ts` import `PaginatedApiEnvelope` from `api.ts`
- API modules (`src/api/`) import types from this directory
- Components and pages import types indirectly through API return types or directly when needed

### Naming Conventions
- Entity types: `PascalCase` noun (e.g., `Cart`, `UserOrder`, `AuthUser`)
- Payload types: `PascalCase` with `Payload` suffix (e.g., `CreateAddressPayload`, `UpdateProfilePayload`)
- Response types: `PascalCase` with `Response` suffix when needed (e.g., `PaginatedProducts`)
- All monetary values are `number` (VND amounts)
- Nullable fields use `| null` (not optional `?`) for API-provided data

### Usage Pattern
```ts
import type { Cart } from '../types/cart';
import type { AuthUser } from '../types/auth';
```