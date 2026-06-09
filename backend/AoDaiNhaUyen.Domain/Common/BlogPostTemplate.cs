using System.Text.Json.Serialization;

namespace AoDaiNhaUyen.Domain.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlogPostTemplate
{
  StandardArticle,
  PhotoGallery,
  VideoFeature,
  ProductSpotlight,
  HowTo
}
