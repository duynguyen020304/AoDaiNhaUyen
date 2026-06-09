namespace AoDaiNhaUyen.Application.DTOs.Marketing;

public sealed record PromoPerformanceDto(
  Guid PromoCodeId,
  string Code,
  int UsesCount,
  int OrdersCount,
  decimal GrossRevenue,
  decimal NetRevenue,
  decimal DiscountCost,
  decimal ShippingSubsidy,
  decimal TotalPromoCost,
  decimal EstimatedGrossProfitBeforePromo,
  decimal EstimatedGrossProfitAfterPromo,
  decimal MarginLoss,
  decimal AverageOrderValue);
