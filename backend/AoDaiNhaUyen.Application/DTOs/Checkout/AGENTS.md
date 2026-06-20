<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/Checkout

## Purpose
Order placement DTOs used during the checkout flow. `ICheckoutService.CheckoutAsync()` accepts a `CheckoutRequestDto` and returns a `CheckoutResultDto`.

## Key Files
| File | Description |
|------|-------------|
| `CheckoutAddressDto.cs` | Shipping address: `RecipientName`, `Phone`, `Province`, `District`, `Ward`, `AddressLine` |
| `CheckoutRequestDto.cs` | Full checkout payload: `Address` (CheckoutAddressDto), `Items` (cart items), `Note`, optional promo code |
| `CheckoutResultDto.cs` | Order created result: `OrderCode`, `OrderId` |

## For AI Agents

### Working In This Directory
- Address fields are Vietnamese address components — `Province`/`District`/`Ward` are administrative division names, not IDs
- `CheckoutRequestDto` carries the snapshot of items and address at checkout time; the service validates stock before creating the order
- `CheckoutResultDto` returns only the order identifier — full order detail is fetched separately via `IOrderService`

## Dependencies
### Internal
- `ICheckoutService` — consumes `CheckoutRequestDto`, returns `CheckoutResultDto`

<!-- MANUAL: -->
