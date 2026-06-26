using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AoDaiNhaUyen.Api.Middleware;

public sealed partial class AdminAuditLogMiddleware(RequestDelegate next)
{
  private const int PreviewCap = 1200;
  private static readonly HashSet<string> MutationMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };
  private static readonly string[] SkipPrefixes =
  [
    "/api/admin/audit-logs",
    "/api/admin/hermes/report",
    "/api/admin/hermes/heartbeat"
  ];

  public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, ILogger<AdminAuditLogMiddleware> logger)
  {
    var path = context.Request.Path.Value ?? string.Empty;
    var method = context.Request.Method;
    var isHermesAgent = context.User.FindFirst("agent")?.Value == "hermes";
    var isAdminPath = path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase)
      || path.StartsWith("/api/v1/admin/", StringComparison.OrdinalIgnoreCase);

    if (!isAdminPath || !MutationMethods.Contains(method) || isHermesAgent || SkipPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
    {
      await next(context);
      return;
    }

    string? requestPreview = null;
    if (ShouldCaptureRequestBody(context.Request))
    {
      context.Request.EnableBuffering();
      var bodyBytes = await ReadStreamAsync(context.Request.Body, context.RequestAborted);
      requestPreview = ScrubPreview(Encoding.UTF8.GetString(bodyBytes));
    }
    else if (context.Request.ContentLength is > 0)
    {
      requestPreview = $"[omitted:{context.Request.ContentType};length={context.Request.ContentLength}]";
    }

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
        responsePreview = await CaptureResponsePreviewAsync(responseBuffer, context.RequestAborted);
        responseBuffer.Position = 0;
        await responseBuffer.CopyToAsync(originalResponseStream, context.RequestAborted);
        context.Response.Body = originalResponseStream;

        var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var actorUserIdRaw = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? actorUserId = Guid.TryParse(actorUserIdRaw, out var parsedUserId) ? parsedUserId : null;
        var actorName = context.User.FindFirstValue(ClaimTypes.Name) ?? context.User.FindFirstValue("full_name");
        var actorEmail = context.User.FindFirstValue(ClaimTypes.Email);
        var actorRoles = string.Join(",", context.User.FindAll(ClaimTypes.Role).Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        var pathParts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var entityType = InferEntityType(pathParts);
        var entityId = InferEntityId(pathParts);
        var actionType = InferActionType(method, pathParts);
        var now = DateTime.UtcNow;

        dbContext.AdminAuditLogs.Add(new AdminAuditLog
        {
          Id = Guid.NewGuid(),
          ActorUserId = actorUserId,
          ActorName = Limit(actorName, 120),
          ActorEmail = Limit(actorEmail, 160),
          ActorRoles = Limit(actorRoles, 200),
          HttpMethod = method,
          Path = Limit(path, 400) ?? path,
          QueryString = Limit(context.Request.QueryString.Value, 500),
          ControllerName = Limit(descriptor?.ControllerName, 80),
          ActionName = Limit(descriptor?.ActionName, 120),
          ActionType = actionType,
          EntityType = entityType,
          EntityId = Limit(entityId, 120),
          StatusCode = context.Response.StatusCode,
          Success = context.Response.StatusCode is >= 200 and < 400,
          RequestPreview = requestPreview,
          ResponsePreview = responsePreview,
          Error = Limit(caught?.Message, 500),
          IpAddressHash = HashValue(GetClientIp(context)),
          UserAgentHash = HashValue(context.Request.Headers.UserAgent.ToString()),
          CreatedAt = now,
          UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(context.RequestAborted);
      }
      catch (Exception auditEx)
      {
        context.Response.Body = originalResponseStream;
        logger.LogWarning(auditEx, "Failed to write admin audit log for {Method} {Path}", method, path);
      }
    }
  }

  private static bool ShouldCaptureRequestBody(HttpRequest request)
  {
    if (request.ContentLength is not > 0) return false;
    var contentType = request.ContentType ?? string.Empty;
    return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
      || contentType.Contains("text/", StringComparison.OrdinalIgnoreCase)
      || contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
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
    return ScrubPreview(Encoding.UTF8.GetString(bytes, 0, read));
  }

  private static string InferEntityType(string[] pathParts)
  {
    if (pathParts.Length < 3) return "admin";
    return pathParts[2] switch
    {
      "products" => "product",
      "categories" => "category",
      "orders" => "order",
      "users" => "user",
      "roles" => "role",
      "media" => "media",
      "inventory" => "inventory",
      "promos" => "promo",
      "reviews" => "review",
      "email-templates" => "email_template",
      "subscribers" => "subscriber",
      "email-jobs" => "email_job",
      "marketing" => "marketing",
      "facebook" => "facebook",
      "ai" => "ai",
      "tools-risk" => "tool_risk",
      "ai-tryon-feedback" => "ai_tryon_feedback",
      "hermes" => "hermes",
      _ => pathParts[2]
    };
  }

  private static string? InferEntityId(string[] pathParts)
  {
    foreach (var part in pathParts.Skip(3))
    {
      if (Guid.TryParse(part, out _)) return part;
    }
    return null;
  }

  private static string InferActionType(string method, string[] pathParts)
  {
    var last = pathParts.LastOrDefault() ?? string.Empty;
    if (last.Equals("restore", StringComparison.OrdinalIgnoreCase)) return "restore";
    if (last.Equals("status", StringComparison.OrdinalIgnoreCase)) return "status_change";
    if (last.Equals("role", StringComparison.OrdinalIgnoreCase)) return "role_change";
    if (last.Equals("visibility", StringComparison.OrdinalIgnoreCase)) return "visibility_change";
    if (last.Equals("primary", StringComparison.OrdinalIgnoreCase)) return "set_primary";
    if (last.Equals("make-public", StringComparison.OrdinalIgnoreCase)) return "make_public";
    if (last.Equals("make-private", StringComparison.OrdinalIgnoreCase)) return "make_private";
    if (last.Equals("toggle", StringComparison.OrdinalIgnoreCase)) return "toggle";
    if (last.Equals("retry", StringComparison.OrdinalIgnoreCase)) return "retry";
    if (last.Equals("cancel", StringComparison.OrdinalIgnoreCase)) return "cancel";
    if (last.Equals("import", StringComparison.OrdinalIgnoreCase)) return "import";
    if (last.Equals("send", StringComparison.OrdinalIgnoreCase)) return "send";
    if (pathParts.Contains("images", StringComparer.OrdinalIgnoreCase) && method.Equals("POST", StringComparison.OrdinalIgnoreCase)) return "upload";
    return method.ToUpperInvariant() switch
    {
      "POST" => "create",
      "PUT" => "update",
      "PATCH" => "update",
      "DELETE" => "delete",
      _ => method.ToLowerInvariant()
    };
  }

  private static string? GetClientIp(HttpContext context)
  {
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
    return !string.IsNullOrWhiteSpace(forwardedFor)
      ? forwardedFor
      : context.Connection.RemoteIpAddress?.ToString();
  }

  private static string? HashValue(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();
  }

  private static string? Limit(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }

  private static string? ScrubPreview(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var scrubbed = SensitiveJsonFieldRegex().Replace(value, m => $"{m.Groups[1].Value}[redacted]{m.Groups[3].Value}");
    scrubbed = EmailRegex().Replace(scrubbed, "[email]");
    scrubbed = PhoneRegex().Replace(scrubbed, "[phone]");
    return scrubbed.Length <= PreviewCap ? scrubbed : scrubbed[..PreviewCap] + "…";
  }

  [GeneratedRegex("(\"(?:password|token|accessToken|refreshToken|pageAccessToken|secret|apiKey|authorization|cookie)\"\\s*:\\s*\")(.*?)(\")", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
  private static partial Regex SensitiveJsonFieldRegex();

  [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
  private static partial Regex EmailRegex();

  [GeneratedRegex(@"(?<!\w)(?:\+?84|0)(?:[\s.\-]?\d){8,10}(?!\w)")]
  private static partial Regex PhoneRegex();
}
