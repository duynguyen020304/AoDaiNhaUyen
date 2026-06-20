using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class BackgroundEmailWorker(IServiceScopeFactory scopeFactory, ILogger<BackgroundEmailWorker> logger) : BackgroundService
{
  private const int BatchSize = 25;
  private const int MaxRetries = 5;
  private static readonly TimeSpan SendingTimeout = TimeSpan.FromMinutes(30);

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await ProcessBatchAsync(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Email worker lỗi.");
      }

      await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    }
  }

  private async Task ProcessBatchAsync(CancellationToken cancellationToken)
  {
    using var scope = scopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var templateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
    var hermesEvents = scope.ServiceProvider.GetRequiredService<IHermesEventOutboxPublisher>();

    await RequeueStaleSendingJobsAsync(dbContext, cancellationToken);

    var claimedJobIds = await dbContext.Database
      .SqlQueryRaw<Guid>(
        """
        UPDATE email_jobs
        SET status = 'sending', updated_at = NOW()
        WHERE id IN (
          SELECT id
          FROM email_jobs
          WHERE status = 'queued' AND scheduled_at <= NOW()
          ORDER BY scheduled_at
          FOR UPDATE SKIP LOCKED
          LIMIT {0}
        )
        RETURNING id AS "Value"
        """,
        BatchSize)
      .ToListAsync(cancellationToken);

    foreach (var jobId in claimedJobIds)
    {
      var job = await dbContext.EmailJobs.FirstAsync(x => x.Id == jobId, cancellationToken);
      await ProcessJobAsync(dbContext, emailService, templateService, hermesEvents, job, cancellationToken);
    }
  }

  private static async Task RequeueStaleSendingJobsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
  {
    var staleBefore = DateTime.UtcNow.Subtract(SendingTimeout);
    await dbContext.EmailJobs
      .Where(x => x.Status == "sending" && x.UpdatedAt < staleBefore && x.RetryCount < MaxRetries)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(x => x.Status, "queued")
        .SetProperty(x => x.ScheduledAt, DateTime.UtcNow)
        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
        .SetProperty(x => x.ErrorMessage, "Email worker recovered stale sending job."), cancellationToken);

    await dbContext.EmailJobs
      .Where(x => x.Status == "sending" && x.UpdatedAt < staleBefore && x.RetryCount >= MaxRetries)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(x => x.Status, "dead")
        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
        .SetProperty(x => x.ErrorMessage, "Email worker marked stale sending job dead."), cancellationToken);
  }

  private static async Task ProcessJobAsync(
    AppDbContext dbContext,
    IEmailService emailService,
    IEmailTemplateService templateService,
    IHermesEventOutboxPublisher hermesEvents,
    EmailJob job,
    CancellationToken cancellationToken)
  {
    var now = DateTime.UtcNow;
    try
    {
      var rendered = await templateService.RenderAsync(job.TemplateKey, job.PayloadJson, cancellationToken: cancellationToken);
      await emailService.SendEmailAsync(job.ToEmail, rendered.Subject, rendered.HtmlBody, cancellationToken);

      job.Status = "sent";
      job.SentAt = DateTime.UtcNow;
      job.UpdatedAt = DateTime.UtcNow;
      dbContext.EmailSendLogs.Add(new EmailSendLog
      {
        EmailJobId = job.Id,
        ToEmail = job.ToEmail,
        TemplateKey = job.TemplateKey,
        Status = "sent",
        SentAt = job.SentAt
      });
    }
    catch (Exception ex)
    {
      job.RetryCount += 1;
      job.Status = job.RetryCount >= MaxRetries ? "dead" : "queued";
      job.ErrorMessage = ex.Message;
      job.ScheduledAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, job.RetryCount));
      job.UpdatedAt = DateTime.UtcNow;
      dbContext.EmailSendLogs.Add(new EmailSendLog
      {
        EmailJobId = job.Id,
        ToEmail = job.ToEmail,
        TemplateKey = job.TemplateKey,
        Status = "failed",
        FailedAt = DateTime.UtcNow,
        ErrorMessage = ex.Message
      });
    }

    if (job.Status == "dead")
    {
      await hermesEvents.EnqueueAdminEmailEventAsync(
        "critical_email_dead",
        job.Id,
        new
        {
          jobId = job.Id,
          maskedToEmail = MaskEmail(job.ToEmail),
          job.TemplateKey,
          retryCount = job.RetryCount,
          errorCategory = ClassifyEmailError(job.ErrorMessage),
          lastError = Truncate(job.ErrorMessage, 300),
          detectedAt = DateTimeOffset.UtcNow
        },
        $"critical_email_dead:Email:{job.Id:N}:{job.RetryCount}",
        cancellationToken);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static string MaskEmail(string? email)
  {
    if (string.IsNullOrWhiteSpace(email)) return string.Empty;
    var trimmed = email.Trim();
    var at = trimmed.IndexOf('@');
    if (at <= 1) return "***";
    return $"{trimmed[0]}***{trimmed[at..]}";
  }

  private static string ClassifyEmailError(string? error)
  {
    if (string.IsNullOrWhiteSpace(error)) return "unknown";
    var lower = error.ToLowerInvariant();
    if (lower.Contains("auth", StringComparison.Ordinal) || lower.Contains("credential", StringComparison.Ordinal) || lower.Contains("login", StringComparison.Ordinal)) return "auth";
    if (lower.Contains("timeout", StringComparison.Ordinal) || lower.Contains("timed out", StringComparison.Ordinal)) return "timeout";
    if (lower.Contains("smtp", StringComparison.Ordinal)) return "smtp";
    if (lower.Contains("rate", StringComparison.Ordinal) || lower.Contains("quota", StringComparison.Ordinal) || lower.Contains("limit", StringComparison.Ordinal)) return "quota";
    return "delivery";
  }

  private static string Truncate(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..Math.Max(0, maxLength - 1)] + "…";
  }
}
