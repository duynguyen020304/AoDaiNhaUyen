using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICachedImageValidationService
{
  Task<ImageValidationResultDto> ValidatePersonImageAsync(
    byte[] imageBytes,
    string mimeType,
    string? fileName = null,
    CancellationToken cancellationToken = default);
}
