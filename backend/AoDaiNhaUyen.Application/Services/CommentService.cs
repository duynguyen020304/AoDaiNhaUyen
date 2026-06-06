using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Services;

public sealed class CommentService(
  ICommentRepository commentRepository) : ICommentService
{
  public async Task<PagedResult<CommentDto>> GetProductCommentsAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    var validPage = page <= 0 ? 1 : page;
    var validPageSize = pageSize is <= 0 or > 50 ? 10 : pageSize;

    var (items, totalCount) = await commentRepository.GetByProductIdAsync(
      productId, validPage, validPageSize, cancellationToken);

    var result = new List<CommentDto>();
    foreach (var item in items)
    {
      result.Add(await MapCommentToDtoAsync(item, cancellationToken));
    }

    return new PagedResult<CommentDto>(result, totalCount, validPage, validPageSize);
  }

  public async Task<CommentDto> CreateCommentAsync(
    Guid userId,
    Guid productId,
    CreateCommentRequest request,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(request.Content))
    {
      throw new ArgumentException("Nội dung bình luận không được để trống.");
    }

    if (request.Rating.HasValue && (request.Rating < 1 || request.Rating > 5))
    {
      throw new ArgumentException("Đánh giá phải từ 1 đến 5 sao.");
    }

    // Top-level rating: one per user per product
    if (request.ParentCommentId == null && request.Rating.HasValue)
    {
      var alreadyRated = await commentRepository.HasUserRatedAsync(userId, productId, cancellationToken);
      if (alreadyRated)
      {
        throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này rồi.");
      }
    }

    var comment = new Comment
    {
      UserId = userId,
      ProductId = productId,
      ParentCommentId = request.ParentCommentId,
      Content = request.Content.Trim(),
      Rating = request.Rating,
      IsVisible = true
    };

    await commentRepository.AddAsync(comment, cancellationToken);

    return new CommentDto(
      comment.Id,
      userId,
      "",
      null,
      comment.Content,
      comment.Rating,
      comment.ParentCommentId,
      comment.CreatedAt,
      []);
  }

  public async Task<PagedResult<ReviewDto>> GetProductReviewsAsync(
    Guid productId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
  {
    var validPage = page <= 0 ? 1 : page;
    var validPageSize = pageSize is <= 0 or > 50 ? 10 : pageSize;

    var (items, totalCount) = await commentRepository.GetRatedByProductIdAsync(
      productId, validPage, validPageSize, cancellationToken);

    var result = items.Select(c => new ReviewDto(
      c.Id,
      c.UserId,
      c.User.FullName,
      c.User.AvatarUrl,
      c.Rating!.Value,
      c.Content,
      c.CreatedAt)).ToList();

    return new PagedResult<ReviewDto>(result, totalCount, validPage, validPageSize);
  }

  public async Task<ReviewSummaryDto?> GetReviewSummaryAsync(
    Guid productId,
    CancellationToken cancellationToken = default)
  {
    var summary = await commentRepository.GetReviewSummaryAsync(productId, cancellationToken);
    if (summary is null) return null;

    return new ReviewSummaryDto(
      summary.AverageRating,
      summary.TotalReviews,
      summary.RatingDistribution);
  }

  public async Task<IReadOnlyDictionary<Guid, ReviewSummaryDto>> GetReviewSummariesAsync(
    IEnumerable<Guid> productIds,
    CancellationToken cancellationToken = default)
  {
    var summaries = await commentRepository.GetReviewSummariesAsync(productIds, cancellationToken);
    return summaries.ToDictionary(
      kvp => kvp.Key,
      kvp => new ReviewSummaryDto(
        kvp.Value.AverageRating,
        kvp.Value.TotalReviews,
        kvp.Value.RatingDistribution));
  }

  public async Task<ReviewDto> CreateReviewAsync(
    Guid userId,
    Guid productId,
    CreateReviewRequest request,
    CancellationToken cancellationToken = default)
  {
    // Forwarded to CreateCommentAsync with rating
    var commentRequest = new CreateCommentRequest(request.Comment ?? "", request.Rating);
    return await CreateCommentAsReviewAsync(userId, productId, commentRequest, cancellationToken);
  }

  private async Task<ReviewDto> CreateCommentAsReviewAsync(
    Guid userId, Guid productId, CreateCommentRequest request, CancellationToken ct)
  {
    // Same as CreateCommentAsync but returns ReviewDto
    if (request.Rating is < 1 or > 5)
      throw new ArgumentException("Đánh giá phải từ 1 đến 5 sao.");

    var alreadyRated = await commentRepository.HasUserRatedAsync(userId, productId, ct);
    if (alreadyRated)
      throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này rồi.");

    var comment = new Comment
    {
      UserId = userId,
      ProductId = productId,
      Content = request.Content.Trim(),
      Rating = request.Rating,
      IsVisible = true
    };
    await commentRepository.AddAsync(comment, ct);

    return new ReviewDto(comment.Id, userId, "", null, comment.Rating!.Value, comment.Content, comment.CreatedAt);
  }

  private async Task<CommentDto> MapCommentToDtoAsync(Comment comment, CancellationToken cancellationToken)
  {
    var replies = await commentRepository.GetRepliesAsync(comment.Id, cancellationToken);
    var replyDtos = replies.Select(r => new CommentDto(
      r.Id, r.UserId, r.User.FullName, r.User.AvatarUrl, r.Content, r.Rating,
      r.ParentCommentId, r.CreatedAt, [])).ToList();

    return new CommentDto(
      comment.Id, comment.UserId, comment.User.FullName, comment.User.AvatarUrl,
      comment.Content, comment.Rating, comment.ParentCommentId, comment.CreatedAt, replyDtos);
  }
}
