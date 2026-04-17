using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class FacebookOAuthSettings
{
  [Required(AllowEmptyStrings = false)]
  public string AppId { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string AppSecret { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string RedirectUri { get; set; } = string.Empty;
}
