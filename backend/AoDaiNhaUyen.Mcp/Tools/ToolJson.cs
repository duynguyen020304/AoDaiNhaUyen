using System.Text.Json;
using System.Text.Json.Serialization;

namespace AoDaiNhaUyen.Mcp.Tools;

internal static class ToolJson
{
  private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  public static string Ok(object value) => JsonSerializer.Serialize(value, Options);

  public static string Error(string message, string? code = null) =>
    JsonSerializer.Serialize(new { error = new { code, message } }, Options);

  public static string ServiceMissing(string serviceName) =>
    Error($"{serviceName} service chưa được inject.", "service_missing");
}
