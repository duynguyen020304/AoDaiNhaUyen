namespace AoDaiNhaUyen.Application.DTOs;

public sealed record IntentClassificationDto(
  string Intent,
  string? Scenario,
  decimal? BudgetCeiling,
  string? ColorFamily,
  string? MaterialKeyword,
  IReadOnlyList<long> ReferencedProductIds,
  bool RequiresPersonImage);
