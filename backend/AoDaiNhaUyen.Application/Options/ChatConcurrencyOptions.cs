using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class ChatConcurrencyOptions
{
  public const string SectionName = "ChatConcurrency";

  [Range(1, int.MaxValue)]
  public int MaxConcurrentThreads { get; set; } = 10;
}
