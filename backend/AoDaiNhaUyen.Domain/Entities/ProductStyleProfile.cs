using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductStyleProfile : BaseEntity
{
  public Guid ProductId { get; set; }
  public string? StyleKeywordsJsonb { get; set; }
  public string? Formality { get; set; }
  public string? Silhouette { get; set; }
  public string? Notes { get; set; }
  public string? PrimaryColorFamily { get; set; }
  public string? SecondaryColorFamily { get; set; }


  public Product Product { get; set; } = null!;
}
