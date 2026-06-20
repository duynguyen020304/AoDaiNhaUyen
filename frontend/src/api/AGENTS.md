<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# api

## Purpose
Fetch-based backend clients. All modules use `client.ts` helpers and backend envelope types.

## Module Map
| File | Purpose |
|------|---------|
| `client.ts` | Base URL resolution, `request<T>()`, `requestPaginated<T>()`, `resolveAssetUrl()`, credentials/envelope handling |
| `auth.ts` | Login/register/logout/session/refresh/password reset/OAuth URL+callbacks |
| `catalog.ts` | Products/categories/catalog filters |
| `cart.ts` | Cart CRUD |
| `checkout.ts` | Checkout/place order |
| `user.ts` | Profile, addresses, orders |
| `aiTryon.ts` | Try-on catalog and FormData generation calls |
| `chat.ts` | AI stylist threads/messages/attachments/in-chat try-on |
| `blog.ts` | Blog list/detail/content queries |
| `comment.ts` | Product comments |
| `review.ts` | Product reviews |
| `events.ts` | Customer event tracking |
| `marketing.ts` | Newsletter/subscriber/unsubscribe/consent |
| `media.ts` | Media/asset helpers |

## Local Conventions
- `request<T>()` returns `data`; `requestPaginated<T>()` returns pagination envelope.
- All requests include cookies via shared client.
- FormData only for uploads/attachments; do not set JSON content-type manually for FormData.
- Error messages should surface backend Vietnamese `message` when available.
- Regional API base URL logic belongs only in `client.ts`.
