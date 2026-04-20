namespace AoDaiNhaUyen.Application.DTOs;

public sealed record IntentClassificationDto(
  string Intent,
  string? Scenario,
  decimal? BudgetCeiling,
  string? ColorFamily,
  string? MaterialKeyword,
  string? ProductType,
  IReadOnlyList<long> ReferencedProductIds,
  bool RequiresPersonImage,
  bool HasImageAttachments = false)
{
  public static IntentClassificationDto Clarification(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType = null,
    IReadOnlyList<long>? referencedProductIds = null,
    bool requiresPersonImage = false,
    bool hasImageAttachments = false) =>
    new(
      "clarification",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      referencedProductIds ?? [],
      requiresPersonImage,
      hasImageAttachments);
}
