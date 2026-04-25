using System.Net;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class VertexAiImageValidationServiceTests
{
  [Fact]
  public async Task ValidatePersonImageAsync_SendsImageInlineDataAndPrompt()
  {
    var handler = new StubHttpMessageHandler(CreateTextResponse("{\"isValid\":true,\"category\":\"valid_person\",\"reason\":\"Ảnh phù hợp để thử đồ.\",\"confidence\":0.95}"));
    var service = CreateService(handler);

    await service.ValidatePersonImageAsync([1, 2, 3], "image/png");

    var request = handler.Requests[0];
    Assert.Contains("gemini-validation-test", request.RequestUri!.ToString());
    Assert.True(request.Headers.TryGetValues("x-goog-api-key", out var values));
    Assert.Equal("api-key", Assert.Single(values));

    var body = await request.Content!.ReadAsStringAsync();
    using var document = JsonDocument.Parse(body);
    var parts = document.RootElement.GetProperty("contents")[0].GetProperty("parts");

    Assert.Contains("thử đồ ảo", parts[0].GetProperty("text").GetString());
    Assert.Equal("image/png", parts[1].GetProperty("inlineData").GetProperty("mimeType").GetString());
    Assert.Equal(Convert.ToBase64String([1, 2, 3]), parts[1].GetProperty("inlineData").GetProperty("data").GetString());
    Assert.Equal("application/json", document.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString());
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ParsesValidJsonResult()
  {
    var handler = new StubHttpMessageHandler(CreateTextResponse("{\"isValid\":true,\"category\":\"valid_person\",\"reason\":\"Ảnh phù hợp để thử đồ.\",\"confidence\":0.95}"));
    var service = CreateService(handler);

    var result = await service.ValidatePersonImageAsync([1, 2, 3], "image/png");

    Assert.True(result.IsValid);
    Assert.Equal("valid_person", result.Category);
    Assert.Equal("Ảnh phù hợp để thử đồ.", result.Reason);
    Assert.Equal(0.95m, result.Confidence);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ParsesSingleObjectResponse()
  {
    var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(
        "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"isValid\\\":true,\\\"category\\\":\\\"valid_person\\\",\\\"reason\\\":\\\"Ảnh phù hợp để thử đồ.\\\",\\\"confidence\\\":0.95}\"}]}}]}",
        Encoding.UTF8,
        "application/json")
    });
    var service = CreateService(handler);

    var result = await service.ValidatePersonImageAsync([1, 2, 3], "image/png");

    Assert.True(result.IsValid);
    Assert.Equal("valid_person", result.Category);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ParsesInvalidJsonResult()
  {
    var handler = new StubHttpMessageHandler(CreateTextResponse("{\"isValid\":false,\"category\":\"object_only\",\"reason\":\"Ảnh không có người mặc phù hợp.\",\"confidence\":0.9}"));
    var service = CreateService(handler);

    var result = await service.ValidatePersonImageAsync([1, 2, 3], "image/png");

    Assert.False(result.IsValid);
    Assert.Equal("object_only", result.Category);
    Assert.Equal("Ảnh không có người mặc phù hợp.", result.Reason);
    Assert.Equal(0.9m, result.Confidence);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ParsesJsonSplitAcrossStreamChunks()
  {
    var responseBody = string.Join(
      "\n",
      "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"isValid\\\":true,\"}]}}]}",
      "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"\\\"category\\\":\\\"valid_person\\\",\"}]}}]}",
      "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"\\\"reason\\\":\\\"Ảnh phù hợp để thử đồ.\\\",\\\"confidence\\\":0.95}\"}]}}]}");
    var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
    });
    var service = CreateService(handler);

    var result = await service.ValidatePersonImageAsync([1, 2, 3], "image/png");

    Assert.True(result.IsValid);
    Assert.Equal("valid_person", result.Category);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_StripsMarkdownCodeFence()
  {
    var handler = new StubHttpMessageHandler(CreateTextResponse("```json\n{\"isValid\":false,\"category\":\"animal\",\"reason\":\"Ảnh là động vật.\",\"confidence\":0.88}\n```"));
    var service = CreateService(handler);

    var result = await service.ValidatePersonImageAsync([1, 2, 3], "image/png");

    Assert.False(result.IsValid);
    Assert.Equal("animal", result.Category);
    Assert.Equal("Ảnh là động vật.", result.Reason);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ThrowsWhenApiKeyMissing()
  {
    var service = new VertexAiImageValidationService(
      new HttpClient(new StubHttpMessageHandler(CreateTextResponse("{}"))),
      Options.Create(new GoogleCloudOptions
      {
        ApiKey = "",
        ImageValidationModel = "gemini-validation-test",
        ImageValidationTimeoutSeconds = 5
      }));

    await Assert.ThrowsAsync<ImageValidationConfigurationException>(() =>
      service.ValidatePersonImageAsync([1, 2, 3], "image/png"));
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ThrowsWhenModelMissing()
  {
    var service = new VertexAiImageValidationService(
      new HttpClient(new StubHttpMessageHandler(CreateTextResponse("{}"))),
      Options.Create(new GoogleCloudOptions
      {
        ApiKey = "api-key",
        ImageValidationModel = "",
        ImageValidationTimeoutSeconds = 5
      }));

    await Assert.ThrowsAsync<ImageValidationConfigurationException>(() =>
      service.ValidatePersonImageAsync([1, 2, 3], "image/png"));
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ThrowsOnProviderError()
  {
    var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
    {
      Content = new StringContent("{\"error\":{\"message\":\"provider exploded\"}}", Encoding.UTF8, "application/json")
    });
    var service = CreateService(handler);

    var exception = await Assert.ThrowsAsync<ImageValidationProviderException>(() =>
      service.ValidatePersonImageAsync([1, 2, 3], "image/png"));

    Assert.Contains("provider exploded", exception.Message);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ThrowsOnMalformedResponse()
  {
    var handler = new StubHttpMessageHandler(CreateTextResponse("not-json"));
    var service = CreateService(handler);

    await Assert.ThrowsAsync<ImageValidationProviderException>(() =>
      service.ValidatePersonImageAsync([1, 2, 3], "image/png"));
  }

  private static VertexAiImageValidationService CreateService(HttpMessageHandler handler) =>
    new(
      new HttpClient(handler),
      Options.Create(new GoogleCloudOptions
      {
        ApiKey = "api-key",
        ImageValidationModel = "gemini-validation-test",
        ImageValidationTimeoutSeconds = 5
      }));

  private static HttpResponseMessage CreateTextResponse(string text) =>
    new(HttpStatusCode.OK)
    {
      Content = new StringContent(
        "[{\"candidates\":[{\"content\":{\"parts\":[{\"text\":" + JsonSerializer.Serialize(text) + "}]}}]}]",
        Encoding.UTF8,
        "application/json")
    };

  private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
  {
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var capturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);
      foreach (var header in request.Headers)
      {
        capturedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }

      if (request.Content is not null)
      {
        var body = await request.Content.ReadAsStringAsync(cancellationToken);
        capturedRequest.Content = new StringContent(body, Encoding.UTF8, request.Content.Headers.ContentType?.MediaType ?? "application/json");
      }

      Requests.Add(capturedRequest);
      return response;
    }
  }
}
