namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductImage
{
  public long Id { get; set; }
  public long ProductId { get; set; }
  public long? VariantId { get; set; }
  public required string ImageUrl { get; set; }
  public string? AltText { get; set; }
  public int SortOrder { get; set; }
  public bool IsPrimary { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public Product Product { get; set; } = null!;
  public ProductVariant? Variant { get; set; }
}
