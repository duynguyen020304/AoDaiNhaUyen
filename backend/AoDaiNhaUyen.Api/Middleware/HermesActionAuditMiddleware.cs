using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Api.Responses;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Api.Middleware;

/// <summary>
/// Audit trail for every real admin API mutation performed by the Hermes agent
/// (the VPS runner calling admin routes with X-Hermes-Admin-Key). Writes one
/// <see cref="HermesActionAudit"/> row per request with method, path, SHA-256
/// body hash, response status, and PII-scrubbed previews.
///
/// Read-only by design (per "all auto, audit only"): never mutates the request
/// path, never blocks except when the explicit kill switch
/// (<see cref="HermesOutboxOptions.AutoExecuteEnabled"/> /
/// <see cref="HermesOutboxOptions.SuspendUntil"/>) is tripped.
/// </summary>
public sealed partial class HermesActionAuditMiddleware(RequestDelegate next)
{
  // Body preview cap keeps the audit row readable and bounded.
  private const int PreviewCap = 800;

  // Routes that are not audited here: agent self-report callbacks (already
  // recorded as report_created trace steps), describe probes (short-circuited
  // upstream), and GETs (read-only).
  private static readonly HashSet<string> MutationMethods =
    new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

  private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
  {
    "/api/admin/hermes/report",
    "/api/admin/hermes/heartbeat"
  };

  public async Task InvokeAsync(
    HttpContext context,
    AppDbContext dbContext,
    IOptions<HermesOutboxOptions> options,
    ILogger<HermesActionAuditMiddleware> logger)
  {
    var opts = options.Value;

    // Only intercept mutation requests made by the Hermes agent itself.
    var isAgent = context.User.FindFirst("agent")?.Value == "hermes";
    var method = context.Request.Method;
    var path = context.Request.Path.Value ?? string.Empty;

    if (!isAgent
        || !MutationMethods.Contains(method)
        || !(path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase)
             || path.StartsWith("/api/v1/admin/", StringComparison.OrdinalIgnoreCase))
        || SkipPaths.Contains(path))
    {
      await next(context);
      return;
    }

    // Kill switch: when explicitly suspended, reject agent mutations with 403
    // and record the attempt so the rejection is itself auditable.
    if (!opts.AutoExecuteEnabled || (opts.SuspendUntil is { } until && until > DateTimeOffset.UtcNow))
    {
      logger.LogWarning("Hermes execution suspended; rejecting {Method} {Path}", method, path);
      await WriteSuspendedAsync(context, method, path, dbContext, opts, logger);
      return;
    }

    // Buffer the REQUEST body so we can hash + preview it, then rewind so the
    // downstream controller reads the original stream.
    // NOTE: EnableBuffering() must be called BEFORE we read; before it runs,
    // Kestrel's default request stream is non-rewindable so the previous
    // CanSeek guard silently swallowed every body.
    string bodyHash = string.Empty;
    string? bodyPreview = null;
    if (context.Request.ContentLength is > 0)
    {
      context.Request.EnableBuffering();
      var bodyBytes = await ReadStreamAsync(context.Request.Body, context.RequestAborted);
      bodyHash = ComputeSha256Hex(bodyBytes);
      bodyPreview = ScrubPreview(Encoding.UTF8.GetString(bodyBytes));
    }

    // Buffer the RESPONSE body by substituting the response stream; restore it
    // after the pipeline runs so the client still receives the real payload.
    var originalResponseStream = context.Response.Body;
    Exception? caught = null;
    string? responsePreview = null;
    using var responseBuffer = new MemoryStream();
    context.Response.Body = responseBuffer;

