namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminShopEventContextService
{
  Task<string?> GetRecentContextAsync(CancellationToken cancellationToken = default);
}
