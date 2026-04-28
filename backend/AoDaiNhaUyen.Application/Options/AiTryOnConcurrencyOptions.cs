using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class AiTryOnConcurrencyOptions
{
  public const string SectionName = "AiTryOnConcurrency";

  [Range(1, int.MaxValue)]
  public int MaxConcurrentGenerations { get; set; } = 3;
}
