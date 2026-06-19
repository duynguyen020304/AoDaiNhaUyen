using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Repositories;

public sealed class CommentRepository(AppDbContext dbContext) : ICommentRepository
{
  public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> GetByProductIdAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Comments
      .AsNoTracking()
      .Include(c => c.User)
      .Where(c => c.ProductId == productId && c.ParentCommentId == null && c.IsVisible)
      .OrderByDescending(c => c.CreatedAt);

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(cancellationToken);

    return (items, totalCount);
  }

  public async Task<IReadOnlyList<Comment>> GetRepliesAsync(
    Guid parentCommentId,
    CancellationToken cancellationToken = default)
  {
    return await dbContext.Comments
      .AsNoTracking()
      .Include(c => c.User)
      .Where(c => c.ParentCommentId == parentCommentId && c.IsVisible)
      .OrderBy(c => c.CreatedAt)
      .ToListAsync(cancellationToken);
  }

  public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
  {
    dbContext.Comments.Add(comment);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Comment?> GetByIdWithProductAndUserAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    return await dbContext.Comments
      .AsNoTracking()
      .Include(c => c.User)
      .Include(c => c.Product)
      .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
  }

  public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> GetRatedByProductIdAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Comments
      .AsNoTracking()
      .Include(c => c.User)
      .Where(c => c.ProductId == productId && c.Rating != null && c.ParentCommentId == null && c.IsVisible)
      .OrderByDescending(c => c.CreatedAt);

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(cancellationToken);

    return (items, totalCount);
  }

  public async Task<bool> HasUserRatedAsync(
    Guid userId,
    Guid productId,
    CancellationToken cancellationToken = default)
  {
    return await dbContext.Comments
      .AsNoTracking()
      .AnyAsync(c => c.UserId == userId && c.ProductId == productId && c.ParentCommentId == null && c.Rating != null, cancellationToken);
  }

  public async Task<ReviewSummaryData?> GetReviewSummaryAsync(
    Guid productId,
    CancellationToken cancellationToken = default)
  {
    var ratings = await dbContext.Comments
      .AsNoTracking()
      .Where(c => c.ProductId == productId && c.Rating != null && c.ParentCommentId == null && c.IsVisible)
      .Select(c => c.Rating!.Value)
      .ToListAsync(cancellationToken);

    if (ratings.Count == 0) return null;

    var dist = new Dictionary<int, int>();
    for (var i = 5; i >= 1; i--) dist[i] = 0;
    foreach (var r in ratings) dist[r]++;

    return new ReviewSummaryData(ratings.Average(), ratings.Count, dist);
  }

  public async Task<IReadOnlyDictionary<Guid, ReviewSummaryData>> GetReviewSummariesAsync(
    IEnumerable<Guid> productIds,
    CancellationToken cancellationToken = default)
  {
    var idSet = productIds.ToHashSet();
    if (idSet.Count == 0) return new Dictionary<Guid, ReviewSummaryData>();

    var raw = await dbContext.Comments
      .AsNoTracking()
      .Where(c => idSet.Contains(c.ProductId)
        && c.Rating != null
        && c.ParentCommentId == null
        && c.IsVisible)
      .GroupBy(c => c.ProductId)
      .Select(g => new
      {
        ProductId = g.Key,
        Average = g.Average(c => c.Rating!.Value),
        Count = g.Count(),
        Rating1 = g.Count(c => c.Rating == 1),
        Rating2 = g.Count(c => c.Rating == 2),
        Rating3 = g.Count(c => c.Rating == 3),
        Rating4 = g.Count(c => c.Rating == 4),
        Rating5 = g.Count(c => c.Rating == 5),
      })
      .ToListAsync(cancellationToken);

    return raw.ToDictionary(
      r => r.ProductId,
      r => new ReviewSummaryData(
        r.Average,
        r.Count,
        new Dictionary<int, int>
        {
          [5] = r.Rating5,
          [4] = r.Rating4,
          [3] = r.Rating3,
          [2] = r.Rating2,
          [1] = r.Rating1,
        }));
  }
}
