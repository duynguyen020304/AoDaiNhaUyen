using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public enum AdminLlmRole { System, User, Assistant, ToolCall, ToolResponse }

public sealed record AdminLlmMessage(
  AdminLlmRole Role,
  string Content,
  string? ToolName = null,
  string? ToolCallId = null,
  string? ToolResponseJson = null);

public sealed record LlmChunk(
  [property: JsonPropertyName("type")] string Type,
  [property: JsonPropertyName("content")] string Content,
  [property: JsonPropertyName("toolName")] string? ToolName = null,
  [property: JsonPropertyName("toolCallId")] string? ToolCallId = null);

public sealed record ToolDefinition(
  string Name,
  string Description,
  IReadOnlyDictionary<string, object?> Parameters);

/// <summary>Abstraction for LLM providers used by the admin AI agent.</summary>
public interface IAdminLlmProvider
{
  /// <summary>
  /// Streams chat completions with tool-calling support.
  /// Yields text chunks, tool call deltas, and tool result markers.
  /// </summary>
  IAsyncEnumerable<LlmChunk> StreamChatAsync(
    List<AdminLlmMessage> history,
    IReadOnlyList<ToolDefinition> tools,
    CancellationToken ct);
}
