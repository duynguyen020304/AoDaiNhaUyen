using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductPairing : BaseEntity
{
  public Guid BaseProductId { get; set; }
  public Guid PairedProductId { get; set; }
  public Guid? ScenarioId { get; set; }
  public decimal Score { get; set; }
  public string? Notes { get; set; }


  public Product BaseProduct { get; set; } = null!;
  public Product PairedProduct { get; set; } = null!;
  public StyleScenario? Scenario { get; set; }
}
