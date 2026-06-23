using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminEmailTemplateService(
  AppDbContext dbContext,
  IHermesEventOutboxPublisher hermesEvents) : IAdminEmailTemplateService
{
  public async Task<(IReadOnlyList<EmailTemplateListItem> Items, int TotalItem)> GetListAsync(string? search, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default)
  {
    if (!await HasTemplateConfigColumnsAsync(cancellationToken))
    {
      return await GetLegacyListAsync(search, includeDeleted, page, pageSize, cancellationToken);
    }

    var query = dbContext.EmailTemplates.AsNoTracking().AsQueryable();
    if (!includeDeleted) query = query.Where(x => !x.IsDeleted);
    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = $"%{search.Trim()}%";
      query = query.Where(x => EF.Functions.ILike(x.Key, term) || EF.Functions.ILike(x.Name, term) || EF.Functions.ILike(x.Subject, term));
    }

    var total = await query.CountAsync(cancellationToken);
    var items = await query.OrderBy(x => x.Key).ThenByDescending(x => x.Version)
      .Skip((page - 1) * pageSize).Take(pageSize)
      .Select(x => new EmailTemplateListItem(x.Id, x.Key, x.Name, x.Subject, x.TemplateType, x.Locale, x.Version, x.IsSystem, x.IsActive, x.IsDeleted, x.CreatedAt, x.UpdatedAt))
      .ToListAsync(cancellationToken);
    return (items, total);
  }

  public async Task<EmailTemplateDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    if (!await HasTemplateConfigColumnsAsync(cancellationToken))
    {
      return await GetLegacyByIdAsync(id, cancellationToken);
    }

    return await dbContext.EmailTemplates.AsNoTracking().Where(x => x.Id == id).Select(x => ToDetail(x)).FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<EmailTemplateDetail> CreateAsync(CreateEmailTemplateRequest request, CancellationToken cancellationToken = default)
  {
    var configJson = NormalizeConfigJson(request.ConfigJson);
    Validate(request.Key, request.Name, request.Subject, request.TemplateType, configJson, request.Locale);
    var key = request.Key.Trim();
    var locale = request.Locale.Trim();
    var exists = await dbContext.EmailTemplates.AnyAsync(x => x.Key == key && x.Locale == locale && x.Version == 1, cancellationToken);
    if (exists) throw new InvalidOperationException("Mẫu email đã tồn tại.");

    var now = DateTime.UtcNow;
    var template = new EmailTemplate
    {
      Id = Guid.NewGuid(),
      Key = key,
      Name = request.Name.Trim(),
      Subject = request.Subject.Trim(),
      Preheader = Normalize(request.Preheader),
      HtmlBody = string.Empty,
      TextBody = null,
      TemplateType = request.TemplateType.Trim(),
      ConfigJson = configJson,
      IsSystem = request.IsSystem,
      Locale = locale,
      Version = 1,
      IsActive = true,
      CreatedAt = now,
      UpdatedAt = now
    };
    dbContext.EmailTemplates.Add(template);
    await dbContext.SaveChangesAsync(cancellationToken);
    await hermesEvents.EnqueueAdminEmailEventAsync(
      "email_template_created",
      template.Id,
      new { templateId = template.Id, template.Key, template.Name, template.Locale, template.Version },
      $"email_template_created:Email:{template.Id:N}:{template.Version}",
      cancellationToken);
    return ToDetail(template);
  }

  public async Task<EmailTemplateDetail?> UpdateAsync(Guid id, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default)
  {
    var configJson = NormalizeConfigJson(request.ConfigJson);
    Validate("existing", request.Name, request.Subject, request.TemplateType, configJson, request.Locale);
    var template = await dbContext.EmailTemplates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (template is null) return null;
    template.Name = request.Name.Trim();
    template.Subject = request.Subject.Trim();
    template.Preheader = Normalize(request.Preheader);
    template.TemplateType = request.TemplateType.Trim();
    template.ConfigJson = configJson;
    template.IsSystem = request.IsSystem;
    template.Locale = request.Locale.Trim();
    template.IsActive = request.IsActive;
    template.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    await hermesEvents.EnqueueAdminEmailEventAsync(
      "email_template_updated",
      template.Id,
      new { templateId = template.Id, template.Key, template.Name, template.Locale, template.IsActive },
      $"email_template_updated:Email:{template.Id:N}:{template.UpdatedAt.Ticks}",
      cancellationToken);
    return ToDetail(template);
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var template = await dbContext.EmailTemplates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    if (template is null) return false;
    template.IsDeleted = true;
    template.IsActive = false;
    template.DeletedAt = DateTime.UtcNow;
    template.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    await hermesEvents.EnqueueAdminEmailEventAsync(
      "email_template_updated",
      template.Id,
      new { templateId = template.Id, template.Key, action = "deleted" },
      $"email_template_deleted:Email:{template.Id:N}:{template.UpdatedAt.Ticks}",
      cancellationToken);
    return true;
  }

  public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var template = await dbContext.EmailTemplates.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);
    if (template is null) return false;
    template.IsDeleted = false;
    template.IsActive = true;
    template.DeletedAt = null;
    template.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    await hermesEvents.EnqueueAdminEmailEventAsync(
      "email_template_updated",
      template.Id,
      new { templateId = template.Id, template.Key, action = "restored" },
      $"email_template_restored:Email:{template.Id:N}:{template.UpdatedAt.Ticks}",
      cancellationToken);
    return true;
  }

  private sealed record LegacyTemplateRow(
    Guid Id,
    string Key,
    string Name,
    string Subject,
    string? Preheader,
    string Locale,
    int Version,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);

  private async Task<bool> HasTemplateConfigColumnsAsync(CancellationToken cancellationToken)
  {
    if (!dbContext.Database.IsRelational()) return true;

    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    try
    {
      if (shouldClose) await connection.OpenAsync(cancellationToken);
      await using var command = connection.CreateCommand();
      command.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'email_templates'
          AND column_name IN ('template_type', 'config_json', 'is_system')
        """;
      var result = await command.ExecuteScalarAsync(cancellationToken);
      return Convert.ToInt32(result) == 3;
    }
    catch (Exception) when (!cancellationToken.IsCancellationRequested)
    {
      return true;
    }
    finally
    {
      if (shouldClose && connection.State == ConnectionState.Open) await connection.CloseAsync();
    }
  }

  private async Task<(IReadOnlyList<EmailTemplateListItem> Items, int TotalItem)> GetLegacyListAsync(string? search, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken)
  {
    var query = dbContext.EmailTemplates.AsNoTracking().AsQueryable();
    if (!includeDeleted) query = query.Where(x => !x.IsDeleted);
    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = $"%{search.Trim()}%";
      query = query.Where(x => EF.Functions.ILike(x.Key, term) || EF.Functions.ILike(x.Name, term) || EF.Functions.ILike(x.Subject, term));
    }

    var total = await query.CountAsync(cancellationToken);
    var rows = await query.OrderBy(x => x.Key).ThenByDescending(x => x.Version)
      .Skip((page - 1) * pageSize).Take(pageSize)
      .Select(x => new LegacyTemplateRow(x.Id, x.Key, x.Name, x.Subject, x.Preheader, x.Locale, x.Version, x.IsActive, x.IsDeleted, x.CreatedAt, x.UpdatedAt))
      .ToListAsync(cancellationToken);
    return (rows.Select(ToLegacyListItem).ToList(), total);
  }

  private async Task<EmailTemplateDetail?> GetLegacyByIdAsync(Guid id, CancellationToken cancellationToken)
  {
    var row = await dbContext.EmailTemplates.AsNoTracking()
      .Where(x => x.Id == id)
      .Select(x => new LegacyTemplateRow(x.Id, x.Key, x.Name, x.Subject, x.Preheader, x.Locale, x.Version, x.IsActive, x.IsDeleted, x.CreatedAt, x.UpdatedAt))
      .FirstOrDefaultAsync(cancellationToken);
    return row is null ? null : ToLegacyDetail(row);
  }

  private static EmailTemplateListItem ToLegacyListItem(LegacyTemplateRow x) => new(
    x.Id,
    x.Key,
    x.Name,
    x.Subject,
    SafeEmailTemplateRenderer.ResolveType(x.Key) ?? "legacy.html",
    x.Locale,
    x.Version,
    SafeEmailTemplateRenderer.IsCodeManagedKey(x.Key),
    x.IsActive,
    x.IsDeleted,
    x.CreatedAt,
    x.UpdatedAt);

  private static EmailTemplateDetail ToLegacyDetail(LegacyTemplateRow x) => new(
    x.Id,
    x.Key,
    x.Name,
    x.Subject,
    x.Preheader,
    SafeEmailTemplateRenderer.ResolveType(x.Key) ?? "legacy.html",
    "{}",
    x.Locale,
    x.Version,
    SafeEmailTemplateRenderer.IsCodeManagedKey(x.Key),
    x.IsActive,
    x.IsDeleted,
    x.CreatedAt,
    x.UpdatedAt);

  private static EmailTemplateDetail ToDetail(EmailTemplate x) => new(x.Id, x.Key, x.Name, x.Subject, x.Preheader, x.TemplateType, x.ConfigJson ?? "{}", x.Locale, x.Version, x.IsSystem, x.IsActive, x.IsDeleted, x.CreatedAt, x.UpdatedAt);
  private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static string NormalizeConfigJson(string? value)
  {
    var normalized = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
    try
    {
      using var document = JsonDocument.Parse(normalized);
      if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Cấu hình template phải là JSON object.");
      return normalized;
    }
    catch (JsonException ex)
    {
      throw new ArgumentException("Cấu hình template phải là JSON hợp lệ.", ex);
    }
  }

  private static void Validate(string key, string name, string subject, string templateType, string configJson, string locale)
  {
    if (string.IsNullOrWhiteSpace(key) || key.Length > 120) throw new ArgumentException("Khóa mẫu email không hợp lệ.");
    if (!System.Text.RegularExpressions.Regex.IsMatch(key.Trim(), "^[a-z0-9._-]+$")) throw new ArgumentException("Khóa mẫu email chỉ dùng chữ thường, số, dấu chấm, gạch ngang hoặc gạch dưới.");
    if (string.IsNullOrWhiteSpace(name) || name.Length > 160) throw new ArgumentException("Tên mẫu email không hợp lệ.");
    if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255) throw new ArgumentException("Tiêu đề email không hợp lệ.");
    if (string.IsNullOrWhiteSpace(templateType) || templateType.Trim() is not ("marketing.promo" or "marketing.newsletter" or "subscriber.welcome" or "order.confirmation" or "legacy.html")) throw new ArgumentException("Loại template không được hỗ trợ.");
    if (configJson.Length > 50_000) throw new ArgumentException("Cấu hình template tối đa 50.000 ký tự.");
    if (string.IsNullOrWhiteSpace(locale) || locale.Length > 20) throw new ArgumentException("Locale không hợp lệ.");
  }
}

public sealed class AdminSubscriberService(
  AppDbContext dbContext,
  IHermesEventOutboxPublisher hermesEvents) : IAdminSubscriberService
{
  public async Task<(IReadOnlyList<SubscriberListItem> Items, int TotalItem)> GetListAsync(string? search, string? status, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default)
  {
    var query = dbContext.Subscribers.AsNoTracking().AsQueryable();
    if (!includeDeleted) query = query.Where(x => !x.IsDeleted);
    if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = $"%{search.Trim()}%";
      query = query.Where(x => EF.Functions.ILike(x.Email, term));
    }
    var total = await query.CountAsync(cancellationToken);
    var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
      .Select(x => new SubscriberListItem(x.Id, x.Email, x.Status, x.SubscribedAt, x.UnsubscribedAt, x.LastSentAt, x.UserId, x.IsDeleted))
      .ToListAsync(cancellationToken);
    return (items, total);
  }

  public async Task<SubscriberDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await dbContext.Subscribers.AsNoTracking().Where(x => x.Id == id)
      .Select(x => new SubscriberDetail(x.Id, x.Email, x.Status, x.SubscribedAt, x.UnsubscribedAt, x.LastSentAt, x.LastOpenAt, x.LastClickAt, x.UserId, x.IsDeleted,
        x.Consents.OrderByDescending(c => c.CreatedAt).Select(c => new ConsentRecord(c.Channel, c.IsOptIn, c.Source, c.ConsentedAt, c.RevokedAt)).ToList()))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<bool> UnsubscribeAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var subscriber = await dbContext.Subscribers.Include(x => x.Consents).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (subscriber is null) return false;
    var now = DateTime.UtcNow;
    subscriber.Status = "unsubscribed";
    subscriber.UnsubscribedAt = now;
    subscriber.UpdatedAt = now;
    foreach (var consent in subscriber.Consents.Where(x => x.Channel == "email" && x.IsOptIn))
    {
      consent.IsOptIn = false;
      consent.RevokedAt = now;
      consent.UpdatedAt = now;
    }
    await dbContext.SaveChangesAsync(cancellationToken);

    await hermesEvents.EnqueueAdminEmailEventAsync(
      "subscriber_unsubscribed",
      subscriber.Id,
      new { subscriberId = subscriber.Id, action = "unsubscribed", status = subscriber.Status },
      $"subscriber_unsubscribed:Email:{subscriber.Id:N}:{subscriber.UpdatedAt.Ticks}",
      cancellationToken);

    return true;
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var subscriber = await dbContext.Subscribers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (subscriber is null || subscriber.IsDeleted) return false;

    var now = DateTime.UtcNow;
    subscriber.IsDeleted = true;
    subscriber.DeletedAt = now;
    subscriber.UpdatedAt = now;

    await dbContext.SaveChangesAsync(cancellationToken);

    await hermesEvents.EnqueueAdminEmailEventAsync(
      "subscriber_deleted",
      subscriber.Id,
      new { subscriberId = subscriber.Id, action = "deleted", subscriber.Email, deletedAt = now },
      $"subscriber_deleted:Email:{subscriber.Id:N}:{now.Ticks}",
      cancellationToken);

    return true;
  }

  public async Task<ImportSubscribersResult> BulkImportAsync(IReadOnlyList<string> emails, string source, CancellationToken cancellationToken = default)
  {
    if (emails.Count > 1000) throw new ArgumentException("Chỉ được nhập tối đa 1000 email mỗi lần.");
    var normalized = emails.Select(x => x.Trim().ToLowerInvariant()).Where(IsEmailLike).Distinct().ToList();
    var existing = await dbContext.Subscribers.Where(x => normalized.Contains(x.Email)).Select(x => x.Email).ToListAsync(cancellationToken);
    var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var now = DateTime.UtcNow;
    var imported = 0;
    foreach (var email in normalized.Where(x => !existingSet.Contains(x)))
    {
      var subscriber = new Subscriber
      {
        Id = Guid.NewGuid(),
        Email = email,
        Status = "active",
        SubscribedAt = now,
        ConfirmedAt = now,
        ConfirmationToken = Token(),
        UnsubscribeToken = Token(),
        CreatedAt = now,
        UpdatedAt = now
      };
      subscriber.Consents.Add(new MarketingConsent { Id = Guid.NewGuid(), SubscriberId = subscriber.Id, Channel = "email", IsOptIn = true, Source = string.IsNullOrWhiteSpace(source) ? "admin_import" : source.Trim(), ConsentedAt = now, CreatedAt = now, UpdatedAt = now });
      dbContext.Subscribers.Add(subscriber);
      imported++;
    }
    await dbContext.SaveChangesAsync(cancellationToken);

    await hermesEvents.EnqueueAdminEmailEventAsync(
      "subscribers_bulk_imported",
      Guid.NewGuid(),
      new { action = "bulk_imported", source = string.IsNullOrWhiteSpace(source) ? "admin_import" : source.Trim(), importedCount = imported, requestedCount = emails.Count },
      $"subscriber_bulk_imported:Email:{now.Ticks}:{imported}",
      cancellationToken);

    return new ImportSubscribersResult(imported, emails.Count - imported);
  }

  private static bool IsEmailLike(string value) => value.Length <= 150 && value.Contains('@') && value.Contains('.');
  private static string Token() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}

public sealed class AdminEmailJobService(
  AppDbContext dbContext,
  IOptions<EmailSettings> emailSettings,
  IHermesEventOutboxPublisher hermesEvents) : IAdminEmailJobService
{
  private readonly EmailSettings emailSettings = emailSettings.Value;

  public async Task<(IReadOnlyList<EmailJobListItem> Items, int TotalItem)> GetListAsync(string? status, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default)
  {
    var query = dbContext.EmailJobs.AsNoTracking().AsQueryable();
    if (!includeDeleted) query = query.Where(x => !x.IsDeleted);
    if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
    var total = await query.CountAsync(cancellationToken);
    var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
      .Select(x => new EmailJobListItem(x.Id, x.ToEmail, x.TemplateKey, x.Status, x.RetryCount, x.ScheduledAt, x.SentAt, x.ErrorMessage, x.IsDeleted))
      .ToListAsync(cancellationToken);
    return (items, total);
  }

  public async Task<EmailJobDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await dbContext.EmailJobs.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted)
      .Select(x => new EmailJobDetail(x.Id, x.ToEmail, x.TemplateKey, x.PayloadJson, x.Status, x.RetryCount, x.ScheduledAt, x.SentAt, x.ErrorMessage,
        x.SendLogs.OrderByDescending(l => l.CreatedAt).Select(l => new SendLogRecord(l.Status, l.SentAt, l.FailedAt, l.ErrorMessage)).ToList()))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<QueueSingleEmailJobResponse> QueueSingleAsync(QueueSingleEmailJobRequest request, CancellationToken cancellationToken = default)
  {
    ValidateSingleEmail(request);
    var idempotencyKey = request.IdempotencyKey.Trim();
    var existing = await FindExistingByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
    if (existing is not null)
      return new QueueSingleEmailJobResponse(existing.Id, existing.ToEmail, existing.TemplateKey, existing.ScheduledAt, true);

    var recipient = await ResolveRecipientAsync(request, cancellationToken);
    var templateKey = request.TemplateKey.Trim();
    var hasTemplate = await dbContext.EmailTemplates.AnyAsync(x => x.Key == templateKey && x.IsActive && !x.IsDeleted, cancellationToken);
    if (!hasTemplate && !SafeEmailTemplateRenderer.IsCodeManagedKey(templateKey) && templateKey != "hermes.single_email") throw new InvalidOperationException("Mẫu email chưa hoạt động hoặc không tồn tại.");
    if (request.Purpose.Trim().Equals("survey", StringComparison.OrdinalIgnoreCase) && !recipient.HasEmailConsent)
      throw new InvalidOperationException("Khách hàng chưa opt-in email survey/marketing.");

    var scheduledAt = request.ScheduledAt?.ToUniversalTime() ?? DateTime.UtcNow;
    var job = new EmailJob
    {
      Id = Guid.NewGuid(),
      ToEmail = recipient.Email,
      TemplateKey = templateKey,
      PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
      {
        subject = request.Subject.Trim(),
        preheader = Normalize(request.Preheader),
        intro = Normalize(request.Intro),
        body = Normalize(request.Body),
        ctaLabel = Normalize(request.CtaLabel),
        ctaUrl = Normalize(request.CtaUrl),
        purpose = request.Purpose.Trim().ToLowerInvariant(),
        scheduledAt = scheduledAt.ToString("O"),
        idempotencyKey,
        customerId = recipient.CustomerId,
        orderId = recipient.OrderId,
        unsubscribeUrl = recipient.UnsubscribeToken is null ? null : BuildFrontendUrl($"/unsubscribe?token={Uri.EscapeDataString(recipient.UnsubscribeToken)}")
      }),
      Status = "queued",
      ScheduledAt = scheduledAt,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    dbContext.EmailJobs.Add(job);
    await dbContext.SaveChangesAsync(cancellationToken);
    await hermesEvents.EnqueueAdminEmailEventAsync(
      "single_email_job_created",
      job.Id,
      new { jobId = job.Id, toEmail = MaskEmail(job.ToEmail), job.TemplateKey, purpose = request.Purpose.Trim().ToLowerInvariant(), scheduledAt, recipient.CustomerId, recipient.OrderId },
      $"single_email_job_created:Email:{idempotencyKey}",
      cancellationToken);
    return new QueueSingleEmailJobResponse(job.Id, job.ToEmail, job.TemplateKey, job.ScheduledAt, false);
  }

  public async Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var job = await dbContext.EmailJobs.FirstOrDefaultAsync(x => x.Id == id && (x.Status == "queued" || x.Status == "dead" || x.Status == "failed" || x.Status == "cancelled"), cancellationToken);
    if (job is null) return false;
    job.Status = "queued";
    job.RetryCount = 0;
    job.ScheduledAt = DateTime.UtcNow;
    job.ErrorMessage = null;
    job.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }

  private async Task<EmailJob?> FindExistingByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
  {
    // payload_json is a jsonb column. EF translates string.Contains(...) into a SQL LIKE
    // ('~~'), which Postgres has no operator for on jsonb (error 42883:
    // "operator does not exist: jsonb ~~ jsonb") — this made every single-email POST 500.
    // Query the JSON field directly with the ->> operator instead (parameterized, safe).
    return await dbContext.EmailJobs
      .FromSqlInterpolated($"SELECT * FROM email_jobs WHERE payload_json->>'idempotencyKey' = {idempotencyKey} AND is_deleted = FALSE ORDER BY created_at DESC LIMIT 1")
      .AsNoTracking()
      .FirstOrDefaultAsync(cancellationToken);
  }

  private async Task<SingleEmailRecipient> ResolveRecipientAsync(QueueSingleEmailJobRequest request, CancellationToken cancellationToken)
  {
    if (!request.CustomerId.HasValue && !request.OrderId.HasValue) throw new ArgumentException("Cần customerId hoặc orderId để xác minh email người nhận.");
    Guid? customerId = request.CustomerId;
    Guid? orderId = request.OrderId;
    string? email = null;

    if (request.OrderId.HasValue)
    {
      var order = await dbContext.Orders.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == request.OrderId.Value, cancellationToken)
        ?? throw new ArgumentException("Không tìm thấy đơn hàng nguồn.");
      orderId = order.Id;
      customerId = order.UserId;
      email = order.User.Email;
    }

    if (request.CustomerId.HasValue)
    {
      if (customerId.HasValue && customerId.Value != request.CustomerId.Value) throw new ArgumentException("customerId không khớp với orderId.");
      var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.CustomerId.Value, cancellationToken)
        ?? throw new ArgumentException("Không tìm thấy khách hàng.");
      customerId = user.Id;
      email ??= user.Email;
    }

    if (string.IsNullOrWhiteSpace(email) || !IsEmailLike(email.Trim().ToLowerInvariant())) throw new ArgumentException("Khách hàng nguồn không có email hợp lệ.");
    var normalizedEmail = email.Trim().ToLowerInvariant();
    if (!string.IsNullOrWhiteSpace(request.ToEmail) && !string.Equals(request.ToEmail.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
      throw new ArgumentException("toEmail không khớp email của khách hàng nguồn.");

    var subscriber = await dbContext.Subscribers.AsNoTracking().Include(x => x.Consents)
      .Where(x => !x.IsDeleted && x.Email == normalizedEmail)
      .OrderByDescending(x => x.UpdatedAt)
      .FirstOrDefaultAsync(cancellationToken);
    var hasEmailConsent = subscriber is not null && subscriber.Status == "active" && subscriber.Consents.Any(x => x.Channel == "email" && x.IsOptIn && x.RevokedAt == null);
    return new SingleEmailRecipient(normalizedEmail, customerId, orderId, hasEmailConsent, subscriber?.UnsubscribeToken);
  }

  private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  private static bool IsEmailLike(string value) => value.Length <= 150 && value.Contains('@') && value.Contains('.');

  private static void ValidateSingleEmail(QueueSingleEmailJobRequest request)
  {
    if (!string.IsNullOrWhiteSpace(request.ToEmail) && !IsEmailLike(request.ToEmail.Trim().ToLowerInvariant())) throw new ArgumentException("Email người nhận không hợp lệ.");
    if (string.IsNullOrWhiteSpace(request.TemplateKey) || request.TemplateKey.Length > 120) throw new ArgumentException("Template key không hợp lệ.");
    if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 255) throw new ArgumentException("Tiêu đề email không hợp lệ.");
    if (string.IsNullOrWhiteSpace(request.Purpose) || request.Purpose.Trim().ToLowerInvariant() is not ("transactional" or "survey" or "thank_you")) throw new ArgumentException("Purpose phải là transactional, survey hoặc thank_you.");
    if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 160) throw new ArgumentException("IdempotencyKey bắt buộc và tối đa 160 ký tự.");
    if (request.Body is { Length: > 4000 }) throw new ArgumentException("Nội dung email tối đa 4000 ký tự.");
    var ctaUrl = Normalize(request.CtaUrl);
    if (ctaUrl is not null && (!Uri.TryCreate(ctaUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))) throw new ArgumentException("CTA URL phải là http/https hợp lệ.");
    if (request.ScheduledAt.HasValue && request.ScheduledAt.Value.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-5)) throw new ArgumentException("Thời gian gửi không được nằm trong quá khứ.");
    if (request.ScheduledAt.HasValue && request.ScheduledAt.Value.ToUniversalTime() > DateTime.UtcNow.AddDays(180)) throw new ArgumentException("Chỉ được lên lịch email trong 180 ngày tới.");
  }

  private string BuildFrontendUrl(string path)
  {
    var baseUrl = string.IsNullOrWhiteSpace(emailSettings.FrontendBaseUrl) ? "https://aodainhauyen.io.vn" : emailSettings.FrontendBaseUrl.TrimEnd('/');
    if (baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) || baseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
      baseUrl = "https://aodainhauyen.io.vn";
    return $"{baseUrl}/{path.TrimStart('/')}";
  }

  private static string MaskEmail(string email)
  {
    var parts = email.Split('@', 2);
    if (parts.Length != 2 || parts[0].Length == 0) return "***";
    return $"{parts[0][0]}***@{parts[1]}";
  }

  public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var job = await dbContext.EmailJobs.FirstOrDefaultAsync(x => x.Id == id && (x.Status == "queued" || x.Status == "dead" || x.Status == "failed"), cancellationToken);
    if (job is null) return false;
    job.Status = "cancelled";
    job.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var job = await dbContext.EmailJobs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (job is null || job.IsDeleted) return false;
    if (job.Status is "queued" or "sending") return false;

    var now = DateTime.UtcNow;
    job.IsDeleted = true;
    job.DeletedAt = now;
    job.UpdatedAt = now;
    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }

  private sealed record SingleEmailRecipient(string Email, Guid? CustomerId, Guid? OrderId, bool HasEmailConsent, string? UnsubscribeToken);
}

public sealed class AdminMarketingStatsService(AppDbContext dbContext) : IAdminMarketingStatsService
{
  public async Task<MarketingStats> GetStatsAsync(CancellationToken cancellationToken = default)
  {
    var today = DateTime.UtcNow.Date;
    var totalSubscribers = await dbContext.Subscribers.CountAsync(cancellationToken);
    var activeSubscribers = await dbContext.Subscribers.CountAsync(x => x.Status == "active", cancellationToken);
    var pendingSubscribers = await dbContext.Subscribers.CountAsync(x => x.Status == "pending", cancellationToken);
    var unsubscribedSubscribers = await dbContext.Subscribers.CountAsync(x => x.Status == "unsubscribed", cancellationToken);
    var queuedJobs = await dbContext.EmailJobs.CountAsync(x => x.Status == "queued" || x.Status == "sending", cancellationToken);
    var sentJobsToday = await dbContext.EmailJobs.CountAsync(x => x.Status == "sent" && x.SentAt >= today, cancellationToken);
    var failedJobs = await dbContext.EmailJobs.CountAsync(x => x.Status == "dead" || x.Status == "failed", cancellationToken);
    var templateCount = await dbContext.EmailTemplates.CountAsync(x => !x.IsDeleted, cancellationToken);
    return new MarketingStats(totalSubscribers, activeSubscribers, pendingSubscribers, unsubscribedSubscribers, queuedJobs, sentJobsToday, failedJobs, templateCount);
  }
}

public sealed class AdminMarketingCampaignService(
  AppDbContext dbContext,
  IOptions<EmailSettings> emailSettings,
  IHermesEventOutboxPublisher hermesEvents) : IAdminMarketingCampaignService
{
  private const int MaxRecipients = 5000;
  private const int MaxAttachments = 12;
  private readonly EmailSettings emailSettings = emailSettings.Value;

  public async Task<IReadOnlyList<MarketingContentOption>> GetContentOptionsAsync(CancellationToken cancellationToken = default)
  {
    var now = DateTime.UtcNow;
    var promos = await dbContext.PromoCodes.AsNoTracking()
      .Where(x => x.IsActive && !x.IsDeleted && x.StartDate <= now && x.EndDate > now)
      .OrderByDescending(x => x.CreatedAt)
      .Take(20)
      .Select(x => new { x.Id, x.Code, x.DiscountType, x.DiscountValue, x.MinOrderAmount, x.EndDate, x.FreeShipping })
      .ToListAsync(cancellationToken);

    var blogs = await dbContext.BlogPosts.AsNoTracking()
      .Where(x => x.IsActive && !x.IsDeleted && x.Status == AoDaiNhaUyen.Domain.Common.BlogPostStatus.Published)
      .OrderByDescending(x => x.PublishedAt ?? x.UpdatedAt)
      .Take(20)
      .Select(x => new { x.Id, x.Title, x.Slug, x.Excerpt, x.PublishedAt })
      .ToListAsync(cancellationToken);

    var products = await dbContext.Products.AsNoTracking()
      .Where(x => x.IsActive && !x.IsDeleted && x.IsPublic && x.Status == "active")
      .OrderByDescending(x => x.IsFeatured)
      .ThenByDescending(x => x.UpdatedAt)
      .Take(20)
      .Select(x => new { x.Id, x.Name, x.Slug, x.ShortDescription, x.ProductType })
      .ToListAsync(cancellationToken);

    var result = new List<MarketingContentOption>();
    result.AddRange(promos.Select(x =>
    {
      var discount = x.DiscountType == "percentage" ? $"Giảm {x.DiscountValue:0}%" : $"Giảm {x.DiscountValue:0,0}đ";
      var subtitle = x.FreeShipping ? $"{discount} + freeship" : discount;
      var url = BuildFrontendUrl("/products");
      return new MarketingContentOption(x.Id, "promo", x.Code, subtitle, url, $"HSD {x.EndDate:dd/MM/yyyy}", PromoHtml(x.Code, subtitle, x.MinOrderAmount, url));
    }));
    result.AddRange(blogs.Select(x =>
    {
      var url = BuildFrontendUrl($"/blog/{x.Slug}/");
      return new MarketingContentOption(x.Id, "blog", x.Title, x.Excerpt, url, x.PublishedAt?.ToString("dd/MM/yyyy"), LinkHtml("Bài viết mới", x.Title, x.Excerpt, url));
    }));
    result.AddRange(products.Select(x =>
    {
      var url = BuildFrontendUrl($"/product/{x.Slug}");
      return new MarketingContentOption(x.Id, "product", x.Name, x.ShortDescription, url, x.ProductType, LinkHtml("Sản phẩm nổi bật", x.Name, x.ShortDescription, url));
    }));

    return result;
  }

  public async Task<MarketingCampaignSendResult> QueueCampaignAsync(SendMarketingCampaignRequest request, CancellationToken cancellationToken = default)
  {
    ValidateCampaign(request);
    var templateKey = request.TemplateKey.Trim();
    var templateExists = SafeEmailTemplateRenderer.IsCodeManagedKey(templateKey) || await dbContext.EmailTemplates.AnyAsync(x => x.Key == templateKey && x.IsActive && !x.IsDeleted, cancellationToken);
    if (!templateExists) throw new InvalidOperationException("Mẫu email chưa hoạt động hoặc không tồn tại.");

    var recipients = await ResolveRecipientsAsync(request, cancellationToken);
    if (recipients.Count == 0) throw new ArgumentException("Không có người nhận hợp lệ.");
    if (recipients.Count > MaxRecipients) throw new ArgumentException($"Chỉ được gửi tối đa {MaxRecipients} email mỗi chiến dịch.");

    var scheduledAt = request.ScheduledAt?.ToUniversalTime() ?? DateTime.UtcNow;
    var attachments = NormalizeAttachments(request.Attachments);
    var attachmentsHtml = BuildAttachmentsHtml(attachments);
    var bodyHtml = BuildBodyHtml(request, attachmentsHtml);
    var queued = 0;
    var skippedEmails = new List<string>();

    foreach (var recipient in recipients)
    {
      if (!IsEmailLike(recipient.Email))
      {
        skippedEmails.Add(recipient.Email);
        continue;
      }

      dbContext.EmailJobs.Add(new EmailJob
      {
        Id = Guid.NewGuid(),
        ToEmail = recipient.Email,
        TemplateKey = templateKey,
        PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
        {
          subject = request.Subject.Trim(),
          preheader = Normalize(request.Preheader),
          heading = request.Subject.Trim(),
          intro = Normalize(request.Intro),
          body = Normalize(request.BodyHtml),
          bodyHtml,
          attachmentsHtml,
          ctaLabel = Normalize(request.CtaLabel),
          ctaText = Normalize(request.CtaLabel) ?? "Xem chi tiết",
          ctaUrl = Normalize(request.CtaUrl) ?? BuildFrontendUrl("/products"),
          expiryDate = attachments.Where(x => x.Type == "promo").Select(x => x.Description).FirstOrDefault() ?? "khi chương trình kết thúc",
          unsubscribeUrl = recipient.UnsubscribeToken is null ? null : BuildFrontendUrl($"/unsubscribe?token={Uri.EscapeDataString(recipient.UnsubscribeToken)}"),
          sentAt = DateTime.UtcNow.ToString("O")
        }),
        Status = "queued",
        ScheduledAt = scheduledAt,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      });
      queued++;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    var campaignKey = $"campaign:{templateKey}:{request.Subject.Trim().GetHashCode(StringComparison.OrdinalIgnoreCase):X}:{scheduledAt.Ticks}";
    await hermesEvents.EnqueueAdminEmailEventAsync(
      scheduledAt > DateTime.UtcNow.AddSeconds(5) ? "email_campaign_scheduled" : "email_campaign_created",
      Guid.NewGuid(),
      new { templateKey, subject = request.Subject.Trim(), recipientCount = queued, scheduledAt, recipientMode = request.RecipientMode },
      $"email_campaign_created:Email:{campaignKey}",
      cancellationToken);

    return new MarketingCampaignSendResult(queued, skippedEmails.Count, skippedEmails);
  }

  private async Task<List<CampaignRecipient>> ResolveRecipientsAsync(SendMarketingCampaignRequest request, CancellationToken cancellationToken)
  {
    var mode = request.RecipientMode.Trim().ToLowerInvariant();
    if (mode == "all_active")
    {
      return await dbContext.Subscribers.AsNoTracking()
        .Where(x => !x.IsDeleted && x.Status == "active")
        .OrderByDescending(x => x.SubscribedAt ?? x.CreatedAt)
        .Select(x => new CampaignRecipient(x.Email.ToLower(), x.UnsubscribeToken))
        .ToListAsync(cancellationToken);
    }

    if (mode == "selected")
    {
      var ids = (request.SubscriberIds ?? Array.Empty<Guid>()).Distinct().ToList();
      if (ids.Count == 0) throw new ArgumentException("Vui lòng chọn ít nhất một người nhận.");
      return await dbContext.Subscribers.AsNoTracking()
        .Where(x => ids.Contains(x.Id) && !x.IsDeleted && x.Status == "active")
        .Select(x => new CampaignRecipient(x.Email.ToLower(), x.UnsubscribeToken))
        .ToListAsync(cancellationToken);
    }

    var emails = (request.ManualEmails ?? Array.Empty<string>())
      .Select(x => x.Trim().ToLowerInvariant())
      .Where(x => !string.IsNullOrWhiteSpace(x))
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
    if (emails.Count == 0) throw new ArgumentException("Vui lòng nhập ít nhất một email.");
    return emails.Select(x => new CampaignRecipient(x, null)).ToList();
  }

  private static void ValidateCampaign(SendMarketingCampaignRequest request)
  {
    var mode = request.RecipientMode?.Trim().ToLowerInvariant();
    if (mode is not ("all_active" or "selected" or "manual")) throw new ArgumentException("Kiểu người nhận không hợp lệ.");
    if (string.IsNullOrWhiteSpace(request.TemplateKey) || request.TemplateKey.Length > 120) throw new ArgumentException("Vui lòng chọn mẫu email.");
    if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 255) throw new ArgumentException("Tiêu đề email không hợp lệ.");
    if ((request.Attachments?.Count ?? 0) > MaxAttachments) throw new ArgumentException($"Chỉ được đính kèm tối đa {MaxAttachments} nội dung.");
    if (request.ScheduledAt.HasValue && request.ScheduledAt.Value.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-5)) throw new ArgumentException("Thời gian gửi không được nằm trong quá khứ.");
  }

  private static IReadOnlyList<MarketingCampaignAttachmentRequest> NormalizeAttachments(IReadOnlyList<MarketingCampaignAttachmentRequest>? attachments)
  {
    return (attachments ?? Array.Empty<MarketingCampaignAttachmentRequest>())
      .Where(x => !string.IsNullOrWhiteSpace(x.Title))
      .Take(MaxAttachments)
      .ToList();
  }

  private static string BuildPlainBody(SendMarketingCampaignRequest request, IReadOnlyList<MarketingCampaignAttachmentRequest> attachments)
  {
    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(request.Intro)) parts.Add(request.Intro.Trim());
    if (!string.IsNullOrWhiteSpace(request.BodyHtml)) parts.Add(request.BodyHtml.Trim());
    parts.AddRange(attachments.Select(x => $"{x.Title}: {x.Description ?? x.Url ?? string.Empty}"));
    return string.Join("\n\n", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
  }

  private static string BuildBodyHtml(SendMarketingCampaignRequest request, string attachmentsHtml)
  {
    var builder = new StringBuilder();
    var intro = Normalize(request.Intro);
    if (intro is not null) builder.Append("<p>").Append(HtmlEncoder.Default.Encode(intro)).Append("</p>");
    if (!string.IsNullOrWhiteSpace(request.BodyHtml)) builder.Append("<p>").Append(HtmlEncoder.Default.Encode(request.BodyHtml.Trim()).Replace("\n", "<br>", StringComparison.Ordinal)).Append("</p>");
    if (!string.IsNullOrWhiteSpace(attachmentsHtml)) builder.Append(attachmentsHtml);
    var ctaUrl = Normalize(request.CtaUrl);
    var ctaLabel = Normalize(request.CtaLabel) ?? "Xem chi tiết";
    if (ctaUrl is not null)
    {
      builder.Append("<p style=\"margin:24px 0\"><a href=\"")
        .Append(HtmlEncoder.Default.Encode(ctaUrl))
        .Append("\" style=\"display:inline-block;background:#7f1d1d;color:#fff;padding:12px 18px;border-radius:999px;text-decoration:none;font-weight:700\">")
        .Append(HtmlEncoder.Default.Encode(ctaLabel))
        .Append("</a></p>");
    }
    return builder.ToString();
  }

  private static string BuildAttachmentsHtml(IReadOnlyList<MarketingCampaignAttachmentRequest> attachments)
  {
    if (attachments.Count == 0) return string.Empty;
    var builder = new StringBuilder("<div style=\"margin-top:24px\">");
    foreach (var item in attachments)
    {
      var typeLabel = item.Type switch { "promo" => "Khuyến mãi", "blog" => "Bài viết", "product" => "Sản phẩm", _ => "Thông báo" };
      builder.Append("<div style=\"border:1px solid #ead7d7;border-radius:16px;padding:16px;margin:12px 0;background:#fffaf7\">")
        .Append("<div style=\"font-size:12px;text-transform:uppercase;letter-spacing:.08em;color:#9f6b2f;font-weight:700\">")
        .Append(HtmlEncoder.Default.Encode(typeLabel))
        .Append("</div><h3 style=\"margin:6px 0 8px;color:#7f1d1d\">")
        .Append(HtmlEncoder.Default.Encode(item.Title.Trim()))
        .Append("</h3>");
      if (!string.IsNullOrWhiteSpace(item.Code)) builder.Append("<p><strong>Mã: ").Append(HtmlEncoder.Default.Encode(item.Code.Trim())).Append("</strong></p>");
      if (!string.IsNullOrWhiteSpace(item.Description)) builder.Append("<p>").Append(HtmlEncoder.Default.Encode(item.Description.Trim())).Append("</p>");
      if (!string.IsNullOrWhiteSpace(item.Url)) builder.Append("<p><a href=\"").Append(HtmlEncoder.Default.Encode(item.Url.Trim())).Append("\">Xem chi tiết</a></p>");
      builder.Append("</div>");
    }
    return builder.Append("</div>").ToString();
  }

  private string BuildFrontendUrl(string path)
  {
    var baseUrl = string.IsNullOrWhiteSpace(emailSettings.FrontendBaseUrl) ? "https://aodainhauyen.io.vn" : emailSettings.FrontendBaseUrl.TrimEnd('/');
    if (baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) || baseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
      baseUrl = "https://aodainhauyen.io.vn";
    return $"{baseUrl}/{path.TrimStart('/')}";
  }

  private static string PromoHtml(string code, string subtitle, decimal minOrderAmount, string url)
  {
    var minOrder = minOrderAmount > 0 ? $" cho đơn từ {minOrderAmount:0,0}đ" : string.Empty;
    return LinkHtml("Khuyến mãi", code, $"{subtitle}{minOrder}", url);
  }

  private static string LinkHtml(string label, string title, string? description, string url)
  {
    return $"<div><strong>{HtmlEncoder.Default.Encode(label)}:</strong> <a href=\"{HtmlEncoder.Default.Encode(url)}\">{HtmlEncoder.Default.Encode(title)}</a><p>{HtmlEncoder.Default.Encode(description ?? string.Empty)}</p></div>";
  }

  private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  private static bool IsEmailLike(string value) => value.Length <= 150 && value.Contains('@') && value.Contains('.');
  private sealed record CampaignRecipient(string Email, string? UnsubscribeToken);
}
