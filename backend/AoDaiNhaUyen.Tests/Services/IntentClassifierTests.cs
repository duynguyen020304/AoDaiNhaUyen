using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class IntentClassifierTests
{
  [Fact]
  public void ParseResponse_ReturnsNormalizedPlannerResult()
  {
    var body = """
               {
                 "candidates": [
                   {
                     "content": {
                       "parts": [
                         {
                           "text": "{\"intent\":\"outfit_recommendation\",\"scenario\":\"giao-vien\",\"budgetCeiling\":3000000,\"colorFamily\":\"blue\",\"materialKeyword\":\"lụa\",\"productType\":\"phu_kien\",\"requiresPersonImage\":false}"
                         }
                       ]
                     }
                   }
                 ]
               }
               """;

    var result = IntentClassifier.ParseResponse(body, new ThreadMemoryStateDto(), false);

    Assert.NotNull(result);
    Assert.Equal("outfit_recommendation", result!.Intent);
    Assert.Equal("giao-vien", result.Scenario);
    Assert.Equal("blue", result.ColorFamily);
    Assert.Equal("lụa", result.MaterialKeyword);
    Assert.Equal(3_000_000m, result.BudgetCeiling);
    Assert.Equal("phu_kien", result.ProductType);
    Assert.False(result.RequiresPersonImage);
  }

  [Fact]
  public void ParseResponse_SupportsAccessoryRecommendationIntent()
  {
    var body = """
               {
                 "candidates": [
                   {
                     "content": {
                       "parts": [
                         {
                           "text": "{\"intent\":\"accessory_recommendation\",\"productType\":\"phu_kien\"}"
                         }
                       ]
                     }
                   }
                 ]
               }
               """;

    var result = IntentClassifier.ParseResponse(body, new ThreadMemoryStateDto(), false);

    Assert.NotNull(result);
    Assert.Equal("accessory_recommendation", result!.Intent);
    Assert.Equal("phu_kien", result.ProductType);
  }

  [Fact]
  public void ParseResponse_SupportsProductDescriptionIntent()
  {
    var body = """
               {
                 "candidates": [
                   {
                     "content": {
                       "parts": [
                         {
                           "text": "{\"intent\":\"product_description\"}"
                         }
                       ]
                     }
                   }
                 ]
               }
               """;

    var result = IntentClassifier.ParseResponse(body, new ThreadMemoryStateDto(), false);

    Assert.NotNull(result);
    Assert.Equal("product_description", result!.Intent);
  }

  [Fact]
  public void ParseResponse_FallsBackToClarificationForInvalidIntentValues()
  {
    var body = """
               {
                 "candidates": [
                   {
                     "content": {
                       "parts": [
                         {
                           "text": "{\"intent\":\"made_up\",\"scenario\":\"unknown\",\"colorFamily\":\"green\"}"
                         }
                       ]
                     }
                   }
                 ]
               }
               """;

    var memory = new ThreadMemoryStateDto
    {
      Scenario = "le-tet",
      ColorFamily = "red"
    };

    var result = IntentClassifier.ParseResponse(body, memory, false);

    Assert.NotNull(result);
    Assert.Equal("clarification", result!.Intent);
    Assert.Equal("le-tet", result.Scenario);
    Assert.Equal("red", result.ColorFamily);
  }

  [Fact]
  public void ParseResponse_ReturnsNullForInvalidJson()
  {
    var body = "{";

    var result = IntentClassifier.ParseResponse(body, new ThreadMemoryStateDto(), false);

    Assert.Null(result);
  }

  [Fact]
  public async Task ClassifyAsync_UsesFallbackSetCompletionIntent_ForPairingFollowUp()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new UnusedHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions()));

    var memory = new ThreadMemoryStateDto
    {
      ShortlistedProductIds = new List<long> { 101, 102 },
      GarmentShortlistedProductIds = new List<long> { 101, 102 },
      SelectedGarmentProductId = 101
    };

    var result = await classifier.ClassifyAsync(
      "vậy bạn nghĩ nó nên đi cặp như thế nào?",
      [],
      memory,
      null,
      null,
      CancellationToken.None);

    Assert.Equal("outfit_recommendation", result.Intent);
    Assert.Equal("ao_dai", result.ProductType);
  }

  private sealed class UnusedHttpMessageHandler : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      throw new NotSupportedException("No outbound call expected when fallback classification is used.");
    }
  }
}
