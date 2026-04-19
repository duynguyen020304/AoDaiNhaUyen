namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductStyleProfile
{
  public long Id { get; set; }
  public long ProductId { get; set; }
  public string? StyleKeywordsJsonb { get; set; }
  public string? Formality { get; set; }
  public string? Silhouette { get; set; }
  public string? Notes { get; set; }
  public string? PrimaryColorFamily { get; set; }
  public string? SecondaryColorFamily { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Product Product { get; set; } = null!;
}
