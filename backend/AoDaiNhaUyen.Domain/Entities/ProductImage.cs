using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductImage : BaseEntity
{
  public Guid ProductId { get; set; }
  public Guid? VariantId { get; set; }
  public required string ImageUrl { get; set; }
  public string? AltText { get; set; }
  public int SortOrder { get; set; }
  public bool IsPrimary { get; set; }
  public bool IsPublic { get; set; }
  public string? PublicObjectKey { get; set; }


  public Product Product { get; set; } = null!;
  public ProductVariant? Variant { get; set; }
}
