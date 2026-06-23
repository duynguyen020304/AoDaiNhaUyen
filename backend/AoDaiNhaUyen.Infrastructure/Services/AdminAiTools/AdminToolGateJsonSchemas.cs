namespace AoDaiNhaUyen.Infrastructure.Services.AdminAiTools;

public static class AdminToolGateJsonSchemas
{
  public static object GateDecision { get; } = new
  {
    type = "object",
    properties = new Dictionary<string, object?>
    {
      ["action"] = new { type = "string", @enum = new[] { "execute", "ask_clarification", "reject", "require_prerequisite_tool" } },
      // Gemini response_schema does not support JSON Schema union types like ["string", "null"].
      // Use empty string/object instead of null for optional values.
      ["toolName"] = new { type = "string" },
      ["arguments"] = new { type = "object" },
      ["message"] = new { type = "string" }
    },
    required = new[] { "action", "toolName", "arguments", "message" }
  };
}
