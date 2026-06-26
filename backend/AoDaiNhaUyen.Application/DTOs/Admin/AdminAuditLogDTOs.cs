using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record AdminAuditLogListItemDto(
  Guid Id,
  Guid? ActorUserId,
  string? ActorName,
  string? ActorEmail,
  string? ActorRoles,
  string HttpMethod,
  string Path,
  string ActionType,
  string EntityType,
  string? EntityId,
  int StatusCode,
  bool Success,
  DateTime CreatedAt,
  string? RequestPreview,
  string? ResponsePreview,
  string? Error);

public sealed record AdminAuditLogDetailDto(
  Guid Id,
  Guid? ActorUserId,
  string? ActorName,
  string? ActorEmail,
  string? ActorRoles,
  string HttpMethod,
  string Path,
  string? QueryString,
  string? ControllerName,
  string? ActionName,
  string ActionType,
  string EntityType,
  string? EntityId,
  int StatusCode,
  bool Success,
  string? RequestPreview,
  string? ResponsePreview,
  string? Error,
  string? IpAddressHash,
  string? UserAgentHash,
  DateTime CreatedAt);

public sealed record AdminAuditLogSearchRequest(
  int Page = 1,
  int PageSize = 20,
  DateTime? From = null,
  DateTime? To = null,
  string? ActionType = null,
  string? EntityType = null,
  Guid? ActorUserId = null,
  bool? Success = null,
  int? StatusCode = null,
  string? Q = null,
  string SortBy = "createdAt",
  string SortDir = "desc");

public sealed record AdminAuditLogStatsDto(
  int Total,
  int Success,
  int Failed,
  int DistinctActors,
  int DistinctEntities);
