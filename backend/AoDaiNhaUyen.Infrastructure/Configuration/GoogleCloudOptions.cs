namespace AoDaiNhaUyen.Infrastructure.Configuration;

public sealed class GoogleCloudOptions
{
  public string? ApiKey { get; set; }
  public string? ProjectId { get; set; }
  public string Location { get; set; } = "us-central1";
  public string VirtualTryOnModel { get; set; } = "gemini-3.1-flash-image-preview";
  public int TimeoutSeconds { get; set; } = 1200;
}
