namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record AdminAiChatRequest(string Message, string? ConversationId);

public sealed record AdminAiConfirmRequest(string ActionId, bool Approved);

public sealed record AdminAiSuggestionResponse(string Id, string Title, string Description, string? Route);

public sealed record AdminPendingAction(
  string ActionId,
  string ToolName,
  string Description,
  string RiskLevel,
  DateTime RequestedAt,
  string? ConversationId = null,
  string? ToolArgsJson = null,
  string? AssistantText = null,
  Guid? AdminUserId = null,
  string? ThoughtSignature = null);

public sealed record AdminConversationSummaryDto(Guid Id, string? Title, int MessageCount, string? LastMessagePreview, DateTime UpdatedAt);

public sealed record AdminConversationDetailDto(Guid Id, string? Title, List<AdminMessageDto> Messages, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record AdminMessageDto(string Role, string Content, string? ToolCallsJson, string? StructuredPayloadJson, DateTime CreatedAt);
