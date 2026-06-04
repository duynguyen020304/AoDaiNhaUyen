using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class StyleScenario : BaseEntity
{
  public required string Slug { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }


  public ICollection<ProductScenario> ProductScenarios { get; set; } = new List<ProductScenario>();
  public ICollection<ProductPairing> ProductPairings { get; set; } = new List<ProductPairing>();
}
