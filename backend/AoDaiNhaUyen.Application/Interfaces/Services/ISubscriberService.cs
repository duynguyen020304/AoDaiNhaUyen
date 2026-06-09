using AoDaiNhaUyen.Application.DTOs.Marketing;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ISubscriberService
{
  Task<SubscribeResultDto> SubscribeAsync(string email, string source, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
  Task<SubscribeResultDto> ConfirmAsync(string token, CancellationToken cancellationToken = default);
  Task<SubscribeResultDto> UnsubscribeAsync(string token, CancellationToken cancellationToken = default);
}
