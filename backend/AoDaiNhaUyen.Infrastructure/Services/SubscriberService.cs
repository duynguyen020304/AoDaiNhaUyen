using System.Security.Cryptography;
using System.Text;
using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class SubscriberService(
  AppDbContext dbContext,
  IEmailQueueService emailQueueService,
  IOptions<EmailSettings> emailSettings) : ISubscriberService
{
  private readonly EmailSettings emailSettings = emailSettings.Value;

  public async Task<SubscribeResultDto> SubscribeAsync(
    string email,
    string source,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
  {
    var normalizedEmail = NormalizeEmail(email);
    if (string.IsNullOrWhiteSpace(normalizedEmail))
    {
      return new SubscribeResultDto(email, "invalid", "Email không hợp lệ.");
    }

    var subscriber = await dbContext.Subscribers
      .Include(x => x.Consents)
      .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    var now = DateTime.UtcNow;
    if (subscriber is null)
    {
      subscriber = new Subscriber
      {
        Email = normalizedEmail,
        Status = "pending",
        ConfirmationToken = CreateToken(),
        UnsubscribeToken = CreateToken(),
        CreatedAt = now,
        UpdatedAt = now
      };
      dbContext.Subscribers.Add(subscriber);
    }
    else if (subscriber.Status == "active")
    {
      return new SubscribeResultDto(normalizedEmail, subscriber.Status, "Email đã đăng ký nhận tin.");
    }
    else
    {
      subscriber.Status = "pending";
      subscriber.ConfirmationToken = CreateToken();
      subscriber.UpdatedAt = now;
    }

    subscriber.Consents.Add(new MarketingConsent
    {
      Subscriber = subscriber,
      Channel = "email",
      IsOptIn = false,
      Source = string.IsNullOrWhiteSpace(source) ? "newsletter" : source.Trim(),
      IpHash = HashOrNull(ipAddress),
      UserAgentHash = HashOrNull(userAgent),
      CreatedAt = now,
      UpdatedAt = now
    });

    var confirmUrl = $"{emailSettings.FrontendBaseUrl.TrimEnd('/')}/newsletter/confirm?token={Uri.EscapeDataString(subscriber.ConfirmationToken)}";
    emailQueueService.Enqueue(normalizedEmail, "marketing.confirm_subscription", new
    {
      greeting = "Chào bạn",
      body = "Cảm ơn bạn đã đăng ký nhận tin từ Áo Dài Nhã Uyên. Vui lòng xác nhận email để hoàn tất đăng ký.",
      buttonText = "Xác nhận đăng ký",
      info = "Nếu nút không hoạt động, vui lòng sao chép liên kết bên dưới:",
      confirmUrl = confirmUrl,
      subject = "Xác nhận đăng ký nhận tin từ Áo Dài Nhã Uyên"
    });

    await dbContext.SaveChangesAsync(cancellationToken);

    return new SubscribeResultDto(normalizedEmail, "pending", "Vui lòng kiểm tra email để xác nhận đăng ký.");
  }

  public async Task<SubscribeResultDto> ConfirmAsync(string token, CancellationToken cancellationToken = default)
  {
    var subscriber = await dbContext.Subscribers
      .Include(x => x.Consents)
      .FirstOrDefaultAsync(x => x.ConfirmationToken == token, cancellationToken);
    if (subscriber is null)
    {
      return new SubscribeResultDto(string.Empty, "invalid", "Token xác nhận không hợp lệ.");
    }

    var now = DateTime.UtcNow;
    subscriber.Status = "active";
    subscriber.ConfirmedAt = now;
    subscriber.SubscribedAt = now;
    subscriber.UpdatedAt = now;
    subscriber.Consents.Add(new MarketingConsent
    {
      SubscriberId = subscriber.Id,
      Channel = "email",
      IsOptIn = true,
      Source = "double_opt_in",
      ConsentedAt = now,
      CreatedAt = now,
      UpdatedAt = now
    });

    await dbContext.SaveChangesAsync(cancellationToken);
    return new SubscribeResultDto(subscriber.Email, "active", "Đăng ký nhận tin thành công.");
  }

  public async Task<SubscribeResultDto> UnsubscribeAsync(string token, CancellationToken cancellationToken = default)
  {
    var subscriber = await dbContext.Subscribers
      .Include(x => x.Consents)
      .FirstOrDefaultAsync(x => x.UnsubscribeToken == token, cancellationToken);
    if (subscriber is null)
    {
      return new SubscribeResultDto(string.Empty, "invalid", "Token hủy đăng ký không hợp lệ.");
    }

    var now = DateTime.UtcNow;
    subscriber.Status = "unsubscribed";
    subscriber.UnsubscribedAt = now;
    subscriber.UpdatedAt = now;
    foreach (var consent in subscriber.Consents.Where(x => x.Channel == "email" && x.RevokedAt == null))
    {
      consent.IsOptIn = false;
      consent.RevokedAt = now;
      consent.UpdatedAt = now;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return new SubscribeResultDto(subscriber.Email, "unsubscribed", "Đã hủy đăng ký nhận tin.");
  }

  private static string NormalizeEmail(string email)
  {
    var normalized = email.Trim().ToLowerInvariant();
    return normalized.Contains('@') && normalized.Length <= 150 ? normalized : string.Empty;
  }

  private static string CreateToken()
  {
    return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
  }

  private static string? HashOrNull(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
  }
}
