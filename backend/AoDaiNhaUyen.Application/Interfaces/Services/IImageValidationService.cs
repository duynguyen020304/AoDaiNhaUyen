using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IImageValidationService
{
  Task<ImageValidationResultDto> ValidatePersonImageAsync(
    byte[] imageBytes,
    string mimeType,
    CancellationToken cancellationToken = default);
}
