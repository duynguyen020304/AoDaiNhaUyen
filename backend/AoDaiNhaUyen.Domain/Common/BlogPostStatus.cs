using System.Text.Json.Serialization;

namespace AoDaiNhaUyen.Domain.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlogPostStatus
{
  Draft,
  Published,
  Archived
}
