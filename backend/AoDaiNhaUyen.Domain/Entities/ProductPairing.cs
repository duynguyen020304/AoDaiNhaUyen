namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductPairing
{
  public Guid Id { get; set; }
  public Guid BaseProductId { get; set; }
  public Guid PairedProductId { get; set; }
  public Guid? ScenarioId { get; set; }
  public decimal Score { get; set; }
  public string? Notes { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Product BaseProduct { get; set; } = null!;
  public Product PairedProduct { get; set; } = null!;
  public StyleScenario? Scenario { get; set; }
}
