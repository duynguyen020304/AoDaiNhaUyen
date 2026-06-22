using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services.AdminAiTools;

public sealed class AdminToolArgumentValidator(
  IAdminToolInstructionRegistry instructions,
  ISafetyGate safety,
  ILogger<AdminToolArgumentValidator> logger)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly HashSet<string> AllowedTools = AdminToolRiskService.GetDefaultToolConfigs()
    .Select(c => c.ToolName).Append("reply_to_review").Append("restore_product")
    .ToHashSet(StringComparer.Ordinal);
  private static readonly HashSet<string> BadFlags = new(StringComparer.OrdinalIgnoreCase)
    { "force", "hardDelete", "permanent", "permanentDelete", "cascade", "truncate", "drop", "purge", "deleteAll", "bypassConfirmation" };
  private static readonly Dictionary<string, string[]> GuidFields = new(StringComparer.Ordinal)
  {
    ["get_product"] = ["id"], ["update_product"] = ["id"], ["delete_product"] = ["id"], ["restore_product"] = ["id"], ["toggle_product_status"] = ["id"],
    ["list_variants"] = ["productId"], ["create_variant"] = ["productId"], ["update_variant"] = ["productId", "variantId"], ["update_variant_stock"] = ["productId", "variantId"], ["delete_variant"] = ["productId", "variantId"],
    ["update_category"] = ["id"], ["delete_category"] = ["id"], ["get_user"] = ["id"], ["update_user_status"] = ["id"], ["update_user_role"] = ["id"], ["update_user_profile"] = ["id"], ["delete_user"] = ["id"], ["restore_user"] = ["id"],
    ["update_role"] = ["id"], ["delete_role"] = ["id"], ["get_order"] = ["orderId"], ["confirm_order"] = ["orderId"], ["start_processing_order"] = ["orderId"], ["ship_order"] = ["orderId"], ["complete_order"] = ["orderId"], ["cancel_order"] = ["orderId"], ["update_order_address"] = ["orderId"], ["update_order_items"] = ["orderId"], ["delete_order"] = ["orderId"], ["restore_order"] = ["orderId"],
    ["get_media"] = ["id"], ["delete_media"] = ["id"], ["hide_review"] = ["id"], ["show_review"] = ["id"], ["delete_review"] = ["id"],
    ["get_promo_code"] = ["promoId"], ["update_promo_code"] = ["promoId"], ["toggle_promo_code"] = ["promoId"], ["delete_promo_code"] = ["promoId"],
    ["get_subscriber"] = ["id"], ["unsubscribe_subscriber"] = ["id"], ["delete_subscriber"] = ["id"], ["get_email_job"] = ["id"], ["retry_email_job"] = ["id"], ["cancel_email_job"] = ["id"], ["delete_email_job"] = ["id"],
    ["get_blog_post"] = ["id"], ["update_blog_post"] = ["id"], ["delete_blog_post"] = ["id"], ["get_hermes_report"] = ["id"]
  };
  private static readonly Dictionary<string, Dictionary<string, string[]>> Enums = new(StringComparer.Ordinal)
  {
    ["toggle_product_status"] = new(StringComparer.Ordinal) { ["status"] = ["draft", "active", "inactive", "out_of_stock"] },
    ["update_product"] = new(StringComparer.Ordinal) { ["status"] = ["draft", "active", "inactive", "out_of_stock"], ["productType"] = ["ao_dai", "phu_kien"] },
    ["update_user_status"] = new(StringComparer.Ordinal) { ["status"] = ["active", "inactive", "blocked"] },
    ["list_orders"] = new(StringComparer.Ordinal) { ["status"] = ["pending", "confirmed", "processing", "shipping", "completed", "cancelled"] },
    ["create_promo_code"] = new(StringComparer.Ordinal) { ["discountType"] = ["percentage", "fixed"] },
    ["update_promo_code"] = new(StringComparer.Ordinal) { ["discountType"] = ["percentage", "fixed"] }
  };

  public async Task<ToolPreparationResult> ValidateAsync(string toolName, string argsJson, IReadOnlyList<ToolDefinition> tools, bool requireGuidFields, CancellationToken ct)
  {
    if (!tools.Any(t => t.Name == toolName) || !AllowedTools.Contains(toolName))
      return Reject(toolName, "Công cụ không được phép hoặc không tồn tại.");
    var risk = await safety.ClassifyAsync(toolName, ct);
    var hasInstruction = instructions.TryGetInstruction(toolName, out _);
    if (risk != RiskLevel.Read && !hasInstruction) return Reject(toolName, "Tool ghi thiếu instruction nên bị chặn.");
    if (risk == RiskLevel.Read && !hasInstruction) logger.LogWarning("[AdminToolGate] Read-only tool {ToolName} has no instruction; allowing.", toolName);

    using var doc = ParseObject(argsJson, out var error);
    if (doc is null) return Reject(toolName, error ?? "JSON tham số không hợp lệ.");
    var args = doc.RootElement;
    foreach (var prop in args.EnumerateObject())
      if (BadFlags.Contains(prop.Name)) return Reject(toolName, $"Tham số nguy hiểm không được phép: {prop.Name}.");

    if (Enums.TryGetValue(toolName, out var enumRules))
      foreach (var (field, allowed) in enumRules)
        if (args.TryGetProperty(field, out var value) && value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined
          && (value.ValueKind != JsonValueKind.String || !allowed.Contains(value.GetString(), StringComparer.OrdinalIgnoreCase)))
          return Reject(toolName, $"Giá trị không hợp lệ cho {field}. Cho phép: {string.Join(", ", allowed)}.");

    if (GuidFields.TryGetValue(toolName, out var guidFields))
      foreach (var field in guidFields)
      {
        if (!args.TryGetProperty(field, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
          if (requireGuidFields) return Reject(toolName, $"Thiếu hoặc sai định dạng GUID: {field}.");
          continue;
        }
        if (value.ValueKind != JsonValueKind.String || !Guid.TryParse(value.GetString(), out _))
        {
          var raw = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
          if (!string.IsNullOrWhiteSpace(raw) && raw.StartsWith("AD-", StringComparison.OrdinalIgnoreCase))
            return Reject(toolName, $"orderCode {raw} không được đặt vào trường GUID {field}.");
          return Reject(toolName, $"Thiếu hoặc sai định dạng GUID: {field}.");
        }
      }

    return new(ToolPreparationAction.Execute, toolName, Canonicalize(args));
  }

  private static JsonDocument? ParseObject(string json, out string? error)
  {
    error = null;
    try
    {
      var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
      if (doc.RootElement.ValueKind == JsonValueKind.Object) return doc;
      doc.Dispose(); error = "Tham số tool phải là JSON object."; return null;
    }
    catch (JsonException ex) { error = $"JSON tham số không hợp lệ: {ex.Message}"; return null; }
  }

  private static string Canonicalize(JsonElement args)
  {
    var map = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var prop in args.EnumerateObject())
      if (!BadFlags.Contains(prop.Name)) map[prop.Name] = JsonSerializer.Deserialize<object?>(prop.Value.GetRawText(), JsonOptions);
    return JsonSerializer.Serialize(map, JsonOptions);
  }

  private static ToolPreparationResult Reject(string toolName, string message) =>
    new(ToolPreparationAction.Reject, toolName, null, message, message, true);
}
