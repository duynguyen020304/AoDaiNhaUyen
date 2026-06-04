namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ImageCatalogEntry(
    Guid AttachmentId,
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
  public List<Guid> ShortlistedProductIds { get; set; } = [];
  public List<Guid> GarmentShortlistedProductIds { get; set; } = [];
  public List<Guid> AccessoryShortlistedProductIds { get; set; } = [];
  public List<Guid> ShownProductIds { get; set; } = [];
  public List<Guid> ShownGarmentProductIds { get; set; } = [];
  public List<Guid> ShownAccessoryProductIds { get; set; } = [];
  public List<string> ShownOutfitSignatures { get; set; } = [];
  public List<Guid> RejectedProductIds { get; set; } = [];
  public List<Guid> LikedProductIds { get; set; } = [];
  public string? LastRecommendationStrategy { get; set; }
  public Guid? SelectedGarmentProductId { get; set; }
  public List<Guid> SelectedAccessoryProductIds { get; set; } = [];
  public Guid? LatestPersonAttachmentId { get; set; }
  public Guid? LatestTryOnResultAttachmentId { get; set; }
  public Guid? LatestTryOnResultMessageId { get; set; }
  public List<string> PendingTryOnRequirements { get; set; } = [];
  public List<string> RecentUserMessages { get; set; } = [];
  public List<string> RecentAssistantMessages { get; set; } = [];
  public string? UserConversationSummary { get; set; }
  public string? AssistantConversationSummary { get; set; }
}
