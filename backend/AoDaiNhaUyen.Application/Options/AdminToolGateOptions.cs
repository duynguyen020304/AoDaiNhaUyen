namespace AoDaiNhaUyen.Application.Options;

public sealed class AdminToolGateOptions
{
  public const string SectionName = "AdminToolGate";
  public bool EnableDeterministicGate { get; set; } = true;
  public bool EnableLlmGate { get; set; } = false;
  public string[] LlmGateToolNames { get; set; } = [];
  public int GateMaxContextChars { get; set; } = 6000;
}
