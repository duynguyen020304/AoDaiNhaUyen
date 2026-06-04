namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductAiAsset
{
  public Guid Id { get; set; }
  public Guid ProductId { get; set; }
  public Guid? VariantId { get; set; }
  public required string AssetKind { get; set; }
  public required string FileUrl { get; set; }
  public required string MimeType { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Product Product { get; set; } = null!;
  public ProductVariant? Variant { get; set; }
}
