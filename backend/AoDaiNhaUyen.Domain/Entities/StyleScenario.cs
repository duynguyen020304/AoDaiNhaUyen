namespace AoDaiNhaUyen.Domain.Entities;

public sealed class StyleScenario
{
  public long Id { get; set; }
  public required string Slug { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public ICollection<ProductScenario> ProductScenarios { get; set; } = new List<ProductScenario>();
  public ICollection<ProductPairing> ProductPairings { get; set; } = new List<ProductPairing>();
}
