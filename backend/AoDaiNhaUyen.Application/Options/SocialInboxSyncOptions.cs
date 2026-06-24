namespace AoDaiNhaUyen.Application.Options;

public sealed class SocialInboxSyncOptions
{
  public const string SectionName = "SocialInboxSync";

  public bool Enabled { get; init; } = true;
  public int PollIntervalSeconds { get; init; } = 10;
  public int AccountBatchSize { get; init; } = 10;
  public int ListLimit { get; init; } = 25;
  public int ItemLimit { get; init; } = 50;
  public int MaxPostsPerAccount { get; init; } = 10;
  public int MaxConversationsPerAccount { get; init; } = 20;
  public bool SyncComments { get; init; } = true;
  public bool SyncMessages { get; init; } = true;

  /// <summary>
  /// When true, inbound Facebook Messenger image attachments are downloaded
  /// (using the Page Access Token) and persisted to S3 under
  /// <c>private/social-inbox/{conversationId}</c> so the Hermes agent can fetch
  /// a stable presigned URL for AI try-on. Failures are non-fatal
  /// (<c>SocialInboxMessage.StoredImageKey</c> stays null and the agent asks
  /// the customer to resend the photo). Set to false to disable S3 cost.
  /// </summary>
  public bool DownloadInboundImages { get; init; } = true;

  /// <summary>
  /// Maximum byte size for a downloaded inbound image (default 8MB, matching
  /// the try-on person-image limit).
  /// </summary>
  public int MaxInboundImageBytes { get; init; } = 8 * 1024 * 1024;
}
