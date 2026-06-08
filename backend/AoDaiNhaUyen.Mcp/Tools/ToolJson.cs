using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Mcp.Tools;

internal static class ToolJson
{
  private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  public static string Ok(object value) => JsonSerializer.Serialize(value, Options);

  public static string OkWithMeta<T>(T value, AdminToolResultMeta? meta = null, AdminToolSafety? safety = null, string? message = null) =>
    Ok(new AdminToolResult<T>(true, "ok", message, value, meta, safety));

  public static string OkPaginated<T>(
    IReadOnlyCollection<T> items,
    int total,
    int page,
    int pageSize,
    object? filtersApplied = null,
    AdminToolSafety? safety = null)
  {
    var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
    var hasMore = page < totalPages;
    var completeness = total == 0 ? "empty_result" : hasMore ? "partial_page" : "complete_page";
    var code = items.Count == 0 && total > 0 ? "empty_page_but_results_exist" : "ok";
    var message = code == "empty_page_but_results_exist"
      ? "Trang này không có dữ liệu nhưng vẫn có kết quả ở trang khác."
      : null;

    return Ok(new AdminToolResult<object>(
      true,
      code,
      message,
      new { items, total, page, pageSize },
      new AdminToolResultMeta(page, pageSize, total, totalPages, hasMore, completeness, filtersApplied),
      safety));
  }

  public static string Error(string message, string? code = null) =>
    JsonSerializer.Serialize(new AdminToolResult<object>(false, code ?? "error", message, null, null, null), Options);

  public static string ServiceMissing(string serviceName) =>
    Error($"{serviceName} service chưa được inject.", "service_missing");
}
