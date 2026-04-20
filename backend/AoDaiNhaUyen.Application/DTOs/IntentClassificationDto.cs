namespace AoDaiNhaUyen.Application.DTOs;

public sealed record IntentClassificationDto(
  string Intent,
  string? Scenario,
  decimal? BudgetCeiling,
  string? ColorFamily,
  string? MaterialKeyword,
  IReadOnlyList<long> ReferencedProductIds,
  bool RequiresPersonImage,
  bool HasImageAttachments = false)
{
  public static IntentClassificationDto Clarification(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    IReadOnlyList<long>? referencedProductIds = null,
    bool requiresPersonImage = false,
    bool hasImageAttachments = false) =>
    new(
      "clarification",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      referencedProductIds ?? [],
      requiresPersonImage,
      hasImageAttachments);
}
