using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ChatThreadMemory : BaseEntity
{
  public Guid ThreadId { get; set; }
  public string? Summary { get; set; }
  public string? FactsJsonb { get; set; }
  public string? ResolvedRefsJsonb { get; set; }
  public Guid? LastMessageId { get; set; }

  public ChatThread Thread { get; set; } = null!;
  public ChatMessage? LastMessage { get; set; }
}
