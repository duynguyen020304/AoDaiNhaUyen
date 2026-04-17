namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ProductImageDto(string ImageUrl, string? AltText, int SortOrder, bool IsPrimary);
