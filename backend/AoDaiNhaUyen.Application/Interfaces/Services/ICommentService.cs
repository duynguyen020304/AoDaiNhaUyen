using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICommentService
{
  Task<PagedResult<CommentDto>> GetProductCommentsAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

  Task<CommentDto> CreateCommentAsync(
    Guid userId,
    Guid productId,
    CreateCommentRequest request,
    CancellationToken cancellationToken = default);

  Task<PagedResult<ReviewDto>> GetProductReviewsAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

  Task<ReviewSummaryDto?> GetReviewSummaryAsync(
    Guid productId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyDictionary<Guid, ReviewSummaryDto>> GetReviewSummariesAsync(
    IEnumerable<Guid> productIds,
    CancellationToken cancellationToken = default);

  Task<ReviewDto> CreateReviewAsync(
    Guid userId,
    Guid productId,
    CreateReviewRequest request,
    CancellationToken cancellationToken = default);
}
