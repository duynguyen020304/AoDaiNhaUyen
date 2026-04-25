using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class ImageValidationOptions
{
  public const string SectionName = "ImageValidation";

  [Range(1, 365)]
  public int CacheTtlDays { get; set; } = 7;

  [Range(1, long.MaxValue)]
  public long MaxImageBytes { get; set; } = 8 * 1024 * 1024;

  [Range(1, int.MaxValue)]
  public int MinWidth { get; set; } = 64;

  [Range(1, int.MaxValue)]
  public int MinHeight { get; set; } = 64;

  [Range(1, int.MaxValue)]
  public int MaxWidth { get; set; } = 8192;

  [Range(1, int.MaxValue)]
  public int MaxHeight { get; set; } = 8192;

  public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}
