<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/Cart

## Purpose
Shopping cart DTOs for add, update, and view operations. Consumed by `ICartService` and the Cart API controller.

## Key Files
| File | Description |
|------|-------------|
| `AddCartItemDto.cs` | Add-to-cart request: `VariantId` (Guid), `Quantity` (int) |
| `UpdateCartItemDto.cs` | Update cart item quantity: `Quantity` (int) |
| `CartItemDto.cs` | Cart line item: `Id`, `VariantId`, `ProductName`, `Size`, `Color`, `Price`, `Quantity`, `LineTotal`, `ImageUrl` |
| `CartDto.cs` | Full cart response: `Id`, `Items` (IReadOnlyList\<CartItemDto\>), `Total` |

## For AI Agents

### Working In This Directory
- `CartDto.Total` is computed server-side from line items — do not pass a total from the client
- `CartItemDto.LineTotal` = `Price * Quantity` — calculated in the service, not stored
- Cart is user-scoped; the userId comes from the JWT claim in the controller, not from the DTO

### Common Patterns
```csharp
// Add item:
var request = new AddCartItemDto(VariantId: variantId, Quantity: 2);

// View cart:
CartDto cart = await cartService.GetCartAsync(userId, ct);
```

## Dependencies
### Internal
- `ICartService` — consumes `AddCartItemDto`, `UpdateCartItemDto`; returns `CartDto`

<!-- MANUAL: -->
