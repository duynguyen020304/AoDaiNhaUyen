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
    ["get_revenue_by_range"] = (RiskLevel.Read, false),
    ["get_orders_by_status_by_range"] = (RiskLevel.Read, false),
    ["get_top_products_by_range"] = (RiskLevel.Read, false),
    ["get_range_metrics"] = (RiskLevel.Read, false),
    ["list_orders_by_range"] = (RiskLevel.Read, false),
    ["count_by_created_range"] = (RiskLevel.Read, false),
    ["list_recent_activity"] = (RiskLevel.Read, false),
    ["list_hermes_reports"] = (RiskLevel.Read, false),
    ["get_hermes_report"] = (RiskLevel.Read, false),
    ["list_hermes_events"] = (RiskLevel.Read, false),
    ["get_user_growth"] = (RiskLevel.Read, false),
    ["list_products"] = (RiskLevel.Read, false),
    ["get_product"] = (RiskLevel.Read, false),
    ["create_product"] = (RiskLevel.Low, false),
    ["update_product"] = (RiskLevel.Medium, true),
    ["delete_product"] = (RiskLevel.High, true),
    ["restore_product"] = (RiskLevel.Medium, true),
    ["toggle_product_status"] = (RiskLevel.Medium, true),
    ["list_variants"] = (RiskLevel.Read, false),
    ["create_variant"] = (RiskLevel.Low, false),
    ["update_variant"] = (RiskLevel.Medium, true),
    ["update_variant_stock"] = (RiskLevel.Medium, true),
    ["delete_variant"] = (RiskLevel.High, true),
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
    ["update_user_profile"] = (RiskLevel.Medium, true),
    ["create_role"] = (RiskLevel.High, true),
    ["list_roles"] = (RiskLevel.Read, false),
    ["update_role"] = (RiskLevel.High, true),
    ["delete_role"] = (RiskLevel.High, true),
    ["create_user"] = (RiskLevel.Low, false),
    ["delete_user"] = (RiskLevel.High, true),
    ["restore_user"] = (RiskLevel.Medium, true),
    ["list_orders"] = (RiskLevel.Read, false),
    ["get_order"] = (RiskLevel.Read, false),
    ["confirm_order"] = (RiskLevel.High, true),
    ["start_processing_order"] = (RiskLevel.High, true),
    ["ship_order"] = (RiskLevel.High, true),
    ["complete_order"] = (RiskLevel.High, true),
    ["cancel_order"] = (RiskLevel.High, true),
    ["update_order_address"] = (RiskLevel.High, true),
    ["update_order_items"] = (RiskLevel.High, true),
    ["delete_order"] = (RiskLevel.High, true),
    ["restore_order"] = (RiskLevel.High, true),
    ["get_inventory_summary"] = (RiskLevel.Read, false),
    ["get_store_health_score"] = (RiskLevel.Read, false),
    ["list_media"] = (RiskLevel.Read, false),
    ["get_media"] = (RiskLevel.Read, false),
    ["upload_media"] = (RiskLevel.Low, false),
    ["delete_media"] = (RiskLevel.High, true),
    ["list_recent_reviews"] = (RiskLevel.Read, false),
    ["list_recent_comments"] = (RiskLevel.Read, false),
    ["reply_to_review"] = (RiskLevel.Medium, true),
    ["reply_to_comment"] = (RiskLevel.Medium, true),
    ["list_reviews"] = (RiskLevel.Read, false),
    ["hide_review"] = (RiskLevel.Medium, true),
    ["show_review"] = (RiskLevel.Medium, true),
    ["delete_review"] = (RiskLevel.High, true),
    ["list_promo_codes"] = (RiskLevel.Read, false),
    ["create_promo_code"] = (RiskLevel.High, true),
    ["get_promo_code"] = (RiskLevel.Read, false),
    ["update_promo_code"] = (RiskLevel.High, true),
    ["toggle_promo_code"] = (RiskLevel.Medium, true),
    ["delete_promo_code"] = (RiskLevel.High, true),
    ["list_marketing_options"] = (RiskLevel.Read, false),
    ["send_marketing_campaign"] = (RiskLevel.High, true),
    ["list_subscribers"] = (RiskLevel.Read, false),
    ["get_subscriber"] = (RiskLevel.Read, false),
    ["unsubscribe_subscriber"] = (RiskLevel.Medium, true),
    ["delete_subscriber"] = (RiskLevel.High, true),
    ["list_email_jobs"] = (RiskLevel.Read, false),
    ["get_email_job"] = (RiskLevel.Read, false),
    ["retry_email_job"] = (RiskLevel.Low, false),
    ["cancel_email_job"] = (RiskLevel.Medium, true),
    ["delete_email_job"] = (RiskLevel.High, true),
    ["generate_blog_draft"] = (RiskLevel.Read, false),
    ["save_blog_draft"] = (RiskLevel.Low, false),
    ["publish_blog_post"] = (RiskLevel.High, true),
    ["list_blog_posts"] = (RiskLevel.Read, false),
    ["get_blog_post"] = (RiskLevel.Read, false),
    ["update_blog_post"] = (RiskLevel.Medium, true),
    ["delete_blog_post"] = (RiskLevel.High, true),
    ["create_purchase_note"] = (RiskLevel.Low, false),
    ["generate_daily_report"] = (RiskLevel.Read, false),
    ["toggle_autonomy"] = (RiskLevel.High, true),
    ["get_autonomy_status"] = (RiskLevel.Read, false),
    ["generate_product_description"] = (RiskLevel.Read, false),
    ["generate_weekly_report"] = (RiskLevel.Read, false),
    ["check_inventory_alerts"] = (RiskLevel.Read, false),
    // Facebook Page (social)
    ["list_facebook_pages"] = (RiskLevel.Read, false),
    ["list_facebook_posts"] = (RiskLevel.Read, false),
    ["list_facebook_post_comments"] = (RiskLevel.Read, false),
    ["reply_facebook_comment"] = (RiskLevel.Medium, true),
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
