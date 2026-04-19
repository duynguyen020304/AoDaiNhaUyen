namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ChatMessage
{
  public long Id { get; set; }
  public long ThreadId { get; set; }
  public string Role { get; set; } = "user";
  public string Content { get; set; } = string.Empty;
  public string? Intent { get; set; }
  public string? ClientMessageId { get; set; }
  public string? PromptVersion { get; set; }
  public string? UsageJsonb { get; set; }
  public string? FinishReason { get; set; }
  public string? ToolCallsJsonb { get; set; }
  public string? StructuredPayloadJsonb { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public ChatThread Thread { get; set; } = null!;
  public ICollection<ChatAttachment> Attachments { get; set; } = new List<ChatAttachment>();
}
