namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record EmailTemplateListItem(Guid Id, string Key, string Name, string Subject, string TemplateType, string Locale, int Version, bool IsSystem, bool IsActive, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record EmailTemplateDetail(Guid Id, string Key, string Name, string Subject, string? Preheader, string TemplateType, string ConfigJson, string Locale, int Version, bool IsSystem, bool IsActive, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CreateEmailTemplateRequest(string Key, string Name, string Subject, string? Preheader, string TemplateType, string? ConfigJson, string Locale, bool IsSystem);
public sealed record UpdateEmailTemplateRequest(string Name, string Subject, string? Preheader, string TemplateType, string? ConfigJson, string Locale, bool IsSystem, bool IsActive);

public sealed record SubscriberListItem(Guid Id, string Email, string Status, DateTime? SubscribedAt, DateTime? UnsubscribedAt, DateTime? LastSentAt, Guid? UserId, bool IsDeleted);
public sealed record SubscriberDetail(Guid Id, string Email, string Status, DateTime? SubscribedAt, DateTime? UnsubscribedAt, DateTime? LastSentAt, DateTime? LastOpenAt, DateTime? LastClickAt, Guid? UserId, bool IsDeleted, IReadOnlyList<ConsentRecord> Consents);
public sealed record ConsentRecord(string Channel, bool IsOptIn, string Source, DateTime? ConsentedAt, DateTime? RevokedAt);
public sealed record ImportSubscribersRequest(IReadOnlyList<string> Emails, string Source);
public sealed record ImportSubscribersResult(int Imported, int Skipped);

public sealed record QueueSingleEmailJobRequest(
  string? ToEmail,
  Guid? CustomerId,
  Guid? OrderId,
  string TemplateKey,
  string Subject,
  string? Preheader,
  string? Intro,
  string? Body,
  string? CtaLabel,
  string? CtaUrl,
  string Purpose,
  DateTime? ScheduledAt,
  string IdempotencyKey);
public sealed record QueueSingleEmailJobResponse(Guid JobId, string ToEmail, string TemplateKey, DateTime ScheduledAt, bool AlreadyQueued);

public sealed record EmailJobListItem(Guid Id, string ToEmail, string TemplateKey, string Status, int RetryCount, DateTime ScheduledAt, DateTime? SentAt, string? ErrorMessage);
public sealed record EmailJobDetail(Guid Id, string ToEmail, string TemplateKey, string PayloadJson, string Status, int RetryCount, DateTime ScheduledAt, DateTime? SentAt, string? ErrorMessage, IReadOnlyList<SendLogRecord> Logs);
public sealed record SendLogRecord(string Status, DateTime? SentAt, DateTime? FailedAt, string? ErrorMessage);

public sealed record MarketingStats(int TotalSubscribers, int ActiveSubscribers, int PendingSubscribers, int UnsubscribedSubscribers, int QueuedJobs, int SentJobsToday, int FailedJobs, int TemplateCount);

public sealed record MarketingContentOption(Guid Id, string Type, string Title, string? Subtitle, string? Url, string? Badge, string HtmlSnippet);
public sealed record MarketingCampaignAttachmentRequest(string Type, Guid? Id, string Title, string? Url, string? Description, string? Code);
public sealed record SendMarketingCampaignRequest(
  string RecipientMode,
  IReadOnlyList<Guid>? SubscriberIds,
  IReadOnlyList<string>? ManualEmails,
  string TemplateKey,
  string Subject,
  string? Preheader,
  string? Intro,
  string? BodyHtml,
  string? CtaLabel,
  string? CtaUrl,
  IReadOnlyList<MarketingCampaignAttachmentRequest>? Attachments,
  DateTime? ScheduledAt);
public sealed record MarketingCampaignSendResult(int Queued, int Skipped, IReadOnlyList<string> SkippedEmails);
