using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class HermesFanOutSubBatch : BaseEntity
{
  public Guid RunId { get; set; }
  public int SubBatchIndex { get; set; }
  public int EventCount { get; set; }
  public string EventIdsJson { get; set; } = "[]";
  public string Status { get; set; } = "pending";
  public int? DurationMs { get; set; }
  public string ReportType { get; set; } = "mixed";
  public string Severity { get; set; } = "info";
  public string? ReportPreview { get; set; }
  public string? ReportTextForCompression { get; set; }
  public string? Error { get; set; }

  public HermesRun? Run { get; set; }
}
