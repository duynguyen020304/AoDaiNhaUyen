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
}
