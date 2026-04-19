using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IStylistResponseComposer
{
  Task<string> ComposeAsync(
    string userMessage,
    string fallbackText,
    string intent,
    string? memorySummary,
    ChatStructuredPayloadDto? structuredPayload,
    CancellationToken cancellationToken = default);
}
