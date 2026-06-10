using AoDaiNhaUyen.Application.DTOs.BlogPost;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IBlogCategoryService
{
  Task<IReadOnlyList<BlogCategoryDto>> GetPublicAsync(CancellationToken cancellationToken = default);
}
