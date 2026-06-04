namespace AoDaiNhaUyen.Application.DTOs;

public sealed record IntentClassificationDto(
  string Intent,
  string? Scenario,
  decimal? BudgetCeiling,
  string? ColorFamily,
  string? MaterialKeyword,
  string? ProductType,
  IReadOnlyList<Guid> ReferencedProductIds,
  bool RequiresPersonImage,
  bool HasImageAttachments = false,
  string? ResponseMode = null,
  bool NeedsCatalogLookup = false,
  bool NeedsClarification = false,
  string? RetrievalQuery = null,
  string? SelectionStrategy = null,
  string? StylistBrief = null,
  string? ReferencedImageHint = null,
  string? ProductReferenceScope = null,
  bool WantsDifferentOptions = false,
  bool HasSpecificAccessoryRequest = false)
{
  public static IntentClassificationDto Clarification(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType = null,
    IReadOnlyList<Guid>? referencedProductIds = null,
    bool requiresPersonImage = false,
    bool hasImageAttachments = false,
    string? stylistBrief = null) =>
    new(
      "clarification",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      referencedProductIds ?? [],
      requiresPersonImage,
      hasImageAttachments,
      NeedsClarification: true,
      StylistBrief: stylistBrief);

  public bool UsesPromptDecisions =>
    !string.IsNullOrWhiteSpace(SelectionStrategy) ||
    !string.IsNullOrWhiteSpace(ReferencedImageHint) ||
    !string.IsNullOrWhiteSpace(ProductReferenceScope) ||
    WantsDifferentOptions ||
    HasSpecificAccessoryRequest;
}
