namespace AoDaiNhaUyen.Application.DTOs;

public sealed class ThreadMemoryStateDto
{
  public string? Scenario { get; set; }
  public decimal? BudgetCeiling { get; set; }
  public string? ColorFamily { get; set; }
  public string? MaterialKeyword { get; set; }
  public List<long> ShortlistedProductIds { get; set; } = [];
  public long? SelectedGarmentProductId { get; set; }
  public List<long> SelectedAccessoryProductIds { get; set; } = [];
  public long? LatestPersonAttachmentId { get; set; }
  public long? LatestTryOnResultAttachmentId { get; set; }
  public long? LatestTryOnResultMessageId { get; set; }
  public List<string> PendingTryOnRequirements { get; set; } = [];
}
