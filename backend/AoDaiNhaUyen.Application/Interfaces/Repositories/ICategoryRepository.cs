using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface ICategoryRepository
{
  Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default);
}
