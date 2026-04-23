using System.Net;
using System.Text;
using System.Text.Json;
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
    Assert.Equal("green", result.ColorFamily);
  }

  [Fact]
  public void ParseResponse_ReturnsNullForInvalidJson()
  {
    var body = "{";

    var result = IntentClassifier.ParseResponse(body, new ThreadMemoryStateDto(), false);

    Assert.Null(result);
  }

  [Fact]
  public void ParseResponse_PersistsReferencedImageHint()
  {
    var body = """
               {
                 "candidates": [
                   {
                     "content": {
                       "parts": [
                         {
                           "text": "{\"intent\":\"image_style_analysis\",\"referencedImageHint\":\"first\",\"productReferenceScope\":\"shortlist_top_3\",\"wantsDifferentOptions\":true,\"hasSpecificAccessoryRequest\":true}"
                         }
                       ]
                     }
                   }
                 ]
               }
               """;

    var result = IntentClassifier.ParseResponse(body, new ThreadMemoryStateDto(), false);

    Assert.NotNull(result);
    Assert.Equal("image_style_analysis", result!.Intent);
    Assert.Equal("first", result.ReferencedImageHint);
    Assert.Equal("shortlist_top_3", result.ProductReferenceScope);
    Assert.True(result.WantsDifferentOptions);
    Assert.True(result.HasSpecificAccessoryRequest);
  }

  [Fact]
  public async Task ClassifyAsync_DegradedFallbackDoesNotInferTryOnFromKeywords()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new UnusedHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions()));

    var memory = new ThreadMemoryStateDto
    {
      ImageCatalog = [new ImageCatalogEntry(11, "user_image", "Ảnh 1", null)]
    };

    var result = await classifier.ClassifyAsync(
      "thu do anh nay",
      [],
      memory,
      null,
      null,
      CancellationToken.None);

    Assert.Equal("clarification", result.Intent);
    Assert.Null(result.ReferencedImageHint);
  }

  [Fact]
  public async Task ClassifyAsync_UsesModelResponseForTryOnSemanticFollowUp()
  {
    var handler = new CaptureHttpMessageHandler(_ => CreateGeminiResponse("{\"intent\":\"tryon_execute\",\"referencedImageHint\":\"last\"}"));
    var classifier = new IntentClassifier(
      new HttpClient(handler),
      Options.Create(new GoogleCloudOptions { ApiKey = "key", StylistTextModel = "gemini-test" }));

    var memory = new ThreadMemoryStateDto
    {
      ImageCatalog = [new ImageCatalogEntry(11, "user_image", "Ảnh 1", null)]
    };

    var result = await classifier.ClassifyAsync(
      "thu do anh nay",
      [],
      memory,
      null,
      null,
      CancellationToken.None);

    Assert.Equal("tryon_execute", result.Intent);
    Assert.Equal("last", result.ReferencedImageHint);
  }

  [Fact]
  public async Task ClassifyAsync_UsesModelResponseForImageReferenceFollowUp()
  {
    var handler = new CaptureHttpMessageHandler(_ => CreateGeminiResponse("{\"intent\":\"image_style_analysis\",\"referencedImageHint\":\"first\"}"));
    var classifier = new IntentClassifier(
      new HttpClient(handler),
      Options.Create(new GoogleCloudOptions { ApiKey = "key", StylistTextModel = "gemini-test" }));

    var memory = new ThreadMemoryStateDto
    {
      ImageCatalog = [new ImageCatalogEntry(11, "user_image", "Ảnh 1", null)]
    };

    var result = await classifier.ClassifyAsync(
      "anh dau tien co dep khong",
      [],
      memory,
      null,
      null,
      CancellationToken.None);

    Assert.Equal("image_style_analysis", result.Intent);
    Assert.Equal("first", result.ReferencedImageHint);
  }

  [Fact]
  public async Task ClassifyAsync_DegradedFallbackDoesNotInferImageAnalysisFromKeywords()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new UnusedHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions()));

    var memory = new ThreadMemoryStateDto
    {
      ImageCatalog = [new ImageCatalogEntry(11, "user_image", "Ảnh 1", null)]
    };

    var result = await classifier.ClassifyAsync(
      "anh dau tien co dep khong",
      [],
      memory,
      null,
      null,
      CancellationToken.None);

    Assert.Equal("clarification", result.Intent);
    Assert.Null(result.ReferencedImageHint);
  }

  [Fact]
  public async Task ClassifyAsync_DoesNotInferImageAnalysisWithoutImages()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new UnusedHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions()));

    var result = await classifier.ClassifyAsync(
      "xem anh nay",
      [],
      new ThreadMemoryStateDto(),
      null,
      null,
      CancellationToken.None);

    Assert.Equal("clarification", result.Intent);
  }

  [Fact]
  public async Task ClassifyAsync_UsesModelResponseForSemanticFollowUp()
  {
    var handler = new CaptureHttpMessageHandler(_ => CreateGeminiResponse("{\"intent\":\"outfit_recommendation\",\"selectionStrategy\":\"stylist_pick\"}"));
    var classifier = new IntentClassifier(
      new HttpClient(handler),
      Options.Create(new GoogleCloudOptions { ApiKey = "key", StylistTextModel = "gemini-test" }));

    var memory = new ThreadMemoryStateDto
    {
      ShortlistedProductIds = new List<long> { 101, 102 },
      SelectedGarmentProductId = 101
    };

    var result = await classifier.ClassifyAsync(
      "món này hợp đi sự kiện nào hơn? phối thêm gì ổn?",
      [],
      memory,
      "cho tôi xem vài mẫu",
      "đây là shortlist hiện tại",
      CancellationToken.None);

    Assert.Equal("outfit_recommendation", result.Intent);
    Assert.Equal("stylist_pick", result.SelectionStrategy);
    Assert.True(result.NeedsCatalogLookup);
    Assert.Equal("món này hợp đi sự kiện nào hơn? phối thêm gì ổn?", result.RetrievalQuery);
  }

  [Fact]
  public async Task ClassifyAsync_UsesThinFallbackForGeneralFashionRequestWithoutModel()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new UnusedHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions()));

    var result = await classifier.ClassifyAsync(
      "tôi muốn xem thêm vài mẫu hợp đi tiệc màu đỏ",
      [],
      new ThreadMemoryStateDto(),
      null,
      null,
      CancellationToken.None);

    Assert.Equal("catalog_lookup", result.Intent);
    Assert.Equal("du-tiec", result.Scenario);
    Assert.Equal("red", result.ColorFamily);
  }

  [Fact]
  public async Task ClassifyAsync_UsesFallbackForDirectListRequest()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new UnusedHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions()));

    var result = await classifier.ClassifyAsync(
      "hãy liệt kê đi",
      [],
      new ThreadMemoryStateDto(),
      null,
      null,
      CancellationToken.None);

    Assert.Equal("catalog_lookup", result.Intent);
    Assert.Equal("ao_dai", result.ProductType);
    Assert.Equal("hãy liệt kê đi", result.RetrievalQuery);
  }

  [Fact]
  public async Task ClassifyAsync_PreservesExpandedColorFamilies()
  {
    var body = """
               {
                 "candidates": [
                   {
                     "content": {
                       "parts": [
                         {
                           "text": "{\"intent\":\"catalog_lookup\",\"colorFamily\":\"purple\"}"
                         }
                       ]
                     }
                   }
                 ]
               }
               """;

    var result = IntentClassifier.ParseResponse(body, new ThreadMemoryStateDto(), false);

    Assert.NotNull(result);
    Assert.Equal("catalog_lookup", result!.Intent);
    Assert.Equal("purple", result.ColorFamily);
  }

  [Fact]
  public async Task ClassifyAsync_UsesFallbackForOutOfScopeRequest()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new UnusedHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions()));

    var result = await classifier.ClassifyAsync(
      "shop có hỗ trợ đổi trả và giao hàng không",
      [],
      new ThreadMemoryStateDto(),
      null,
      null,
      CancellationToken.None);

    Assert.Equal("out_of_scope", result.Intent);
  }

  [Fact]
  public async Task ClassifyAsync_UsesFallbackWhenModelCallFails()
  {
    var classifier = new IntentClassifier(
      new HttpClient(new FailingHttpMessageHandler()),
      Options.Create(new GoogleCloudOptions { ApiKey = "key", StylistTextModel = "gemini-test" }));

    var result = await classifier.ClassifyAsync(
      "tôi muốn xem thêm vài mẫu hợp đi tiệc màu đỏ",
      [],
      new ThreadMemoryStateDto(),
      null,
      null,
      CancellationToken.None);

    Assert.Equal("catalog_lookup", result.Intent);
    Assert.Equal("du-tiec", result.Scenario);
    Assert.Equal("red", result.ColorFamily);
  }

  [Fact]
  public async Task ClassifyAsync_PromptUsesSemanticGuidanceInsteadOfPhraseRules()
  {
    var handler = new CaptureHttpMessageHandler(_ => CreateGeminiResponse("{\"intent\":\"clarification\"}"));
    var classifier = new IntentClassifier(
      new HttpClient(handler),
      Options.Create(new GoogleCloudOptions { ApiKey = "key", StylistTextModel = "gemini-test" }));

    await classifier.ClassifyAsync(
      "cho tôi gợi ý",
      [],
      new ThreadMemoryStateDto(),
      "tin nhắn trước",
      "phản hồi trước",
      CancellationToken.None);

    Assert.NotNull(handler.LastRequestContent);
    using var document = JsonDocument.Parse(handler.LastRequestContent!);
    var prompt = document.RootElement
      .GetProperty("contents")[0]
      .GetProperty("parts")[0]
      .GetProperty("text")
      .GetString();

    Assert.NotNull(prompt);
    Assert.Contains("Ưu tiên hiểu mục tiêu thật sự của user theo ngữ nghĩa", prompt);
    Assert.Contains("Định nghĩa intent:", prompt);
    Assert.DoesNotContain("Nếu user nói 'mẫu này'", prompt);
    Assert.DoesNotContain("Nếu user hỏi 'đi cặp như thế nào'", prompt);
  }

  private static HttpResponseMessage CreateGeminiResponse(string plannerJson) =>
    new(HttpStatusCode.OK)
    {
      Content = new StringContent(JsonSerializer.Serialize(new
      {
        candidates = new[]
        {
          new
          {
            content = new
            {
              parts = new[]
              {
                new { text = plannerJson }
              }
            }
          }
        }
      }), Encoding.UTF8, "application/json")
    };

  private sealed class UnusedHttpMessageHandler : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      throw new NotSupportedException("No outbound call expected when fallback classification is used.");
    }
  }

  private sealed class FailingHttpMessageHandler : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      throw new HttpRequestException("boom");
    }
  }

  private sealed class CaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
  {
    public string? LastRequestContent { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      LastRequestContent = request.Content is null
        ? null
        : await request.Content.ReadAsStringAsync(cancellationToken);
      return responder(request);
    }
  }
}
