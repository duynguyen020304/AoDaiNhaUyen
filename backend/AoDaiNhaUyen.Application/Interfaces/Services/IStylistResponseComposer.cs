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
    CancellationToken cancellationToken = default);

  Task<string> ComposeAsync(
    string userMessage,
    string fallbackText,
    string intent,
    string? memorySummary,
    ChatStructuredPayloadDto? structuredPayload,
    CancellationToken cancellationToken = default);
}
