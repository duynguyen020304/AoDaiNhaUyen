namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductPairing
{
  public long Id { get; set; }
  public long BaseProductId { get; set; }
  public long PairedProductId { get; set; }
  public long? ScenarioId { get; set; }
  public decimal Score { get; set; }
  public string? Notes { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Product BaseProduct { get; set; } = null!;
  public Product PairedProduct { get; set; } = null!;
  public StyleScenario? Scenario { get; set; }
}
