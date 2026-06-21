using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Collection : BaseEntity
{
  public required string Name { get; set; }
  public required string Slug { get; set; }
  public string? Description { get; set; }
  public string? CoverImageUrl { get; set; }
  public bool IsPublished { get; set; }
  public bool IsFeatured { get; set; }
  public int SortOrder { get; set; }
  public DateTime? PublishedAt { get; set; }

  public ICollection<CollectionProduct> Products { get; set; } = new List<CollectionProduct>();
}
