using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IStylistResponseComposer
{
  IAsyncEnumerable<string> ComposeStreamAsync(
    string userMessage,
    string fallbackText,
    string intent,
    string? memorySummary,
    ChatStructuredPayloadDto? structuredPayload,
    string? previousUserMessage = null,
    string? previousAssistantMessage = null,
    CancellationToken cancellationToken = default);

  Task<string> ComposeAsync(
    string userMessage,
    string fallbackText,
    string intent,
    string? memorySummary,
    ChatStructuredPayloadDto? structuredPayload,
    string? previousUserMessage = null,
    string? previousAssistantMessage = null,
    CancellationToken cancellationToken = default);
}
