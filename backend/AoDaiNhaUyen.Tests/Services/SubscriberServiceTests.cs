using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class SubscriberServiceTests
{
  [Fact]
  public async Task SubscribeAsync_CreatesPendingSubscriberConsentAndEmailJob()
  {
    await using var dbContext = CreateDbContext();
    var emailQueue = new RecordingEmailQueueService(dbContext);
    var service = CreateService(dbContext, emailQueue);

    var result = await service.SubscribeAsync(" UYEN@example.com ", "footer", "127.0.0.1", "agent");

    Assert.Equal("pending", result.Status);
    var subscriber = await dbContext.Subscribers.Include(x => x.Consents).SingleAsync();
    Assert.Equal("uyen@example.com", subscriber.Email);
    Assert.Equal("pending", subscriber.Status);
    Assert.NotEmpty(subscriber.ConfirmationToken);
    Assert.NotEmpty(subscriber.UnsubscribeToken);
    var consent = Assert.Single(subscriber.Consents);
    Assert.False(consent.IsOptIn);
    Assert.Equal("footer", consent.Source);
    var job = await dbContext.EmailJobs.SingleAsync();
    Assert.Equal("uyen@example.com", job.ToEmail);
    Assert.Equal("marketing.confirm_subscription", job.TemplateKey);
    Assert.Single(emailQueue.EnqueuedJobs);
  }

  [Fact]
  public async Task ConfirmAsync_ActivatesSubscriberAndAddsOptInConsent()
  {
    await using var dbContext = CreateDbContext();
    var subscriber = new Subscriber
    {
      Email = "uyen@example.com",
      Status = "pending",
      ConfirmationToken = "confirm-token",
      UnsubscribeToken = "unsubscribe-token"
    };
    dbContext.Subscribers.Add(subscriber);
    await dbContext.SaveChangesAsync();
    var service = CreateService(dbContext, new RecordingEmailQueueService(dbContext));

    var result = await service.ConfirmAsync("confirm-token");

    Assert.Equal("active", result.Status);
    var updated = await dbContext.Subscribers.Include(x => x.Consents).SingleAsync();
    Assert.Equal("active", updated.Status);
    Assert.NotNull(updated.ConfirmedAt);
    Assert.Contains(updated.Consents, x => x.IsOptIn && x.Source == "double_opt_in" && x.RevokedAt == null);
  }

  [Fact]
  public async Task UnsubscribeAsync_RevokesEmailConsents()
  {
    await using var dbContext = CreateDbContext();
    var subscriber = new Subscriber
    {
      Email = "uyen@example.com",
      Status = "active",
      ConfirmationToken = "confirm-token",
      UnsubscribeToken = "unsubscribe-token",
      Consents =
      {
        new MarketingConsent
        {
          Channel = "email",
          IsOptIn = true,
          Source = "double_opt_in",
          ConsentedAt = DateTime.UtcNow
        }
      }
    };
    dbContext.Subscribers.Add(subscriber);
    await dbContext.SaveChangesAsync();
    var service = CreateService(dbContext, new RecordingEmailQueueService(dbContext));

    var result = await service.UnsubscribeAsync("unsubscribe-token");

    Assert.Equal("unsubscribed", result.Status);
    var updated = await dbContext.Subscribers.Include(x => x.Consents).SingleAsync();
    Assert.Equal("unsubscribed", updated.Status);
    Assert.NotNull(updated.UnsubscribedAt);
    var consent = Assert.Single(updated.Consents);
    Assert.False(consent.IsOptIn);
    Assert.NotNull(consent.RevokedAt);
  }

  private static SubscriberService CreateService(AppDbContext dbContext, RecordingEmailQueueService emailQueue)
  {
    return new SubscriberService(
      dbContext,
      emailQueue,
      Options.Create(new EmailSettings
      {
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        SmtpUsername = "user",
        SmtpPassword = "password",
        FromEmail = "noreply@example.com",
        FromName = "Ao Dai Nha Uyen",
        ApiBaseUrl = "http://localhost:5043",
        FrontendBaseUrl = "http://localhost:5173"
      }));
  }

  private static AppDbContext CreateDbContext()
  {
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
  }

  private sealed class RecordingEmailQueueService(AppDbContext dbContext) : AoDaiNhaUyen.Application.Interfaces.Services.IEmailQueueService
  {
    public List<EmailJob> EnqueuedJobs { get; } = [];

    public async Task<Guid> QueueAsync(string toEmail, string templateKey, object payload, DateTime? scheduledAt = null, CancellationToken cancellationToken = default)
    {
      var job = Enqueue(toEmail, templateKey, payload, scheduledAt);
      await dbContext.SaveChangesAsync(cancellationToken);
      return job.Id;
    }

    public EmailJob Enqueue(string toEmail, string templateKey, object payload, DateTime? scheduledAt = null)
    {
      var job = new EmailJob
      {
        ToEmail = toEmail.Trim().ToLowerInvariant(),
        TemplateKey = templateKey,
        PayloadJson = "{}",
        ScheduledAt = scheduledAt ?? DateTime.UtcNow
      };
      dbContext.EmailJobs.Add(job);
      EnqueuedJobs.Add(job);
      return job;
    }
  }
}
