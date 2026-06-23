namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IHermesReportCompressorService
{
  Task<HermesCompressedReportResult> CompressAsync(
    IReadOnlyList<HermesPartialReportInput> partialReports,
    CancellationToken cancellationToken);
}

public sealed record HermesPartialReportInput(
  int Index,
  IReadOnlyList<Guid> EventIds,
  string ReportText,
  string? Severity,
  string? ReportType);

public sealed record HermesCompressedReportResult(
  string Title,
  string Summary,
  string Severity,
  string ReportType,
  IReadOnlyList<string> KeyFindings,
  IReadOnlyList<string> RecommendedActions,
  string Markdown);
