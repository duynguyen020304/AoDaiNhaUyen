using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class CollectionProduct : BaseEntity
{
  public Guid CollectionId { get; set; }
  public Guid ProductId { get; set; }
  public int SortOrder { get; set; }

  public Collection Collection { get; set; } = null!;
  public Product Product { get; set; } = null!;
}
