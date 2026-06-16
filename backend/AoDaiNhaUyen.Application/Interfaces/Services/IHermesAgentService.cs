using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesAgentService
{
  Task<HermesStatusResponse> GetStatusAsync(CancellationToken cancellationToken);

  Task RecordHeartbeatAsync(HermesHeartbeatRequest request, CancellationToken cancellationToken);

  IAsyncEnumerable<HermesStreamChunk> StreamChatAsync(
    HermesChatRequest request,
    Guid adminUserId,
    CancellationToken cancellationToken);

  Task<IReadOnlyList<HermesRunSummaryResponse>> ListRunsAsync(CancellationToken cancellationToken);
}
