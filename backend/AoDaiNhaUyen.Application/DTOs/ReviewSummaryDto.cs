namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ReviewSummaryDto(
  double AverageRating,
  int TotalReviews,
  IReadOnlyDictionary<int, int> RatingDistribution);
