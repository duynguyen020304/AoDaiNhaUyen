using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class LlmAuditService(
  AppDbContext dbContext,
  IPromptRedactionService redactionService) : ILlmAuditService
{
  private const int MaxPageSize = 100;
  private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
  {
    "createdAt", "latencyMs", "totalTokens", "status"
  };

  public async Task<Guid> LogStartedAsync(LlmAuditLogCreateDto request, CancellationToken cancellationToken = default)
  {
    var now = DateTime.UtcNow;
    var log = new LlmAuditLog
    {
      Id = Guid.NewGuid(),
      Source = Limit(request.Source, 80) ?? "Unknown",
      Provider = Limit(request.Provider, 80) ?? "Unknown",
      Model = Limit(request.Model, 160),
      Operation = Limit(request.Operation, 120) ?? "Unknown",
      ActorUserId = request.ActorUserId,
      ActorRole = Limit(request.ActorRole, 40),
      ThreadId = request.ThreadId,
      ConversationId = request.ConversationId,
      MessageId = request.MessageId,
      AdminActionId = request.AdminActionId,
      UserGeneratedImageId = request.UserGeneratedImageId,
      ToolName = Limit(request.ToolName, 120),
      RiskLevel = Limit(request.RiskLevel, 30),
      RequiresConfirmation = request.RequiresConfirmation,
      StartedAt = now,
      CreatedAt = now,
      UpdatedAt = now,
      RetainUntil = now.AddDays(90),
      PromptPreviewRedacted = redactionService.Redact(request.Prompt),
      CompletionPreviewRedacted = redactionService.Redact(request.Completion),
      InputMetadataJson = request.InputMetadataJson,
      Status = "started",
      RedactionVersion = "v1"
    };

    dbContext.LlmAuditLogs.Add(log);
    await dbContext.SaveChangesAsync(cancellationToken);
    return log.Id;
  }

  public async Task LogCompletedAsync(Guid id, LlmAuditLogUpdateDto request, CancellationToken cancellationToken = default)
  {
    var log = await dbContext.LlmAuditLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (log is null) return;

    var now = DateTime.UtcNow;
    log.CompletedAt = now;
    log.LatencyMs = (long)Math.Max(0, (now - log.StartedAt).TotalMilliseconds);
    log.Status = NormalizeStatus(request.Status);
    log.ErrorCode = Limit(request.ErrorCode, 80);
    log.PromptTokens = request.PromptTokens;
    log.CompletionTokens = request.CompletionTokens;
    log.TotalTokens = (request.PromptTokens ?? 0) + (request.CompletionTokens ?? 0);
    log.EstimatedCost = request.EstimatedCost;
    log.CompletionPreviewRedacted = redactionService.Redact(request.Completion);
    log.OutputMetadataJson = request.OutputMetadataJson;
    log.SafetyFlagsJson = request.SafetyFlagsJson;
    log.UpdatedAt = now;

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public Task LogFailedAsync(Guid id, string errorCode, CancellationToken cancellationToken = default) =>
    LogCompletedAsync(id, new LlmAuditLogUpdateDto("failed", ErrorCode: Limit(errorCode, 80)), cancellationToken);

  public async Task<PagedResult<LlmAuditLogListItemDto>> SearchAsync(LlmAuditLogSearchRequest request, CancellationToken cancellationToken = default)
  {
    var normalized = NormalizeRequest(request);
    var query = ApplyFilters(dbContext.LlmAuditLogs.AsNoTracking(), normalized);
    var total = await query.CountAsync(cancellationToken);
    var items = await ApplySort(query, normalized)
      .Skip((normalized.Page - 1) * normalized.PageSize)
      .Take(normalized.PageSize)
      .Select(x => new LlmAuditLogListItemDto(
        x.Id,
        x.RequestId,
        x.CorrelationId,
        x.TraceId,
        x.ActorUserId,
        x.ActorRole,
        x.Source,
        x.Provider,
        x.Model,
        x.Operation,
        x.ToolName,
        x.RiskLevel,
        x.RequiresConfirmation,
        x.Status,
        x.ErrorCode,
        x.LatencyMs,
        x.TotalTokens,
        x.EstimatedCost,
        x.CreatedAt,
        x.PromptPreviewRedacted,
        x.CompletionPreviewRedacted))
      .ToListAsync(cancellationToken);

    return new PagedResult<LlmAuditLogListItemDto>(items, total, normalized.Page, normalized.PageSize);
  }

  public async Task<LlmAuditLogDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await dbContext.LlmAuditLogs.AsNoTracking()
      .Where(x => x.Id == id)
      .Select(x => new LlmAuditLogDetailDto(
        x.Id,
        x.RequestId,
        x.CorrelationId,
        x.TraceId,
        x.ConversationId,
        x.ThreadId,
        x.MessageId,
        x.AdminActionId,
        x.UserGeneratedImageId,
        x.ActorUserId,
        x.ActorRole,
        x.Source,
        x.IpHash,
        x.UserAgentHash,
        x.Provider,
        x.Model,
        x.Operation,
        x.ActionType,
        x.ToolName,
        x.RiskLevel,
        x.RequiresConfirmation,
        x.ApprovedByUserId,
        x.ApprovedAt,
        x.StartedAt,
        x.CompletedAt,
        x.LatencyMs,
        x.PromptTokens,
        x.CompletionTokens,
        x.TotalTokens,
        x.EstimatedCost,
        x.Status,
        x.ErrorCode,
        x.PromptPreviewRedacted,
        x.CompletionPreviewRedacted,
        x.InputMetadataJson,
        x.OutputMetadataJson,
        x.SafetyFlagsJson,
        x.RedactionVersion,
        x.RetainUntil,
        x.CreatedAt))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<LlmAuditLogStatsDto> GetStatsAsync(LlmAuditLogSearchRequest request, CancellationToken cancellationToken = default)
  {
    var query = ApplyFilters(dbContext.LlmAuditLogs.AsNoTracking(), NormalizeRequest(request with { Page = 1, PageSize = MaxPageSize }), includeQ: false);
    var total = await query.CountAsync(cancellationToken);
    if (total == 0) return new LlmAuditLogStatsDto(0, 0, 0, 0, 0, 0, 0m);

    var success = await query.CountAsync(x => x.Status == "success", cancellationToken);
    var failed = await query.CountAsync(x => x.Status == "failed", cancellationToken);
    var timeout = await query.CountAsync(x => x.Status == "timeout", cancellationToken);
    var averageLatency = await query.Where(x => x.LatencyMs != null).AverageAsync(x => (double?)x.LatencyMs, cancellationToken) ?? 0;
    var tokens = await query.SumAsync(x => x.TotalTokens ?? 0, cancellationToken);
    var cost = await query.SumAsync(x => x.EstimatedCost ?? 0m, cancellationToken);
    return new LlmAuditLogStatsDto(total, success, failed, timeout, averageLatency, tokens, cost);
  }

  private static IQueryable<LlmAuditLog> ApplyFilters(IQueryable<LlmAuditLog> query, LlmAuditLogSearchRequest request, bool includeQ = true)
  {
    if (request.From is not null) query = query.Where(x => x.CreatedAt >= request.From.Value);
    if (request.To is not null) query = query.Where(x => x.CreatedAt <= request.To.Value);
    if (!string.IsNullOrWhiteSpace(request.Source)) query = query.Where(x => x.Source == request.Source);
    if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(x => x.Status == request.Status);
    if (!string.IsNullOrWhiteSpace(request.Provider)) query = query.Where(x => x.Provider == request.Provider);
    if (!string.IsNullOrWhiteSpace(request.Model)) query = query.Where(x => x.Model == request.Model);
    if (!string.IsNullOrWhiteSpace(request.Operation)) query = query.Where(x => x.Operation == request.Operation);
    if (!string.IsNullOrWhiteSpace(request.RiskLevel)) query = query.Where(x => x.RiskLevel == request.RiskLevel);
    if (!string.IsNullOrWhiteSpace(request.ToolName)) query = query.Where(x => x.ToolName == request.ToolName);
    if (request.ActorUserId is not null) query = query.Where(x => x.ActorUserId == request.ActorUserId);
    if (request.ThreadId is not null) query = query.Where(x => x.ThreadId == request.ThreadId);
    if (request.ConversationId is not null) query = query.Where(x => x.ConversationId == request.ConversationId);
    if (!string.IsNullOrWhiteSpace(request.RequestId)) query = query.Where(x => x.RequestId == request.RequestId || x.CorrelationId == request.RequestId);

    if (includeQ && !string.IsNullOrWhiteSpace(request.Q))
    {
      var q = request.Q.Trim();
      query = query.Where(x =>
        x.RequestId.Contains(q)
        || x.CorrelationId.Contains(q)
        || (x.PromptPreviewRedacted != null && x.PromptPreviewRedacted.Contains(q))
        || (x.CompletionPreviewRedacted != null && x.CompletionPreviewRedacted.Contains(q)));
    }

    return query;
  }

  private static IQueryable<LlmAuditLog> ApplySort(IQueryable<LlmAuditLog> query, LlmAuditLogSearchRequest request)
  {
    var desc = !string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
    return request.SortBy.ToLowerInvariant() switch
    {
      "latencyms" => desc ? query.OrderByDescending(x => x.LatencyMs) : query.OrderBy(x => x.LatencyMs),
      "totaltokens" => desc ? query.OrderByDescending(x => x.TotalTokens) : query.OrderBy(x => x.TotalTokens),
      "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
      _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
    };
  }

  private static LlmAuditLogSearchRequest NormalizeRequest(LlmAuditLogSearchRequest request)
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

  private static string NormalizeStatus(string status) => status.ToLowerInvariant() switch
  {
    "success" => "success",
    "failed" => "failed",
    "timeout" => "timeout",
    "cancelled" => "cancelled",
    _ => "success"
  };

  private static string? Limit(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }
}
