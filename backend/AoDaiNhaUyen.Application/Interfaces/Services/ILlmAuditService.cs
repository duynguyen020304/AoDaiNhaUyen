using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ILlmAuditService
{
  Task<Guid> LogStartedAsync(LlmAuditLogCreateDto request, CancellationToken cancellationToken = default);
  Task LogCompletedAsync(Guid id, LlmAuditLogUpdateDto request, CancellationToken cancellationToken = default);
  Task LogFailedAsync(Guid id, string errorCode, CancellationToken cancellationToken = default);
  Task<PagedResult<LlmAuditLogListItemDto>> SearchAsync(LlmAuditLogSearchRequest request, CancellationToken cancellationToken = default);
  Task<LlmAuditLogDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
  Task<LlmAuditLogStatsDto> GetStatsAsync(LlmAuditLogSearchRequest request, CancellationToken cancellationToken = default);
}
