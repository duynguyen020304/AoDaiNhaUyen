using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

/// <summary>
/// Audit trail of every real admin API mutation performed by the Hermes agent
/// (the VPS runner calling admin routes with X-Hermes-Admin-Key). Written by
/// <c>HermesActionAuditMiddleware</c>. Read-only safety net: never blocks a
/// request — pure observability + undo/debug evidence.
/// </summary>
public sealed class HermesActionAudit : BaseEntity
{
  /// <summary>Optional link to the Hermes run that triggered the action (chat/cron path).</summary>
  public Guid? RunId { get; set; }

  /// <summary>Optional link to the outbox event that triggered the action (rare on execution path).</summary>
  public Guid? EventOutboxId { get; set; }

  /// <summary>HTTP method (POST/PUT/PATCH/DELETE). GETs are not audited.</summary>
  public string Method { get; set; } = string.Empty;

  /// <summary>Relative admin route, e.g. /api/admin/products/{id}.</summary>
  public string Path { get; set; } = string.Empty;

  /// <summary>SHA-256 hex hash of the request body for dedup/fingerprinting.</summary>
  public string BodyHash { get; set; } = string.Empty;

  /// <summary>First ~800 chars of request body for inspection (PII-scrubbed).</summary>
  public string? BodyPreview { get; set; }

  /// <summary>SOUL.md risk label copied from describe metadata when available (low/medium/high).</summary>
  public string? RiskLevel { get; set; }

  /// <summary>HTTP response status code.</summary>
  public int ResponseStatus { get; set; }

  /// <summary>First ~800 chars of response body (PII-scrubbed); null when body could not be captured.</summary>
  public string? ResponsePreview { get; set; }

  /// <summary>Exception or error detail if the call threw.</summary>
  public string? Error { get; set; }

  /// <summary>When the action was executed (UTC).</summary>
  public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
}
