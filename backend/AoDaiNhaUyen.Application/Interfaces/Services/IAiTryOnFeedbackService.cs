using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAiTryOnFeedbackService
{
  Task<AiTryOnFeedbackDto> CreateAsync(
    Guid? userId,
    string? guestKeyHash,
    CreateAiTryOnFeedbackDto request,
    CancellationToken cancellationToken = default);

  Task<PagedResult<AdminAiTryOnFeedbackDto>> GetForAdminAsync(
    int page,
    int pageSize,
    int? rating,
    bool? isResolved,
    CancellationToken cancellationToken = default);

  Task<AdminAiTryOnFeedbackDto?> UpdateStatusAsync(
    Guid id,
    UpdateAiTryOnFeedbackStatusDto request,
    CancellationToken cancellationToken = default);
}
