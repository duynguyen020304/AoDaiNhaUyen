using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAiTryOnService
{
  Task<AiTryOnResultDto> GenerateAsync(
    AiTryOnRequestDto request,
    CancellationToken cancellationToken = default);
}
