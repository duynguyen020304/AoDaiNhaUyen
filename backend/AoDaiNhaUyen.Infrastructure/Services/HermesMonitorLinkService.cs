using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed partial class HermesMonitorLinkService(AppDbContext dbContext) : IHermesMonitorLinkService
{
  private const int DefaultExpiryHours = 24;
  private const int MaxExpiryHours = 72;

  public async Task<HermesMonitorLinkResponse> CreateLinkAsync(
    CreateHermesMonitorLinkRequest request,
    Guid? adminUserId,
    string publicBaseUrl,
    CancellationToken cancellationToken)
  {
    var scopeType = NormalizeScopeType(request.ScopeType);
    var scopeId = NormalizeScopeId(request.ScopeId);

    if (scopeType != "event") throw new ArgumentException("Scope monitor không hợp lệ.", nameof(request.ScopeType));
    if (!Guid.TryParse(scopeId, out var eventId)) throw new ArgumentException("ScopeId event không hợp lệ.", nameof(request.ScopeId));

    var eventExists = await dbContext.HermesEventOutbox.AsNoTracking().AnyAsync(x => x.Id == eventId, cancellationToken);
    if (!eventExists) throw new KeyNotFoundException("Không tìm thấy event Hermes.");

    var token = GenerateToken();
    var now = DateTimeOffset.UtcNow;
    var expiryHours = Math.Clamp(request.ExpiresInHours ?? DefaultExpiryHours, 1, MaxExpiryHours);
    var link = new HermesMonitorLink
    {
      Id = Guid.NewGuid(),
      TokenHash = HashToken(token),
      ScopeType = scopeType,
      ScopeId = eventId.ToString("D"),
      CreatedByAdminUserId = adminUserId,
      ExpiresAt = now.AddHours(expiryHours),
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    };

    dbContext.HermesMonitorLinks.Add(link);
    await dbContext.SaveChangesAsync(cancellationToken);

    var url = $"{publicBaseUrl.TrimEnd('/')}/hermes-monitor/{token}";
    return new HermesMonitorLinkResponse(link.Id, url, token, link.ScopeType, link.ScopeId, link.ExpiresAt, link.RevokedAt, link.AccessCount, link.CreatedAt);
  }

  public async Task<bool> RevokeLinkAsync(Guid id, CancellationToken cancellationToken)
  {
    var link = await dbContext.HermesMonitorLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (link is null || link.RevokedAt is not null) return false;

    link.RevokedAt = DateTimeOffset.UtcNow;
    link.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }

  public async Task<HermesMonitorSnapshotResponse?> GetSnapshotAsync(string token, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(token) || token.Length > 256) return null;

    var hash = HashToken(token.Trim());
    var now = DateTimeOffset.UtcNow;
    var link = await dbContext.HermesMonitorLinks.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
    if (link is null || link.RevokedAt is not null || link.ExpiresAt <= now) return null;
    if (link.ScopeType != "event" || !Guid.TryParse(link.ScopeId, out var eventId)) return null;

    link.LastAccessedAt = now;
    link.AccessCount += 1;
    link.UpdatedAt = now.UtcDateTime;
    await dbContext.SaveChangesAsync(cancellationToken);

    var item = await dbContext.HermesEventOutbox.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);
    if (item is null) return null;

    var eventConversationId = item.Id.ToString("N");
    var runs = await dbContext.HermesRuns.AsNoTracking()
      .Where(x => x.ConversationId == eventConversationId || x.ConversationId == item.CorrelationId)
      .OrderBy(x => x.StartedAt)
      .Select(x => new HermesMonitorRunSummaryResponse(
        x.Id,
        x.Status,
        x.Trigger,
        Redact(x.PromptPreview, 500) ?? string.Empty,
        Redact(x.ResultPreview, 1000),
        Redact(x.Error, 500),
        x.StartedAt,
        x.CompletedAt))
      .ToListAsync(cancellationToken);

    var runIds = runs.Select(x => x.Id).ToArray();
    var reportsQuery = dbContext.HermesReports.AsNoTracking().AsQueryable();
    reportsQuery = reportsQuery.Where(x =>
      (item.CorrelationId != null && x.CorrelationId == item.CorrelationId) ||
      (x.CorrelationId == eventConversationId) ||
      (x.RunId != null && runIds.Contains(x.RunId.Value)));

    var reports = await reportsQuery
      .OrderBy(x => x.CreatedAt)
      .Select(x => new HermesMonitorReportSummaryResponse(
        x.Id,
        x.ReportType,
        x.Severity,
        Redact(x.Title, 200) ?? string.Empty,
        Redact(x.Summary, 2000) ?? string.Empty,
        x.Source,
        x.CorrelationId,
        x.RunId,
        x.Status,
        x.CreatedAt))
      .ToListAsync(cancellationToken);

    var traceSteps = await dbContext.HermesAgentTraceSteps.AsNoTracking()
      .Where(x => x.EventOutboxId == eventId || (x.RunId != null && runIds.Contains(x.RunId.Value)))
      .OrderBy(x => x.StartedAt)
      .Select(x => new HermesMonitorStepResponse(
        x.Id,
        x.RunId,
        x.EventOutboxId,
        x.Kind,
        x.Title,
        x.Summary,
        x.Status,
        x.StartedAt,
        x.CompletedAt,
        x.DurationMs,
        x.SafePayloadJson,
        x.Error))
      .ToListAsync(cancellationToken);

    var heartbeat = await dbContext.HermesHeartbeats.AsNoTracking()
      .OrderByDescending(x => x.RecordedAt)
      .Select(x => new HermesMonitorHeartbeatSummaryResponse(
        x.RunnerName,
        x.Status,
        x.Model,
        x.GatewayStatus,
        x.ActiveJobs,
        Redact(x.LastError, 500),
        x.RecordedAt))
      .FirstOrDefaultAsync(cancellationToken);

    return new HermesMonitorSnapshotResponse(
      new HermesMonitorLinkSummaryResponse(link.Id, link.ScopeType, link.ScopeId, link.ExpiresAt, link.RevokedAt, link.LastAccessedAt, link.AccessCount),
      new HermesMonitorEventSummaryResponse(
        item.Id,
        item.EventType,
        item.AggregateType,
        item.AggregateId,
        item.Status,
        item.Attempts,
        item.MaxAttempts,
        Redact(item.LastError, 500),
        item.CorrelationId,
        item.OccurredAt,
        item.ScheduledAt,
        item.ProcessedAt,
        item.CreatedAt),
      runs,
      traceSteps,
      reports,
      heartbeat,
      BuildThinkingSummary(item, runs.Count, reports.Count),
      now);
  }

  private static string NormalizeScopeType(string? value) => string.IsNullOrWhiteSpace(value) ? "event" : value.Trim().ToLowerInvariant();

  private static string NormalizeScopeId(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ScopeId bắt buộc.", nameof(value));
    var trimmed = value.Trim();
    return trimmed.Length <= 128 ? trimmed : throw new ArgumentException("ScopeId quá dài.", nameof(value));
  }

  private static string GenerateToken()
  {
    var bytes = RandomNumberGenerator.GetBytes(32);
    return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }

  private static string HashToken(string token)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }

  private static string BuildThinkingSummary(HermesEventOutbox item, int runCount, int reportCount) =>
    item.Status switch
    {
      "pending" => "Hermes chưa xử lý event. Monitor chỉ hiển thị event đã vào hàng đợi.",
      "processing" => "Worker đang xử lý event. Theo dõi timeline để xem bước gọi Hermes và báo cáo.",
      "completed" => reportCount > 0
        ? $"Hermes đã xử lý event và tạo {reportCount} báo cáo an toàn. Nội dung hiển thị đã được rút gọn và che dữ liệu nhạy cảm."
        : $"Hermes đã xử lý event qua {runCount} lần chạy. Chưa có báo cáo rủi ro được ghi nhận.",
      "failed" => "Lần xử lý gần nhất thất bại. Worker có thể retry theo lịch nếu chưa vượt giới hạn.",
      "dead" => "Event đã hết lượt retry. Admin cần xem lỗi trong trang quản trị nội bộ.",
      "cancelled" => "Event đã bị hủy bởi admin hoặc hệ thống. Không còn xử lý tự động.",
      _ => "Monitor hiển thị tóm tắt vận hành đã được làm sạch, không phải suy nghĩ thô của mô hình."
    };

  private static string? Redact(string? value, int max)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var redacted = SecretPattern().Replace(value, "$1[đã che]");
    redacted = EmailPattern().Replace(redacted, MaskEmail);
    redacted = PhonePattern().Replace(redacted, "[sđt đã che]");
    return redacted.Length <= max ? redacted : redacted[..max] + "…";
  }

  private static string MaskEmail(Match match)
  {
    var value = match.Value;
    var at = value.IndexOf('@');
    if (at <= 1) return "[email đã che]";
    return value[..1] + "***" + value[at..];
  }

  [GeneratedRegex("(?i)(api[_-]?key|token|secret|password|authorization|bearer)\\s*[:=]\\s*([^\\s,;\"']+)")]
  private static partial Regex SecretPattern();

  [GeneratedRegex("[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
  private static partial Regex EmailPattern();

  [GeneratedRegex("(?<!\\d)(?:\\+?84|0)(?:[\\s.-]?\\d){8,10}(?!\\d)")]
  private static partial Regex PhonePattern();
}
