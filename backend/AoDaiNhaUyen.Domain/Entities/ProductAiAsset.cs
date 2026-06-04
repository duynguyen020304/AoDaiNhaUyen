using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductAiAsset : BaseEntity
{
  public Guid ProductId { get; set; }
  public Guid? VariantId { get; set; }
  public required string AssetKind { get; set; }
  public required string FileUrl { get; set; }
  public required string MimeType { get; set; }


  public Product Product { get; set; } = null!;
  public ProductVariant? Variant { get; set; }
}
