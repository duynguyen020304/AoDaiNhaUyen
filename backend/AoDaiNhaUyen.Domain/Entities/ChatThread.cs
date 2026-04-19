namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ChatThread
{
  public long Id { get; set; }
  public long? UserId { get; set; }
  public string? GuestKeyHash { get; set; }
  public string Status { get; set; } = "active";
  public string Source { get; set; } = "web";
  public DateTime? ClaimedAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public User? User { get; set; }
  public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
  public ICollection<ChatAttachment> Attachments { get; set; } = new List<ChatAttachment>();
  public ChatThreadMemory? Memory { get; set; }
}
