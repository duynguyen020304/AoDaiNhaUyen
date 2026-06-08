namespace AoDaiNhaUyen.Application.Interfaces;

public interface ICacheKeyService
{
  string BuildKey(string entity, string operation, string? identifier = null, string? variant = null, string? layer = null);
  string BuildAdminKey(string entity, string operation, string? identifier = null, string? variant = null);
  string BuildPattern(string layer, string entity);
  string BuildPattern(string layer, string entity, string operation);
}
