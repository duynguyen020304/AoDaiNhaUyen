namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CategoryTreeDto(
  Guid Id,
  string Name,
  string Slug,
  int SortOrder,
  IReadOnlyList<CategoryTreeChildDto> Children);

public sealed record CategoryTreeChildDto(
  Guid Id,
  string Name,
  string Slug,
  int SortOrder);
