using AoDaiNhaUyen.Application.DTOs.Marketing;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICustomerEventService
{
  Task<TrackCustomerEventResultDto> TrackAsync(
    Guid? userId,
    TrackCustomerEventRequest request,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);
}
