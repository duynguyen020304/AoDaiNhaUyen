using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public interface ISafetyGate
{
  RiskLevel Classify(string toolName);
  Task<RiskLevel> ClassifyAsync(string toolName, CancellationToken ct = default);
  bool RequiresConfirmation(RiskLevel level);
  Task<bool> RequiresConfirmationAsync(string toolName, CancellationToken ct = default);
  bool IsAutoApproved(RiskLevel level);
  string GetConfirmationPrompt(string toolName, string description);
  Task InvalidateCacheAsync(CancellationToken ct = default);
}

public sealed class SafetyGate : ISafetyGate
{
  private readonly ILogger<SafetyGate> _logger;
  private readonly AppDbContext? _dbContext;

  // Cache: toolName → (RiskLevel, RequiresConfirmation)
  private Dictionary<string, (RiskLevel Level, bool RequiresConfirmation)>? _cache;
  private readonly object _lock = new();
  private DateTime _cacheLoadedAt = DateTime.MinValue;
  private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

  // Fallback defaults — only used when DB is empty or unavailable
  private static readonly Dictionary<string, (RiskLevel Level, bool RequiresConfirmation)> DefaultMap = new()
  {
    ["get_dashboard_summary"] = (RiskLevel.Read, false),
    ["get_revenue"] = (RiskLevel.Read, false),
    ["get_orders_by_status"] = (RiskLevel.Read, false),
    ["get_recent_orders"] = (RiskLevel.Read, false),
    ["get_top_products"] = (RiskLevel.Read, false),
    ["get_user_growth"] = (RiskLevel.Read, false),
    ["list_products"] = (RiskLevel.Read, false),
    ["get_product"] = (RiskLevel.Read, false),
    ["create_product"] = (RiskLevel.Low, false),
    ["update_product"] = (RiskLevel.Medium, true),
    ["delete_product"] = (RiskLevel.High, true),
    ["restore_product"] = (RiskLevel.Medium, true),
    ["toggle_product_status"] = (RiskLevel.Medium, true),
    ["upload_product_image"] = (RiskLevel.Low, false),
    ["list_categories"] = (RiskLevel.Read, false),
    ["get_category"] = (RiskLevel.Read, false),
    ["create_category"] = (RiskLevel.Low, false),
    ["update_category"] = (RiskLevel.Medium, true),
    ["delete_category"] = (RiskLevel.High, true),
    ["list_users"] = (RiskLevel.Read, false),
    ["get_user"] = (RiskLevel.Read, false),
    ["update_user_role"] = (RiskLevel.High, true),
    ["update_user_status"] = (RiskLevel.Medium, true),
    ["list_orders"] = (RiskLevel.Read, false),
    ["get_order"] = (RiskLevel.Read, false),
    ["confirm_order"] = (RiskLevel.High, true),
    ["start_processing_order"] = (RiskLevel.High, true),
    ["ship_order"] = (RiskLevel.High, true),
    ["cancel_order"] = (RiskLevel.High, true),
    ["get_inventory_summary"] = (RiskLevel.Read, false),
    ["get_store_health_score"] = (RiskLevel.Read, false),
    ["list_recent_reviews"] = (RiskLevel.Read, false),
    ["list_recent_comments"] = (RiskLevel.Read, false),
    ["reply_to_review"] = (RiskLevel.Medium, true),
    ["reply_to_comment"] = (RiskLevel.Medium, true),
    ["list_promo_codes"] = (RiskLevel.Read, false),
    ["create_promo_code"] = (RiskLevel.High, true),
    ["create_purchase_note"] = (RiskLevel.Low, false),
    ["generate_daily_report"] = (RiskLevel.Read, false),
    ["toggle_autonomy"] = (RiskLevel.High, true),
    ["get_autonomy_status"] = (RiskLevel.Read, false),
    ["generate_product_description"] = (RiskLevel.Read, false),
    ["generate_weekly_report"] = (RiskLevel.Read, false),
    ["check_inventory_alerts"] = (RiskLevel.Read, false),
  };

  public SafetyGate(ILogger<SafetyGate> logger, AppDbContext? dbContext = null)
  {
    _logger = logger;
    _dbContext = dbContext;
  }

