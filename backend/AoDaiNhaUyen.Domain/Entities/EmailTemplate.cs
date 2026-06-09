using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class EmailTemplate : BaseEntity
{
  public required string Key { get; set; }
  public required string Name { get; set; }
  public required string Subject { get; set; }
  public string? Preheader { get; set; }
  public required string HtmlBody { get; set; }
  public string? TextBody { get; set; }
  public string Locale { get; set; } = "vi-VN";
  public int Version { get; set; } = 1;
}
