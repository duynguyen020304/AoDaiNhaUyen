namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record EmailTemplateListItem(Guid Id, string Key, string Name, string Subject, string Locale, int Version, bool IsActive, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record EmailTemplateDetail(Guid Id, string Key, string Name, string Subject, string? Preheader, string HtmlBody, string? TextBody, string Locale, int Version, bool IsActive, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CreateEmailTemplateRequest(string Key, string Name, string Subject, string? Preheader, string HtmlBody, string? TextBody, string Locale);
public sealed record UpdateEmailTemplateRequest(string Name, string Subject, string? Preheader, string HtmlBody, string? TextBody, string Locale, bool IsActive);

public sealed record SubscriberListItem(Guid Id, string Email, string Status, DateTime? SubscribedAt, DateTime? UnsubscribedAt, DateTime? LastSentAt, Guid? UserId, bool IsDeleted);
public sealed record SubscriberDetail(Guid Id, string Email, string Status, DateTime? SubscribedAt, DateTime? UnsubscribedAt, DateTime? LastSentAt, DateTime? LastOpenAt, DateTime? LastClickAt, Guid? UserId, bool IsDeleted, IReadOnlyList<ConsentRecord> Consents);
public sealed record ConsentRecord(string Channel, bool IsOptIn, string Source, DateTime? ConsentedAt, DateTime? RevokedAt);
public sealed record ImportSubscribersRequest(IReadOnlyList<string> Emails, string Source);
public sealed record ImportSubscribersResult(int Imported, int Skipped);

public sealed record EmailJobListItem(Guid Id, string ToEmail, string TemplateKey, string Status, int RetryCount, DateTime ScheduledAt, DateTime? SentAt, string? ErrorMessage);
public sealed record EmailJobDetail(Guid Id, string ToEmail, string TemplateKey, string PayloadJson, string Status, int RetryCount, DateTime ScheduledAt, DateTime? SentAt, string? ErrorMessage, IReadOnlyList<SendLogRecord> Logs);
public sealed record SendLogRecord(string Status, DateTime? SentAt, DateTime? FailedAt, string? ErrorMessage);

public sealed record MarketingStats(int TotalSubscribers, int ActiveSubscribers, int PendingSubscribers, int UnsubscribedSubscribers, int QueuedJobs, int SentJobsToday, int FailedJobs, int TemplateCount);
