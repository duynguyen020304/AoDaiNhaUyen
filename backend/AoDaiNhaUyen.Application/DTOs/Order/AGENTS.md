<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/Order

## Purpose
Admin order management DTOs for updating order status, creating shipments, and updating shipment status. These are write-side (command) DTOs; order read models live in `DTOs/User/` (`UserOrderDto`) and `DTOs/Admin/` (admin order responses).

## Key Files
| File | Description |
|------|-------------|
| `UpdateOrderStatusRequest.cs` | Status transition request: `Status` (string — e.g., `"confirmed"`, `"shipping"`, `"completed"`) |
| `OrderUpdateResult.cs` | Result of an order status update: `Succeeded`, `ErrorCode?`, `ErrorMessage?` |

Note: `UpdateOrderStatusRequest.cs` also contains `CreateShipmentRequest` (Carrier, TrackingNumber) and `UpdateShipmentStatusRequest` (Status).

## For AI Agents

### Working In This Directory
- Status values are string constants matching the `OrderStatus` domain enum — validate against allowed transitions in `IAdminOrderService`
- `OrderUpdateResult` follows the standard result pattern consistent with `AdminMutationResult`
- Shipment DTOs are co-located in `UpdateOrderStatusRequest.cs` for compactness — if this file grows, split into separate files

## Dependencies
### Internal
- `IAdminOrderService` — consumes these request DTOs; returns `OrderUpdateResult`

<!-- MANUAL: -->
