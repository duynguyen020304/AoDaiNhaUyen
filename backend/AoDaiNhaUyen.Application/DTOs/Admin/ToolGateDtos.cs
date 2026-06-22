using System.Text.Json;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

public enum ToolPreparationAction
{
  Execute,
  AskClarification,
  Reject,
  RequirePrerequisiteTool
}

public sealed record ToolPreparationResult(
  ToolPreparationAction Action,
  string? ToolName,
  string? ArgumentsJson,
  string? Message = null,
  string? HumanSummary = null,
  bool IsTerminal = false);

public sealed record ToolInstruction(
  string ToolName,
  string Purpose,
  string BeforeUse,
  string RequiredChecks,
  string ArgumentRules,
  string SafetyRules,
  string ClarificationRules,
  string ResultRules);

public sealed record ToolPrerequisite(
  string Condition,
  string ToolName,
  string ArgumentMapping);

public sealed record ToolGateDecision(
  string Action,
  string? ToolName,
  JsonElement? Arguments,
  string? Message);
