using System.Text.Json;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class EmailQueueService(AppDbContext dbContext) : IEmailQueueService
{
  public async Task<Guid> QueueAsync(
    string toEmail,
    string templateKey,
    object payload,
    DateTime? scheduledAt = null,
    CancellationToken cancellationToken = default)
  {
    var job = CreateJob(toEmail, templateKey, payload, scheduledAt);
    dbContext.EmailJobs.Add(job);
    await dbContext.SaveChangesAsync(cancellationToken);
    return job.Id;
  }

  public EmailJob Enqueue(
    string toEmail,
    string templateKey,
    object payload,
    DateTime? scheduledAt = null)
  {
    var job = CreateJob(toEmail, templateKey, payload, scheduledAt);
    dbContext.EmailJobs.Add(job);
    return job;
  }

  private static EmailJob CreateJob(string toEmail, string templateKey, object payload, DateTime? scheduledAt)
  {
    if (string.IsNullOrWhiteSpace(toEmail))
    {
      throw new ArgumentException("Email người nhận không hợp lệ.", nameof(toEmail));
    }

    return new EmailJob
    {
      ToEmail = toEmail.Trim().ToLowerInvariant(),
      TemplateKey = templateKey.Trim(),
      PayloadJson = JsonSerializer.Serialize(payload),
      Status = "queued",
      ScheduledAt = scheduledAt ?? DateTime.UtcNow
    };
  }
}
