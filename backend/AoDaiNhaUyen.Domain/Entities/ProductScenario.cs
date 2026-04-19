namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ProductScenario
{
  public long ProductId { get; set; }
  public long ScenarioId { get; set; }
  public decimal Score { get; set; }
  public string? Notes { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public Product Product { get; set; } = null!;
  public StyleScenario Scenario { get; set; } = null!;
}
