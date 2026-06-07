namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record AdminAiChatRequest(string Message, string? ConversationId);

public sealed record AdminAiConfirmRequest(string ActionId, bool Approved);

public sealed record AdminAiSuggestionResponse(string Id, string Title, string Description, string? Route);

public sealed record AdminPendingAction(
  string ActionId,
  string ToolName,
  string Description,
  string RiskLevel,
  DateTime RequestedAt);
