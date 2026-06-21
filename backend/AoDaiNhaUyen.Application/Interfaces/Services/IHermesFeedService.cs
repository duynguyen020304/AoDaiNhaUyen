using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesFeedService
{
  Task<HermesFeedSnapshotResponse> GetRecentFeedAsync(int maxItems, CancellationToken cancellationToken);
  Task<HermesFeedSnapshotResponse> GetRecentFeedAsync(int maxItems, DateTimeOffset? startDateUtc, DateTimeOffset? endDateUtc, CancellationToken cancellationToken);
  Task<HermesFeedHeartbeatResponse?> GetLatestHeartbeatAsync(CancellationToken cancellationToken);
}
