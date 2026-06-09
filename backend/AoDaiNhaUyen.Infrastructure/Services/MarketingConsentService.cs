using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class MarketingConsentService(AppDbContext dbContext) : IMarketingConsentService
{
  public async Task<bool> HasOptInAsync(string email, string channel = "email", CancellationToken cancellationToken = default)
  {
    var normalizedEmail = email.Trim().ToLowerInvariant();
    return await dbContext.MarketingConsents
      .AsNoTracking()
      .AnyAsync(x => x.Channel == channel
        && x.IsOptIn
        && x.RevokedAt == null
        && x.Subscriber.Email == normalizedEmail
        && x.Subscriber.Status == "active", cancellationToken);
  }
}
