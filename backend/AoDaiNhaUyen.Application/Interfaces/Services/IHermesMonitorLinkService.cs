using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesMonitorLinkService
{
  Task<HermesMonitorLinkResponse> CreateLinkAsync(CreateHermesMonitorLinkRequest request, Guid? adminUserId, string publicBaseUrl, CancellationToken cancellationToken);
  Task<bool> RevokeLinkAsync(Guid id, CancellationToken cancellationToken);
  Task<HermesMonitorSnapshotResponse?> GetSnapshotAsync(string token, CancellationToken cancellationToken);
}
