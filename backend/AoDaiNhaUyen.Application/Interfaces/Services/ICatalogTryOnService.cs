using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICatalogTryOnService
{
  Task<AiTryOnCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default);

  Task<AiTryOnResultDto> CreateAsync(
    CatalogAiTryOnRequestDto request,
    CancellationToken cancellationToken = default);
}
