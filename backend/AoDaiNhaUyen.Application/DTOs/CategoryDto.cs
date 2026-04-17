namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CategoryDto(
	long Id,
	long? Parent,
	string Name,
	string Slug,
	string? Description,
	string? ImageUrl,
	int SortOrder,
	bool IsActive);
