namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductVariant
{
  public long Id { get; set; }
  public long ProductId { get; set; }
  public required string Sku { get; set; }
  public string? VariantName { get; set; }
  public string? Size { get; set; }
  public string? Color { get; set; }
  public decimal Price { get; set; }
  public decimal? SalePrice { get; set; }
  public int StockQty { get; set; }
  public int? WeightGram { get; set; }
  public bool IsDefault { get; set; }
  public string Status { get; set; } = "active";
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Product Product { get; set; } = null!;
  public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
  public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
  public ICollection<ProductAiAsset> AiAssets { get; set; } = new List<ProductAiAsset>();
}
