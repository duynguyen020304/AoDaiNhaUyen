using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminMediaService
{
  Task<UserImageListDto> GetAllAsync(
    int page,
    int pageSize,
    string? sourceType,
    string? search,
    CancellationToken ct = default);

  Task<UserImageDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

  Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

  Task<MediaStatsDto> GetStatsAsync(CancellationToken ct = default);
}
