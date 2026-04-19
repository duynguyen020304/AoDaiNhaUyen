using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class ZaloOAuthSettings
{
  [Required(AllowEmptyStrings = false)]
  public string AppId { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string SecretKey { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string RedirectUri { get; set; } = string.Empty;
}
