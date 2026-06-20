<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/Dashboard

## Purpose
Admin analytics DTOs powering the dashboard charts and KPI cards. All types are `sealed record`. Consumed by `IAdminDashboardService` and the admin dashboard API endpoint.

## Key Files
| File | Description |
|------|-------------|
| `DashboardDtos.cs` | All dashboard DTOs in one file: |
| | `DashboardSummaryDto` — TotalRevenue, TotalOrders, TotalUsers, TotalProducts + growth percentages |
| | `RevenuePointDto` — single time-series point: Date, Revenue, Orders |
| | `RevenueDataDto` — period label + list of `RevenuePointDto` |
| | `OrderStatusDistributionDto` — counts per status: Pending, Confirmed, Processing, Shipping, Completed, Cancelled, Returned |
| | `RecentOrderDto` — recent order row: Id, OrderCode, CustomerName, TotalAmount, Status, CreatedAt |
| | `TopProductDto` — bestseller row: ProductId?, ProductName, ImageUrl?, SoldCount, Revenue |
| | `UserGrowthPointDto` — user growth point: Date, NewUsers, TotalUsers |
| | `UserGrowthDataDto` — list of `UserGrowthPointDto` |

## For AI Agents

### Working In This Directory
- All monetary values use `decimal` (Vietnamese dong, no fractional cents needed)
- Growth fields (e.g., `RevenueGrowth`) are percentage doubles — positive means growth, negative means decline
- `TopProductDto.ProductId` is nullable to handle deleted/archived products that still appear in historical data
- Dashboard data is cached with the `CacheTags.Dashboard` tag and invalidated on order/product/user mutations

## Dependencies
### Internal
- `IAdminDashboardService` — returns these DTOs
- Cache: invalidated via `ICacheInvalidationService.InvalidateDashboardCacheAsync()`

<!-- MANUAL: -->
