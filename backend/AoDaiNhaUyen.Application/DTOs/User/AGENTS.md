<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/User

## Purpose
User-facing profile, address, and order history DTOs. Consumed by `IUserService` and the User API controller. These are the customer's own-account views — separate from admin user management DTOs in `DTOs/Admin/`.

## Key Files
| File | Description |
|------|-------------|
| `UserProfileDto.cs` | Profile read model: `FullName`, `Email`, `Phone`, `Gender`, `DateOfBirth`, `AvatarUrl` |
| `UpdateUserProfileDto.cs` | Profile update request: `FullName`, `Phone`, `Gender`, `DateOfBirth` |
| `UserAddressDto.cs` | Saved address: `Id`, `RecipientName`, `Phone`, `Province`, `District`, `Ward`, `AddressLine`, `IsDefault` |
| `CreateAddressDto.cs` | Create address request: all address fields + `IsDefault` |
| `UserOrderDto.cs` | Order in order history: `OrderCode`, `Status`, subtotals, `Items` (list of `OrderItemDto`), `CreatedAt`, `UpdatedAt` |
| `OrderItemDto.cs` | Order line item: `ProductName`, `Sku`, `Size`, `Color`, `UnitPrice`, `Quantity`, `LineTotal` |

## For AI Agents

### Working In This Directory
- Address fields use Vietnamese administrative hierarchy: `Province` → `District` → `Ward` → `AddressLine`
- `IsDefault` on `UserAddressDto` and `CreateAddressDto` controls which address pre-fills at checkout
- `UserOrderDto` is a read-only history view — order mutations go through `IAdminOrderService` or `ICheckoutService`
- `OrderItemDto.LineTotal` = `UnitPrice * Quantity` — computed server-side

## Dependencies
### Internal
- `IUserService` — returns `UserProfileDto`, `UserAddressDto` list, `UserOrderDto` list; consumes update/create requests
- `IUserProfileRepository` — underlying data access for user profile and address CRUD

<!-- MANUAL: -->
