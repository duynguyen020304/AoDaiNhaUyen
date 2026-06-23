namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record AdminToolDiagnosticsRequest(
  bool IncludeFacebookMock = true,
  int MaxPerTool = 1,
  IReadOnlyList<string>? ToolNames = null);

public sealed record AdminToolDiagnosticsResponse(
  int Passed,
  int Failed,
  int Skipped,
  int ConfirmationRequired,
  IReadOnlyList<AdminToolDiagnosticsItem> Items);

public sealed record AdminToolDiagnosticsItem(
  string ToolName,
  string Status,
  string? RiskLevel,
  bool RequiresConfirmation,
  string? Message,
  string? ErrorCode,
  int DurationMs);
