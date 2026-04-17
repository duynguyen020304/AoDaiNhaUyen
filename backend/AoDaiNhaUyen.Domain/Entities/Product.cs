namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Product
{
  public long Id { get; set; }
  public long CategoryId { get; set; }
  public required string Name { get; set; }
  public required string Slug { get; set; }
  public required string ProductType { get; set; }
  public string? ShortDescription { get; set; }
  public string? Description { get; set; }
  public string? Material { get; set; }
  public string? Brand { get; set; }
  public string? Origin { get; set; }
  public string? CareInstruction { get; set; }
  public string Status { get; set; } = "draft";
  public bool IsFeatured { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Category Category { get; set; } = null!;
  public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
  public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
