using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class AiTryOnFeedback : BaseEntity
{
  public Guid UserGeneratedImageId { get; set; }
  public Guid? UserId { get; set; }
  public string? GuestKeyHash { get; set; }
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public string? AdminNote { get; set; }
  public bool IsResolved { get; set; }

  public UserGeneratedImage UserGeneratedImage { get; set; } = null!;
  public User? User { get; set; }
}
