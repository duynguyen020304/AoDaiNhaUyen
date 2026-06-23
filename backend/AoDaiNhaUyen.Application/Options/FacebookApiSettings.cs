using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class FacebookApiSettings
{
  public const string SectionName = "FacebookApi";

  public string AppId { get; set; } = string.Empty;

  public string AppSecret { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string GraphApiVersion { get; set; } = "v25.0";

  public bool UseMockData { get; set; }
}
