namespace AoDaiNhaUyen.Application.Options;

public sealed class SocialAutoReplyOptions
{
  public const string SectionName = "SocialAutoReply";

  public bool Enabled { get; init; } = true;
  public int DebounceSeconds { get; init; } = 30;
  public int PollIntervalSeconds { get; init; } = 3;
  public int BatchSize { get; init; } = 20;
  public int MaxRecentContextMessages { get; init; } = 10;
  public bool SkipBacklogBeforeConnection { get; init; } = true;
}
