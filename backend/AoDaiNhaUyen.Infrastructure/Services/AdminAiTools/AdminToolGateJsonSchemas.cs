namespace AoDaiNhaUyen.Infrastructure.Services.AdminAiTools;

public static class AdminToolGateJsonSchemas
{
  public static object GateDecision { get; } = new
  {
    type = "object",
    properties = new Dictionary<string, object?>
    {
      ["action"] = new { type = "string", @enum = new[] { "execute", "ask_clarification", "reject", "require_prerequisite_tool" } },
      ["toolName"] = new { type = new[] { "string", "null" } },
      ["arguments"] = new { type = new[] { "object", "null" } },
      ["message"] = new { type = new[] { "string", "null" } }
    },
    required = new[] { "action", "toolName", "arguments", "message" }
  };
}
