<!-- Parent: ../../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components/dashboard

## Purpose
Dashboard widget components used exclusively by `DashboardPage`. Each widget receives its data as props from the page; none fetch data independently.

## Key Files
| File | Description |
|------|-------------|
| `StatsCard.tsx` | Single KPI card: label, value, optional trend indicator |
| `StatsCardGrid.tsx` | Grid layout wrapper that renders multiple `StatsCard` items |
| `RevenueChart.tsx` | Line/area chart of revenue over time; uses Recharts |
| `OrdersByStatusChart.tsx` | Pie or bar chart of orders grouped by status; uses Recharts |
| `UserGrowthChart.tsx` | Line chart of new user registrations over time; uses Recharts |
| `RecentOrdersTable.tsx` | Table of the most recent orders with order ID, customer, total, status |
| `TopProductsList.tsx` | Ranked list of top-selling products by revenue or quantity |
| `LowStockAlerts.tsx` | List of products with stock below threshold |

## For AI Agents
### Working In This Directory
- All components are purely presentational — data comes from `DashboardPage` via props.
- Chart components wrap Recharts primitives (`LineChart`, `BarChart`, `PieChart`, `ResponsiveContainer`).
- Period selector (7/30/90 days) is controlled by `DashboardPage`; it passes the correct data slice to each chart.
- Do not add `useEffect` or store calls inside these components.

### Common Patterns
- Recharts charts need `<ResponsiveContainer width="100%" height={N}>` wrapper.
- Loading skeleton: accept an `isLoading?: boolean` prop and render placeholder bars/shimmer.
- Format currency in Vietnamese dong (VND); use `Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })`.

## Dependencies
### Internal
- `@/types/dashboard` — chart data shapes, summary DTO
- `@/components/ui/card` — Card primitive for widget containers

### External
- Recharts (`LineChart`, `BarChart`, `PieChart`, `ResponsiveContainer`, `Tooltip`, `Legend`, etc.)
- lucide-react (trend icons)

<!-- MANUAL: -->
