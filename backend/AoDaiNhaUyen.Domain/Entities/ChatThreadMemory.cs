namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ChatThreadMemory
{
  public long ThreadId { get; set; }
  public string? Summary { get; set; }
  public string? FactsJsonb { get; set; }
  public string? ResolvedRefsJsonb { get; set; }
  public long? LastMessageId { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public ChatThread Thread { get; set; } = null!;
  public ChatMessage? LastMessage { get; set; }
}
