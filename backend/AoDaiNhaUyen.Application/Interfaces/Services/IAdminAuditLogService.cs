using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminAuditLogService
{
  Task<PagedResult<AdminAuditLogListItemDto>> SearchAsync(AdminAuditLogSearchRequest request, CancellationToken cancellationToken = default);
  Task<AdminAuditLogDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
  Task<AdminAuditLogStatsDto> GetStatsAsync(AdminAuditLogSearchRequest request, CancellationToken cancellationToken = default);
}
