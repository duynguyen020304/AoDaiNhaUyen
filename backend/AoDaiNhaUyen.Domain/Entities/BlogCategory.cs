using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class BlogCategory : BaseEntity
{
  public required string Name { get; set; }
  public required string Slug { get; set; }
  public string? Description { get; set; }
  public int SortOrder { get; set; }
  public ICollection<BlogPost> Posts { get; set; } = [];
}
