using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class BlogImage : BaseEntity
{
  public Guid? BlogPostId { get; set; }
  public BlogPost? BlogPost { get; set; }
  public required string ImageUrl { get; set; }
  public string? PublicObjectKey { get; set; }
  public bool IsPublic { get; set; }
  public string? AltText { get; set; }
  public int SortOrder { get; set; }
}
