namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ImageCatalogEntry(
    long AttachmentId,
    string Kind,
    string Label,
    string? Description);

public sealed class ThreadMemoryStateDto
{
  public List<ImageCatalogEntry> ImageCatalog { get; set; } = [];
  public string? Scenario { get; set; }
  public decimal? BudgetCeiling { get; set; }
  public string? ColorFamily { get; set; }
  public string? MaterialKeyword { get; set; }
  public string? ConversationStage { get; set; }
  public string? LastUserRequestType { get; set; }
  public List<long> ShortlistedProductIds { get; set; } = [];
  public List<long> GarmentShortlistedProductIds { get; set; } = [];
  public List<long> AccessoryShortlistedProductIds { get; set; } = [];
  public List<long> ShownProductIds { get; set; } = [];
  public List<long> ShownGarmentProductIds { get; set; } = [];
  public List<long> ShownAccessoryProductIds { get; set; } = [];
  public List<string> ShownOutfitSignatures { get; set; } = [];
  public List<long> RejectedProductIds { get; set; } = [];
  public List<long> LikedProductIds { get; set; } = [];
  public string? LastRecommendationStrategy { get; set; }
  public long? SelectedGarmentProductId { get; set; }
  public List<long> SelectedAccessoryProductIds { get; set; } = [];
  public long? LatestPersonAttachmentId { get; set; }
  public long? LatestTryOnResultAttachmentId { get; set; }
  public long? LatestTryOnResultMessageId { get; set; }
  public List<string> PendingTryOnRequirements { get; set; } = [];
  public List<string> RecentUserMessages { get; set; } = [];
  public List<string> RecentAssistantMessages { get; set; } = [];
  public string? UserConversationSummary { get; set; }
  public string? AssistantConversationSummary { get; set; }
}