    try
    {
      await next(context);
    }
    catch (Exception ex)
    {
      caught = ex;
      throw;
    }
    finally
    {
      try
      {
        // Capture response preview before copying back to the original stream.
        responsePreview = await CaptureResponsePreviewAsync(responseBuffer, context.RequestAborted);

        // Restore + flush the real response to the client.
        responseBuffer.Position = 0;
        await responseBuffer.CopyToAsync(originalResponseStream, context.RequestAborted);
        context.Response.Body = originalResponseStream;

        // Record regardless of success/failure; never let audit failure mask
        // the real response or error.
        await RecordAuditAsync(
          dbContext, method, path, bodyHash, bodyPreview,
          context.Response.StatusCode, responsePreview, caught?.Message, context.RequestAborted);
      }
      catch (Exception auditEx)
      {
        // Last-resort: make sure the response stream is restored even if audit
        // throws, otherwise the client gets an empty body.
        context.Response.Body = originalResponseStream;
        logger.LogWarning(auditEx, "Failed to write Hermes action audit for {Method} {Path}", method, path);
      }
    }
  }

  private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken ct)
  {
    stream.Position = 0;
    await using var ms = new MemoryStream();
    await stream.CopyToAsync(ms, ct);
    stream.Position = 0;
    return ms.ToArray();
  }

  private static async Task<string?> CaptureResponsePreviewAsync(MemoryStream buffer, CancellationToken ct)
  {
    if (buffer.Length == 0) return null;
    buffer.Position = 0;
    var bytes = new byte[Math.Min(buffer.Length, PreviewCap * 2)];
    var read = await buffer.ReadAsync(bytes.AsMemory(0, bytes.Length), ct);
    var preview = Encoding.UTF8.GetString(bytes, 0, read);
    return ScrubPreview(preview);
  }

  private static string ComputeSha256Hex(byte[] bytes)
  {
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash).ToLowerInvariant();
  }

  private static async Task RecordAuditAsync(
    AppDbContext dbContext,
    string method,
    string path,
    string bodyHash,
    string? bodyPreview,
    int responseStatus,
    string? responsePreview,
    string? error,
    CancellationToken ct)
  {
    var now = DateTimeOffset.UtcNow;
    dbContext.HermesActionAudits.Add(new HermesActionAudit
    {
      Id = Guid.NewGuid(),
      Method = method,
      Path = path,
      BodyHash = bodyHash,
      BodyPreview = bodyPreview,
      RiskLevel = null, // describe-side metadata; surfaced in UI later
      ResponseStatus = responseStatus,
      ResponsePreview = responsePreview,
      Error = error,
      ExecutedAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    });
    await dbContext.SaveChangesAsync(ct);
  }

  private static async Task WriteSuspendedAsync(
    HttpContext context,
    string method,
    string path,
    AppDbContext dbContext,
    HermesOutboxOptions opts,
    ILogger<HermesActionAuditMiddleware> logger)
  {
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    var reason = !opts.AutoExecuteEnabled ? "disabled" : "suspended";
    await context.Response.WriteAsJsonAsync(ApiResponseFactory.Failure(
      "Hermes execution suspended.",
      "hermes_suspended",
      $"Hermes autonomous execution is {reason}. Set HermesOutbox__AutoExecuteEnabled=true to resume."),
      context.RequestAborted);

    try
    {
      await RecordAuditAsync(dbContext, method, path, string.Empty, null, 403, null, $"hermes_suspended:{reason}", context.RequestAborted);
    }
    catch (Exception auditEx)
    {
      logger.LogWarning(auditEx, "Failed to write Hermes suspended-audit for {Method} {Path}", method, path);
    }
  }

  // PII scrub: redact email + Vietnamese/International phone numbers from any
  // captured body preview so secrets/PII never land in the audit table.
  private static string? ScrubPreview(string? value)
  {
    if (string.IsNullOrEmpty(value)) return null;
    var scrubbed = EmailRegex().Replace(value, "[email]");
    scrubbed = PhoneRegex().Replace(scrubbed, "[phone]");
    return scrubbed.Length <= PreviewCap ? scrubbed : scrubbed[..PreviewCap] + "…";
  }

  [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
  private static partial Regex EmailRegex();

  [GeneratedRegex(@"(?<![A-Za-z])\+?\d[\d\s.\-]{7,}\d(?![A-Za-z])")]
  private static partial Regex PhoneRegex();
}
