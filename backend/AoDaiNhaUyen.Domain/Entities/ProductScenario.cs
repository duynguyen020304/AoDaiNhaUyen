using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductScenario : BaseEntity
{
  public Guid ProductId { get; set; }
  public Guid ScenarioId { get; set; }
  public decimal Score { get; set; }
  public string? Notes { get; set; }


  public Product Product { get; set; } = null!;
  public StyleScenario Scenario { get; set; } = null!;
}
