namespace AoDaiNhaUyen.Domain.Entities;

public sealed class MeasurementProfile
{
  public long Id { get; set; }
  public long UserId { get; set; }
  public required string ProfileName { get; set; }
  public decimal? HeightCm { get; set; }
  public decimal? WeightKg { get; set; }
  public decimal? BustCm { get; set; }
  public decimal? WaistCm { get; set; }
  public decimal? HipCm { get; set; }
  public decimal? ShoulderCm { get; set; }
  public decimal? SleeveLengthCm { get; set; }
  public decimal? DressLengthCm { get; set; }
  public string? Notes { get; set; }
  public bool IsDefault { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public User User { get; set; } = null!;
}
