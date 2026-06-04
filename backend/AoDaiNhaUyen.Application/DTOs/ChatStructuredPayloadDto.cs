namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ChatStructuredPayloadDto(
  string Kind,
  string? Scenario,
  bool CanTryOn,
  bool RequiresPersonImage,
  Guid? SelectedGarmentProductId,
  IReadOnlyList<Guid> SelectedAccessoryProductIds,
  IReadOnlyList<string> PendingTryOnRequirements,
  IReadOnlyList<ChatRecommendationItemDto> Products,
  IReadOnlyList<ChatRecommendationItemDto>? GarmentProducts = null,
  IReadOnlyList<ChatRecommendationItemDto>? AccessoryProducts = null);
