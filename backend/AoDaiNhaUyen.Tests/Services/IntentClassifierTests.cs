using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Infrastructure.Services;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class IntentClassifierTests
{
  private readonly IntentClassifier classifier = new();

  [Fact]
  public async Task ClassifyAsync_ReturnsOutfitRecommendation_ForStylingRequest()
  {
    var memory = new ThreadMemoryStateDto();

    var result = await classifier.ClassifyAsync(
      "Gợi ý cho mình một bộ áo dài đi dạy màu xanh, dưới 3 triệu",
      [],
      memory);

    Assert.Equal("outfit_recommendation", result.Intent);
    Assert.Equal("giao-vien", result.Scenario);
    Assert.Equal("blue", result.ColorFamily);
    Assert.Equal(3_000_000m, result.BudgetCeiling);
  }

  [Fact]
  public async Task ClassifyAsync_ReturnsTryOnPrepare_WhenImageIsMissing()
  {
    var memory = new ThreadMemoryStateDto();

    var result = await classifier.ClassifyAsync(
      "Thử cái đầu tiên cho mình",
      [],
      memory);

    Assert.Equal("tryon_prepare", result.Intent);
    Assert.True(result.RequiresPersonImage);
  }

  [Fact]
  public async Task ClassifyAsync_ReturnsTryOnExecute_WhenThreadAlreadyHasPersonImage()
  {
    var memory = new ThreadMemoryStateDto
    {
      LatestPersonAttachmentId = 42
    };

    var result = await classifier.ClassifyAsync(
      "Thử bộ vừa rồi nhé",
      [],
      memory);

    Assert.Equal("tryon_execute", result.Intent);
    Assert.False(result.RequiresPersonImage);
  }
}
