using AoDaiNhaUyen.Infrastructure.Services;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class StylistFallbackTextServiceTests
{
  [Theory]
  [InlineData("thread_welcome")]
  [InlineData("clarification")]
  [InlineData("out_of_scope")]
  [InlineData("catalog_lookup_empty")]
  [InlineData("recommendation_empty")]
  [InlineData("comparison_need_more_refs")]
  [InlineData("comparison_insufficient_data")]
  [InlineData("tryon_need_garment")]
  [InlineData("tryon_need_person_image")]
  [InlineData("tryon_ready")]
  [InlineData("image_analysis_need_scenario")]
  [InlineData("tryon_result")]
  public void Pick_ReturnsNonEmptyMessage_ForKnownTheme(string theme)
  {
    var service = new StylistFallbackTextService();

    var result = service.Pick(theme);

    Assert.False(string.IsNullOrWhiteSpace(result));
  }

  [Fact]
  public void Pick_WithPlaceholders_ReplacesTemplateTokens()
  {
    var service = new StylistFallbackTextService();

    var result = service.Pick("catalog_lookup_intro", ("count", "4"));

    Assert.Contains("4", result);
    Assert.DoesNotContain("{count}", result);
  }

  [Fact]
  public void Pick_Throws_ForUnknownTheme()
  {
    var service = new StylistFallbackTextService();

    Assert.Throws<InvalidOperationException>(() => service.Pick("unknown_theme"));
  }
}
