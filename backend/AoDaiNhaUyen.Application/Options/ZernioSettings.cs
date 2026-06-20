using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class ZernioSettings
{
  public const string SectionName = "Zernio";

  [Required(AllowEmptyStrings = false)]
  public string ApiKey { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string ApiUrl { get; set; } = "https://zernio.com";

  public string? WebhookSecret { get; set; }
}
