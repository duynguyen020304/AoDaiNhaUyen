using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Subscriber : BaseEntity
{
  public required string Email { get; set; }
  public Guid? UserId { get; set; }
  public string Status { get; set; } = "pending";
  public DateTime? SubscribedAt { get; set; }
  public DateTime? UnsubscribedAt { get; set; }
  public required string UnsubscribeToken { get; set; }
  public required string ConfirmationToken { get; set; }
  public DateTime? ConfirmedAt { get; set; }
  public DateTime? LastSentAt { get; set; }
  public DateTime? LastOpenAt { get; set; }
  public DateTime? LastClickAt { get; set; }

  public User? User { get; set; }
  public ICollection<MarketingConsent> Consents { get; set; } = new List<MarketingConsent>();
}
