using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class HermesReport : BaseEntity
{
  public required string ReportType { get; set; }
  public string Severity { get; set; } = "info";
  public required string Title { get; set; }
  public required string Summary { get; set; }
  public string? PayloadJson { get; set; }
  public string Source { get; set; } = "hermes_agent";
  public string? CorrelationId { get; set; }
  public Guid? RunId { get; set; }
  public string Status { get; set; } = "open";

  public HermesRun? Run { get; set; }
}
