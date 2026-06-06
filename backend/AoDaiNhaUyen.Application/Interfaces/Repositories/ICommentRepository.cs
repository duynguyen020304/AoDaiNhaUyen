using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Repositories;

public interface ICommentRepository
{
  Task<(IReadOnlyList<Comment> Items, int TotalCount)> GetByProductIdAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Comment>> GetRepliesAsync(
    Guid parentCommentId,
    CancellationToken cancellationToken = default);

  Task AddAsync(Comment comment, CancellationToken cancellationToken = default);

  Task<(IReadOnlyList<Comment> Items, int TotalCount)> GetRatedByProductIdAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

  Task<bool> HasUserRatedAsync(
    Guid userId,
    Guid productId,
    CancellationToken cancellationToken = default);

  Task<ReviewSummaryData?> GetReviewSummaryAsync(
    Guid productId,
    CancellationToken cancellationToken = default);
}

public sealed record ReviewSummaryData(
  double AverageRating,
  int TotalReviews,
  IReadOnlyDictionary<int, int> RatingDistribution);
