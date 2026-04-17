namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Category
{
  public long Id { get; set; }
  public long? Parent { get; set; }
  public required string Name { get; set; }
  public required string Slug { get; set; }
  public string? Description { get; set; }
  public string? ImageUrl { get; set; }
  public int SortOrder { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public Category? ParentCategory { get; set; }
  public ICollection<Category> Children { get; set; } = new List<Category>();
  public ICollection<Product> Products { get; set; } = new List<Product>();
}
