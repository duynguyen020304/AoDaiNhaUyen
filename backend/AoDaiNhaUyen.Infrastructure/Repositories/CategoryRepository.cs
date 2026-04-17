using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
  public async Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default)
  {
    return await dbContext.Categories
      .AsNoTracking()
      .Where(c => c.IsActive)
      .OrderBy(c => c.Parent)
      .ThenBy(c => c.SortOrder)
      .ThenBy(c => c.Name)
      .ToListAsync(cancellationToken);
  }
}
