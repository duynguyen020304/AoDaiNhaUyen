namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CategoryDto(
	Guid Id,
	Guid? Parent,
	string Name,
	string Slug,
	string? Description,
	string? ImageUrl,
	int SortOrder,
	bool IsActive);
