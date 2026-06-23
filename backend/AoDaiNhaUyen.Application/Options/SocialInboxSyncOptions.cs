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
}
