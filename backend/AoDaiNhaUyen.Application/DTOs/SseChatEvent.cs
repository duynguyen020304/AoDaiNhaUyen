namespace AoDaiNhaUyen.Application.DTOs;

public abstract record SseChatEvent
{
  public sealed record Created(
    long MessageId,
    string Role,
    string Content,
    string? Intent,
    DateTime CreatedAt,
    IReadOnlyList<ChatAttachmentDto> Attachments,
    ChatStructuredPayloadDto? StructuredPayload) : SseChatEvent;

  public sealed record Queued(int Position) : SseChatEvent;

  public sealed record TextDelta(string Delta) : SseChatEvent;

  public sealed record TextDone(string FullText, long MessageId, DateTime CreatedAt) : SseChatEvent;

  public sealed record StreamError(string Code, string Message) : SseChatEvent;

  public sealed record Done() : SseChatEvent;
}
