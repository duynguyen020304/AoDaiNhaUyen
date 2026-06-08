namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin inventory and store health service for AI agent.</summary>
public interface IAdminInventoryService
{
  /// <summary>Get inventory summary with low-stock alerts.</summary>
  Task<InventorySummary> GetInventorySummaryAsync(int threshold = 10, CancellationToken ct = default);

  /// <summary>Get store health score (0-100) based on multiple metrics.</summary>
  Task<StoreHealthScore> GetStoreHealthScoreAsync(CancellationToken ct = default);
}

public sealed record InventorySummary(
  int TotalProducts,
  int TotalVariants,
  int LowStockCount,
  int OutOfStockCount,
  IReadOnlyList<LowStockItem> LowStockItems);

public sealed record LowStockItem(
  Guid ProductId,
  string ProductName,
  string? Sku,
  string? Size,
  string? Color,
  int StockQty);

public sealed record StoreHealthScore(
  int Overall,
  int FulfillmentRate,
  int StockHealth,
  int RevenueTrend,
  int CustomerSatisfaction,
  string Summary);
