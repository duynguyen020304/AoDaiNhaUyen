namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IMarketingConsentService
{
  Task<bool> HasOptInAsync(string email, string channel = "email", CancellationToken cancellationToken = default);
}
