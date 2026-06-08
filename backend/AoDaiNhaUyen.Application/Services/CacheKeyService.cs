using AoDaiNhaUyen.Application.Interfaces;

namespace AoDaiNhaUyen.Application.Services;

public sealed class CacheKeyService : ICacheKeyService
{
  private const string KeySeparator = ":";
  public const string LayerAdmin = "admin";
  public const string LayerService = "service";

  public string BuildKey(
    string entity,
    string operation,
    string? identifier = null,
    string? variant = null,
    string? layer = null)
  {
    layer ??= LayerService;
    var parts = new List<string> { layer, entity, operation };
    if (!string.IsNullOrWhiteSpace(identifier)) parts.Add(identifier);
    if (!string.IsNullOrWhiteSpace(variant)) parts.Add(variant);
    return string.Join(KeySeparator, parts);
  }

  public string BuildAdminKey(string entity, string operation, string? identifier = null, string? variant = null)
    => BuildKey(entity, operation, identifier, variant, LayerAdmin);

  public string BuildPattern(string layer, string entity)
    => $"{layer}{KeySeparator}{entity}{KeySeparator}*";

  public string BuildPattern(string layer, string entity, string operation)
    => $"{layer}{KeySeparator}{entity}{KeySeparator}{operation}{KeySeparator}*";
}
