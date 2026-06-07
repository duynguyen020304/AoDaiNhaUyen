using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public interface ISafetyGate
{
  RiskLevel Classify(string toolName);
  bool RequiresConfirmation(RiskLevel level);
  bool IsAutoApproved(RiskLevel level);
  string GetConfirmationPrompt(string toolName, string description);
}

public sealed class SafetyGate : ISafetyGate
{
  private readonly ILogger<SafetyGate> _logger;

  private static readonly Dictionary<string, RiskLevel> ToolRiskMap = new()
  {
    // Dashboard — Read
    ["get_dashboard_summary"] = RiskLevel.Read,
    ["get_revenue"] = RiskLevel.Read,
    ["get_orders_by_status"] = RiskLevel.Read,
    ["get_recent_orders"] = RiskLevel.Read,
    ["get_top_products"] = RiskLevel.Read,
    ["get_user_growth"] = RiskLevel.Read,

    // Products
    ["list_products"] = RiskLevel.Read,
    ["get_product"] = RiskLevel.Read,
    ["create_product"] = RiskLevel.Low,
    ["update_product"] = RiskLevel.Medium,
    ["delete_product"] = RiskLevel.High,
    ["restore_product"] = RiskLevel.Medium,
    ["toggle_product_status"] = RiskLevel.Medium,
    ["upload_product_image"] = RiskLevel.Low,

    // Categories
    ["list_categories"] = RiskLevel.Read,
    ["get_category"] = RiskLevel.Read,
    ["create_category"] = RiskLevel.Low,
    ["update_category"] = RiskLevel.Medium,
    ["delete_category"] = RiskLevel.High,

    // Users
    ["list_users"] = RiskLevel.Read,
    ["get_user"] = RiskLevel.Read,
    ["update_user_role"] = RiskLevel.High,
    ["update_user_status"] = RiskLevel.Medium,
  };

  public SafetyGate(ILogger<SafetyGate> logger)
  {
    _logger = logger;
  }

  public RiskLevel Classify(string toolName)
  {
    if (ToolRiskMap.TryGetValue(toolName, out var level))
    {
      _logger.LogDebug("[SafetyGate] Tool {ToolName} classified as {RiskLevel}", toolName, level);
      return level;
    }

    _logger.LogWarning("[SafetyGate] Unknown tool {ToolName}, defaulting to Medium", toolName);
    return RiskLevel.Medium;
  }

  public bool RequiresConfirmation(RiskLevel level) => level >= RiskLevel.Medium;

  public bool IsAutoApproved(RiskLevel level) => level <= RiskLevel.Low;

  public string GetConfirmationPrompt(string toolName, string description)
  {
    return $"Bạn có chắc muốn thực hiện: {description}? (Hành động: {toolName})";
  }
}
