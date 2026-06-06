namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CreateReviewRequest(int Rating, string? Comment = null);
