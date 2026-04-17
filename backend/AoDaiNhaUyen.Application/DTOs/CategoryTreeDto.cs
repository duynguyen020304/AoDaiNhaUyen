namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CategoryTreeDto(
  long Id,
  string Name,
  string Slug,
  int SortOrder,
  IReadOnlyList<CategoryTreeChildDto> Children);

public sealed record CategoryTreeChildDto(
  long Id,
  string Name,
  string Slug,
  int SortOrder);
