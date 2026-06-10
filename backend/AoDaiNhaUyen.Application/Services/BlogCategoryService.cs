using AoDaiNhaUyen.Application.Constants;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Application.Services;

public sealed class BlogCategoryService(IBlogCategoryRepository blogCategoryRepository, IFusionCacheService cache) : IBlogCategoryService
{
  public async Task<IReadOnlyList<BlogCategoryDto>> GetPublicAsync(CancellationToken cancellationToken = default)
    => await cache.GetOrSetAsync(
      "blog:categories:public",
      GetPublicCoreAsync,
      tags: [CacheTags.Blog],
      duration: TimeSpan.FromMinutes(30),
      token: cancellationToken) ?? [];

  private async Task<IReadOnlyList<BlogCategoryDto>> GetPublicCoreAsync(CancellationToken cancellationToken)
  {
    var categories = await blogCategoryRepository.GetPublicAsync(cancellationToken);
    return categories
      .Select(category => new BlogCategoryDto(
        category.Id,
        category.Name,
        category.Slug,
        category.Description,
        category.SortOrder,
        category.Posts.Count))
      .ToList();
  }
}
