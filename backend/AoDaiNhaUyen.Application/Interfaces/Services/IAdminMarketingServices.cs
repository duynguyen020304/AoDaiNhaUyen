using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminEmailTemplateService
{
  Task<(IReadOnlyList<EmailTemplateListItem> Items, int TotalItem)> GetListAsync(string? search, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default);
  Task<EmailTemplateDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<EmailTemplateDetail> CreateAsync(CreateEmailTemplateRequest request, CancellationToken cancellationToken = default);
  Task<EmailTemplateDetail?> UpdateAsync(Guid id, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default);
  Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
  Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAdminSubscriberService
{
  Task<(IReadOnlyList<SubscriberListItem> Items, int TotalItem)> GetListAsync(string? search, string? status, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default);
  Task<SubscriberDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<bool> UnsubscribeAsync(Guid id, CancellationToken cancellationToken = default);
  Task<ImportSubscribersResult> BulkImportAsync(IReadOnlyList<string> emails, string source, CancellationToken cancellationToken = default);
}

public interface IAdminEmailJobService
{
  Task<(IReadOnlyList<EmailJobListItem> Items, int TotalItem)> GetListAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
  Task<EmailJobDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default);
  Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAdminMarketingStatsService
{
  Task<MarketingStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

public interface IAdminMarketingCampaignService
{
  Task<IReadOnlyList<MarketingContentOption>> GetContentOptionsAsync(CancellationToken cancellationToken = default);
  Task<MarketingCampaignSendResult> QueueCampaignAsync(SendMarketingCampaignRequest request, CancellationToken cancellationToken = default);
}
