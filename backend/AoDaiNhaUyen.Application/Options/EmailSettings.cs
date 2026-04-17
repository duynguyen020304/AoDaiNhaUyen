using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.Options;

public sealed class EmailSettings
{
  [Required(AllowEmptyStrings = false)]
  public string SmtpHost { get; set; } = string.Empty;

  [Range(1, 65535)]
  public int SmtpPort { get; set; } = 587;

  [Required(AllowEmptyStrings = false)]
  public string SmtpUsername { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string SmtpPassword { get; set; } = string.Empty;

  public bool EnableSsl { get; set; } = true;

  [Required(AllowEmptyStrings = false)]
  [EmailAddress]
  public string FromEmail { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string FromName { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string ApiBaseUrl { get; set; } = string.Empty;

  [Required(AllowEmptyStrings = false)]
  public string FrontendBaseUrl { get; set; } = string.Empty;
}
