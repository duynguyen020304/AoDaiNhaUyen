using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class GoogleOAuthSettings
{
  [Required(AllowEmptyStrings = false)]
  public string ClientId { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string ClientSecret { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string RedirectUri { get; set; } = string.Empty;
}
