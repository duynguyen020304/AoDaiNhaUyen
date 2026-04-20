using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Infrastructure.Services;
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
                           "text": "{\"intent\":\"outfit_recommendation\",\"scenario\":\"giao-vien\",\"budgetCeiling\":3000000,\"colorFamily\":\"blue\",\"materialKeyword\":\"lụa\",\"requiresPersonImage\":false}"
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
    Assert.False(result.RequiresPersonImage);
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
}
