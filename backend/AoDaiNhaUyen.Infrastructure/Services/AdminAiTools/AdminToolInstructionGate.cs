using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services.AdminAiTools;

public sealed class AdminToolInstructionGate(
  IAdminLlmProvider llm,
  IAdminToolInstructionRegistry instructions,
  ISafetyGate safety,
  AdminToolInstructionPromptBuilder promptBuilder,
  IOptions<AdminToolGateOptions> options,
  ILogger<AdminToolInstructionGate> logger)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<ToolPreparationResult> GateAsync(
    string toolName,
    string argsJson,
    IReadOnlyList<AdminLlmMessage> history,
    CancellationToken ct)
  {
    var risk = await safety.ClassifyAsync(toolName, ct);
    if (!instructions.TryGetInstruction(toolName, out var instruction))
      return new(ToolPreparationAction.Reject, toolName, null, "Tool ghi thiếu instruction nên bị chặn.", null, true);

    try
    {
      var prompts = promptBuilder.Build(toolName, argsJson, instruction, history, options.Value.GateMaxContextChars);
      var decision = await llm.CompleteJsonAsync<ToolGateDecision>(prompts.SystemPrompt, prompts.UserPrompt, AdminToolGateJsonSchemas.GateDecision, ct);
      return MapDecision(toolName, argsJson, decision);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      logger.LogWarning(ex, "[AdminToolGate] LLM gate failed for {ToolName}.", toolName);
      return risk == RiskLevel.Read
        ? new(ToolPreparationAction.Execute, toolName, argsJson)
        : new(ToolPreparationAction.Reject, toolName, null, "Không kiểm tra được tool ghi qua LLM gate. Hành động bị chặn an toàn.", null, true);
    }
  }

  private static ToolPreparationResult MapDecision(string originalToolName, string originalArgsJson, ToolGateDecision decision)
  {
    var action = (decision.Action ?? string.Empty).Trim().ToLowerInvariant();
    var argsJson = HasNonEmptyArgumentObject(decision.Arguments)
      ? decision.Arguments!.Value.GetRawText()
      : originalArgsJson;

    return action switch
    {
      "execute" => new(ToolPreparationAction.Execute, originalToolName, argsJson, decision.Message),
      "ask_clarification" => new(ToolPreparationAction.AskClarification, originalToolName, null, decision.Message ?? "Cần thêm thông tin để thực hiện.", null, true),
      "reject" => new(ToolPreparationAction.Reject, originalToolName, null, decision.Message ?? "Hành động bị chặn bởi tool gate.", null, true),
      "require_prerequisite_tool" => new(ToolPreparationAction.RequirePrerequisiteTool, originalToolName, argsJson, decision.Message ?? "Cần lookup bổ sung trước khi thực hiện.", null, false),
      _ => new(ToolPreparationAction.Reject, originalToolName, null, "LLM gate trả action không hợp lệ.", null, true)
    };
  }

  private static bool HasNonEmptyArgumentObject(JsonElement? arguments) =>
    arguments.HasValue
    && arguments.Value.ValueKind == JsonValueKind.Object
    && arguments.Value.EnumerateObject().Any();
}
