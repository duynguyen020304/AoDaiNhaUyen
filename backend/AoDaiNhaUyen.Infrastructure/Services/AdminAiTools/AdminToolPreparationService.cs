using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Common;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AoDaiNhaUyen.Infrastructure.Services.AdminAiTools;

public sealed class AdminToolPreparationService(
  AdminToolArgumentValidator validator,
  AdminToolPrerequisiteResolver resolver,
  AdminToolInstructionGate llmGate,
  ISafetyGate safety,
  IOptions<AdminToolGateOptions> options) : IAdminToolPreparationService
{
  private static readonly HashSet<string> DefaultLlmGateTools = new(StringComparer.Ordinal)
  {
    "confirm_order", "start_processing_order", "ship_order", "complete_order", "cancel_order", "delete_order", "restore_order", "update_order_address", "update_order_items",
    "update_user_status", "update_user_role", "update_user_profile", "delete_user", "restore_user", "create_role", "update_role", "delete_role",
    "delete_product", "toggle_product_status", "update_product", "delete_variant", "update_variant", "update_variant_stock",
    "send_marketing_campaign", "retry_email_job", "cancel_email_job", "delete_email_job", "update_promo_code", "toggle_promo_code", "delete_promo_code",
    "upload_media", "delete_media"
  };

  public async Task<ToolPreparationResult> PrepareAsync(string toolName, string draftArgsJson, IReadOnlyList<ToolDefinition> tools, IReadOnlyList<AdminLlmMessage> history, Guid adminUserId, CancellationToken ct = default)
  {
    if (!options.Value.EnableDeterministicGate)
      return new(ToolPreparationAction.Execute, toolName, string.IsNullOrWhiteSpace(draftArgsJson) ? "{}" : draftArgsJson);

    var first = await validator.ValidateAsync(toolName, draftArgsJson, tools, requireGuidFields: false, ct);
    if (first.Action == ToolPreparationAction.Reject && IsGuidOnlyRejection(first.Message))
    {
      var prereq = await resolver.ResolveAsync(toolName, draftArgsJson, ct);
      if (prereq is null) return first;
      if (prereq.Action != ToolPreparationAction.Execute) return prereq;
      first = prereq;
    }
    else if (first.Action != ToolPreparationAction.Execute)
    {
      return first;
    }

    var currentTool = first.ToolName ?? toolName;
    var currentArgs = first.ArgumentsJson ?? draftArgsJson;
    for (var i = 0; i < 2; i++)
    {
      var prereq = await resolver.ResolveAsync(currentTool, currentArgs, ct);
      if (prereq is null) break;
      if (prereq.Action != ToolPreparationAction.Execute) return prereq;
      currentTool = prereq.ToolName ?? currentTool;
      currentArgs = prereq.ArgumentsJson ?? currentArgs;
    }

    var second = await validator.ValidateAsync(currentTool, currentArgs, tools, requireGuidFields: true, ct);
    if (second.Action != ToolPreparationAction.Execute) return second;
    currentTool = second.ToolName ?? currentTool;
    currentArgs = second.ArgumentsJson ?? currentArgs;

    if (await ShouldRunLlmGateAsync(currentTool, ct))
    {
      var gate = await llmGate.GateAsync(currentTool, currentArgs, history, ct);
      if (gate.Action != ToolPreparationAction.Execute) return gate;
      if (!CriticalTargetsUnchanged(currentArgs, gate.ArgumentsJson ?? "{}"))
        return new(ToolPreparationAction.Reject, currentTool, null, "LLM gate đã thay đổi định danh mục tiêu; hành động bị chặn an toàn.", null, true);
      currentTool = gate.ToolName ?? currentTool;
      currentArgs = gate.ArgumentsJson ?? currentArgs;
      var final = await validator.ValidateAsync(currentTool, currentArgs, tools, requireGuidFields: true, ct);
      if (final.Action != ToolPreparationAction.Execute) return final;
      return final;
    }

    return second;
  }

  private async Task<bool> ShouldRunLlmGateAsync(string toolName, CancellationToken ct)
  {
    if (!options.Value.EnableLlmGate) return false;
    var configured = options.Value.LlmGateToolNames is { Length: > 0 }
      ? options.Value.LlmGateToolNames.ToHashSet(StringComparer.Ordinal)
      : DefaultLlmGateTools;
    if (!configured.Contains(toolName)) return false;
    return await safety.ClassifyAsync(toolName, ct) != RiskLevel.Read;
  }

  private static bool CriticalTargetsUnchanged(string beforeJson, string afterJson)
  {
    using var before = JsonDocument.Parse(string.IsNullOrWhiteSpace(beforeJson) ? "{}" : beforeJson);
    using var after = JsonDocument.Parse(string.IsNullOrWhiteSpace(afterJson) ? "{}" : afterJson);
    foreach (var field in CriticalTargetFields)
    {
      if (!before.RootElement.TryGetProperty(field, out var beforeValue)) continue;
      if (beforeValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) continue;
      if (!after.RootElement.TryGetProperty(field, out var afterValue)) return false;
      if (!string.Equals(beforeValue.ToString(), afterValue.ToString(), StringComparison.OrdinalIgnoreCase))
        return false;
    }
    return true;
  }

  private static readonly string[] CriticalTargetFields =
  [
    "id", "orderId", "productId", "variantId", "userId", "roleId", "promoId", "commentId", "postId", "pageId"
  ];

  private static bool IsGuidOnlyRejection(string? message) =>
    message?.Contains("GUID", StringComparison.OrdinalIgnoreCase) == true
    || message?.Contains("orderCode", StringComparison.OrdinalIgnoreCase) == true;
}
