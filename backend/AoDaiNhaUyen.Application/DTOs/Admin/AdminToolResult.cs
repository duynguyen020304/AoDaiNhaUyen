namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Standard envelope for admin tool results consumed by the AI agent.</summary>
public sealed record AdminToolResult<T>(
  bool Success,
  string? Code,
  string? Message,
  T? Data,
  AdminToolResultMeta? Meta = null,
  AdminToolSafety? Safety = null);

/// <summary>Pagination and completeness metadata for AI-safe list reasoning.</summary>
public sealed record AdminToolResultMeta(
  int? Page = null,
  int? PageSize = null,
  int? Total = null,
  int? TotalPages = null,
  bool? HasMore = null,
  string? Completeness = null,
  object? FiltersApplied = null,
  int? ItemsLoaded = null,
  bool? AllItemsLoaded = null,
  bool? Truncated = null,
  int? NextPage = null,
  string? NextCursor = null,
  object? NextPageArgs = null);

/// <summary>Safety classification metadata for admin tool results.</summary>
public sealed record AdminToolSafety(
  string RiskLevel,
  bool RequiresConfirmation,
  string? Warning = null);
