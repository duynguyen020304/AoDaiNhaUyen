using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminBlogImageGenerationService
{
  Task<AdminGeneratedImageDto> GenerateAsync(
    string prompt,
    CancellationToken cancellationToken = default);
}
