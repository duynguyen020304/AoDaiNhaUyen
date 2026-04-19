namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatStructuredPayloadDto(
  string Kind,
  string? Scenario,
  bool CanTryOn,
  bool RequiresPersonImage,
  long? SelectedGarmentProductId,
  IReadOnlyList<long> SelectedAccessoryProductIds,
  IReadOnlyList<string> PendingTryOnRequirements,
  IReadOnlyList<ChatRecommendationItemDto> Products);
