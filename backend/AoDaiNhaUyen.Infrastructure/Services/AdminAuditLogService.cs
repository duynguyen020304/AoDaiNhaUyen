using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminAuditLogService(AppDbContext dbContext) : IAdminAuditLogService
{
  private const int MaxPageSize = 100;
  private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
  {
    "createdAt", "statusCode", "actionType", "entityType"
  };

  public async Task<PagedResult<AdminAuditLogListItemDto>> SearchAsync(AdminAuditLogSearchRequest request, CancellationToken cancellationToken = default)
  {
    var normalized = NormalizeRequest(request);
    var query = ApplyFilters(dbContext.AdminAuditLogs.AsNoTracking(), normalized);
    var total = await query.CountAsync(cancellationToken);
    var items = await ApplySort(query, normalized)
      .Skip((normalized.Page - 1) * normalized.PageSize)
      .Take(normalized.PageSize)
      .Select(x => new AdminAuditLogListItemDto(
        x.Id,
        x.ActorUserId,
        x.ActorName,
        x.ActorEmail,
        x.ActorRoles,
        x.HttpMethod,
        x.Path,
        x.ActionType,
        x.EntityType,
        x.EntityId,
        x.StatusCode,
        x.Success,
        x.CreatedAt,
        x.RequestPreview,
        x.ResponsePreview,
        x.Error))
      .ToListAsync(cancellationToken);

    return new PagedResult<AdminAuditLogListItemDto>(items, total, normalized.Page, normalized.PageSize);
  }

  public async Task<AdminAuditLogDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await dbContext.AdminAuditLogs.AsNoTracking()
      .Where(x => x.Id == id)
      .Select(x => new AdminAuditLogDetailDto(
        x.Id,
        x.ActorUserId,
        x.ActorName,
        x.ActorEmail,
        x.ActorRoles,
        x.HttpMethod,
        x.Path,
        x.QueryString,
        x.ControllerName,
        x.ActionName,
        x.ActionType,
        x.EntityType,
        x.EntityId,
        x.StatusCode,
        x.Success,
        x.RequestPreview,
        x.ResponsePreview,
        x.Error,
        x.IpAddressHash,
        x.UserAgentHash,
        x.CreatedAt))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<AdminAuditLogStatsDto> GetStatsAsync(AdminAuditLogSearchRequest request, CancellationToken cancellationToken = default)
  {
    var query = ApplyFilters(dbContext.AdminAuditLogs.AsNoTracking(), NormalizeRequest(request with { Page = 1, PageSize = MaxPageSize }));
    var total = await query.CountAsync(cancellationToken);
    if (total == 0) return new AdminAuditLogStatsDto(0, 0, 0, 0, 0);

    var success = await query.CountAsync(x => x.Success, cancellationToken);
    var failed = total - success;
    var distinctActors = await query.Where(x => x.ActorUserId != null).Select(x => x.ActorUserId).Distinct().CountAsync(cancellationToken);
    var distinctEntities = await query.Where(x => x.EntityId != null).Select(x => new { x.EntityType, x.EntityId }).Distinct().CountAsync(cancellationToken);
    return new AdminAuditLogStatsDto(total, success, failed, distinctActors, distinctEntities);
  }

  private static IQueryable<AdminAuditLog> ApplyFilters(IQueryable<AdminAuditLog> query, AdminAuditLogSearchRequest request)
  {
    if (request.From is not null) query = query.Where(x => x.CreatedAt >= request.From.Value);
    if (request.To is not null) query = query.Where(x => x.CreatedAt <= request.To.Value);
    if (!string.IsNullOrWhiteSpace(request.ActionType)) query = query.Where(x => x.ActionType == request.ActionType);
    if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(x => x.EntityType == request.EntityType);
    if (request.ActorUserId is not null) query = query.Where(x => x.ActorUserId == request.ActorUserId);
    if (request.Success is not null) query = query.Where(x => x.Success == request.Success.Value);
    if (request.StatusCode is not null) query = query.Where(x => x.StatusCode == request.StatusCode.Value);
    if (!string.IsNullOrWhiteSpace(request.Q))
    {
      var q = request.Q.Trim();
      query = query.Where(x =>
        x.Path.Contains(q)
        || x.ActionType.Contains(q)
        || x.EntityType.Contains(q)
        || (x.EntityId != null && x.EntityId.Contains(q))
        || (x.ActorName != null && x.ActorName.Contains(q))
        || (x.ActorEmail != null && x.ActorEmail.Contains(q))
        || (x.RequestPreview != null && x.RequestPreview.Contains(q))
        || (x.ResponsePreview != null && x.ResponsePreview.Contains(q))
        || (x.Error != null && x.Error.Contains(q)));
    }

    return query;
  }

  private static IQueryable<AdminAuditLog> ApplySort(IQueryable<AdminAuditLog> query, AdminAuditLogSearchRequest request)
  {
    var desc = !string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
    return request.SortBy.ToLowerInvariant() switch
    {
      "statuscode" => desc ? query.OrderByDescending(x => x.StatusCode).ThenByDescending(x => x.CreatedAt) : query.OrderBy(x => x.StatusCode).ThenByDescending(x => x.CreatedAt),
      "actiontype" => desc ? query.OrderByDescending(x => x.ActionType).ThenByDescending(x => x.CreatedAt) : query.OrderBy(x => x.ActionType).ThenByDescending(x => x.CreatedAt),
      "entitytype" => desc ? query.OrderByDescending(x => x.EntityType).ThenByDescending(x => x.CreatedAt) : query.OrderBy(x => x.EntityType).ThenByDescending(x => x.CreatedAt),
      _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
    };
  }

  private static AdminAuditLogSearchRequest NormalizeRequest(AdminAuditLogSearchRequest request)
  {
    var page = Math.Max(1, request.Page);
    var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
    var sortBy = SortFields.Contains(request.SortBy) ? request.SortBy : "createdAt";
    var sortDir = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
    var from = request.From;
    var to = request.To;
    if (from is not null && to is not null && to.Value < from.Value) (from, to) = (to, from);
    if (from is not null && to is not null && to.Value - from.Value > TimeSpan.FromDays(31)) to = from.Value.AddDays(31);
    return request with { Page = page, PageSize = pageSize, SortBy = sortBy, SortDir = sortDir, From = from, To = to };
  }
}
