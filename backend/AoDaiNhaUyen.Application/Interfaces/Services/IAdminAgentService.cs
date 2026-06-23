using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>
/// Orchestrates AI agent conversations for the admin panel.
/// Handles tool registry, safety classification, and LLM streaming.
/// </summary>
public interface IAdminAgentService
{
  /// <summary>Stream a chat conversation with tool-calling.</summary>
  IAsyncEnumerable<LlmChunk> StreamChatAsync(
    AdminAiChatRequest request,
    Guid adminUserId,
    CancellationToken ct);

  /// <summary>Confirm or reject a pending action.</summary>
  Task<bool> ConfirmActionAsync(string actionId, bool approved, Guid adminUserId, CancellationToken ct);

  /// <summary>Get proactive suggestions for the dashboard.</summary>
  Task<IReadOnlyList<AdminAiSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct);

  /// <summary>Run safe diagnostics for read tools and confirmation gates.</summary>
  Task<AdminToolDiagnosticsResponse> RunDiagnosticsAsync(
    AdminToolDiagnosticsRequest request,
    Guid adminUserId,
    CancellationToken ct);
}
