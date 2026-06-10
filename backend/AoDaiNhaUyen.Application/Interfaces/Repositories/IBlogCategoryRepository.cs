using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface IBlogCategoryRepository
{
  Task<IReadOnlyList<BlogCategory>> GetPublicAsync(CancellationToken cancellationToken = default);
  Task<BlogCategory?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
  Task<BlogCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