  public RiskLevel Classify(string toolName)
  {
    if (TryGetFromCache(toolName, out var cached))
      return cached.Level;

    if (DefaultMap.TryGetValue(toolName, out var def))
    {
      _logger.LogDebug("[SafetyGate] Tool {ToolName} classified from defaults as {RiskLevel}", toolName, def.Level);
      return def.Level;
    }

    _logger.LogWarning("[SafetyGate] Unknown tool {ToolName}, defaulting to Medium", toolName);
    return RiskLevel.Medium;
  }

  public async Task<RiskLevel> ClassifyAsync(string toolName, CancellationToken ct = default)
  {
    if (TryGetFromCache(toolName, out var cached))
      return cached.Level;

    if (_dbContext is not null)
    {
      await LoadCacheFromDbAsync(ct);
      if (TryGetFromCache(toolName, out cached))
        return cached.Level;
    }

    if (DefaultMap.TryGetValue(toolName, out var def))
    {
      _logger.LogDebug("[SafetyGate] Tool {ToolName} classified from defaults as {RiskLevel}", toolName, def.Level);
      return def.Level;
    }

    _logger.LogWarning("[SafetyGate] Unknown tool {ToolName}, defaulting to Medium", toolName);
    return RiskLevel.Medium;
  }

  public bool RequiresConfirmation(RiskLevel level) => level >= RiskLevel.Medium;

  public async Task<bool> RequiresConfirmationAsync(string toolName, CancellationToken ct = default)
  {
    if (TryGetFromCache(toolName, out var cached))
      return cached.RequiresConfirmation;

    if (_dbContext is not null)
    {
      await LoadCacheFromDbAsync(ct);
      if (TryGetFromCache(toolName, out cached))
        return cached.RequiresConfirmation;
    }

    if (DefaultMap.TryGetValue(toolName, out var def))
      return def.RequiresConfirmation;

    _logger.LogWarning("[SafetyGate] Unknown tool {ToolName}, requiring confirmation", toolName);
    return true;
  }

  public bool IsAutoApproved(RiskLevel level) => level <= RiskLevel.Low;

  public string GetConfirmationPrompt(string toolName, string description)
  {
    return $"Bạn có chắc muốn thực hiện: {description}? (Hành động: {toolName})";
  }

  public async Task InvalidateCacheAsync(CancellationToken ct = default)
  {
    lock (_lock) { _cache = null; }
    _logger.LogInformation("[SafetyGate] Cache invalidated, will reload from DB on next access");
    if (_dbContext is not null)
      await LoadCacheFromDbAsync(ct);
  }

  private bool TryGetFromCache(string toolName, out (RiskLevel Level, bool RequiresConfirmation) result)
  {
    result = default;
    Dictionary<string, (RiskLevel, bool)>? snapshot;
    lock (_lock) { snapshot = _cache; }
    return snapshot is not null && snapshot.TryGetValue(toolName, out result);
  }

  private async Task LoadCacheFromDbAsync(CancellationToken ct)
  {
    Dictionary<string, (RiskLevel, bool)>? snapshot;
    lock (_lock) { snapshot = _cache; }
    if (snapshot is not null && (DateTime.UtcNow - _cacheLoadedAt) < CacheTtl)
      return;

    if (_dbContext is null) return;

    try
    {
      var dbConfigs = await _dbContext.ToolRiskConfigs
        .AsNoTracking()
        .ToListAsync(ct);

      if (dbConfigs.Count == 0)
      {
        _logger.LogDebug("[SafetyGate] DB has no tool risk configs, using defaults");
        return;
      }

      var newCache = new Dictionary<string, (RiskLevel, bool)>(StringComparer.OrdinalIgnoreCase);
      foreach (var config in dbConfigs)
      {
        if (Enum.TryParse<RiskLevel>(config.RiskLevel, true, out var level))
          newCache[config.ToolName] = (level, config.RequiresConfirmation);
      }

      lock (_lock)
      {
        _cache = newCache;
        _cacheLoadedAt = DateTime.UtcNow;
      }

      _logger.LogInformation("[SafetyGate] Cache loaded from DB: {Count} tool configs", newCache.Count);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "[SafetyGate] Failed to load from DB, using defaults");
    }
  }
}
