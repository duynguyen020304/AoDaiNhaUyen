namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Payment
{
  public long Id { get; set; }
  public long OrderId { get; set; }
  public decimal Amount { get; set; }
  public DateTime PaidAt { get; set; } = DateTime.UtcNow;
  public string? Note { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public Order Order { get; set; } = null!;
}
