using System.Net;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class VertexAiTryOnServiceTests
{
  [Fact]
  public async Task GenerateAsync_SendsAccessoryDisplayNamesInPrompt()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("khan-lua-do", "Khăn lụa đỏ", [7, 8, 9], "image/png")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("Khăn lụa đỏ", prompt);
    Assert.DoesNotContain("khan-lua-do", prompt);
  }

  [Fact]
  public async Task GenerateAsync_FallsBackToAccessoryIdWhenDisplayNameBlank()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("khan-lua-do", "", [7, 8, 9], "image/png")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("khan-lua-do", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PreservesImagePartOrdering()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("khan-lua-do", "Khăn lụa đỏ", [7, 8, 9], "image/png")
    ]));

    var body = await handler.Requests[0].Content!.ReadAsStringAsync();
    using var document = JsonDocument.Parse(body);
    var parts = document.RootElement
      .GetProperty("contents")[0]
      .GetProperty("parts");

    Assert.True(parts[0].TryGetProperty("text", out _));
    Assert.Equal("image/png", parts[1].GetProperty("inlineData").GetProperty("mimeType").GetString());
    Assert.Equal(Convert.ToBase64String([1, 2, 3]), parts[1].GetProperty("inlineData").GetProperty("data").GetString());
    Assert.Equal("image/jpeg", parts[2].GetProperty("inlineData").GetProperty("mimeType").GetString());
    Assert.Equal(Convert.ToBase64String([4, 5, 6]), parts[2].GetProperty("inlineData").GetProperty("data").GetString());
    Assert.Equal("image/png", parts[3].GetProperty("inlineData").GetProperty("mimeType").GetString());
    Assert.Equal(Convert.ToBase64String([7, 8, 9]), parts[3].GetProperty("inlineData").GetProperty("data").GetString());
  }

  private static AiTryOnRequestDto CreateRequest(IReadOnlyList<AiTryOnAccessoryImageDto> accessories) =>
    new(
      "ao-dai-thu-do",
      [1, 2, 3],
      "image/png",
      [4, 5, 6],
      "image/jpeg",
      accessories);

  private static VertexAiTryOnService CreateService(HttpMessageHandler handler) =>
    new(
      new HttpClient(handler),
      Options.Create(new GoogleCloudOptions
      {
        ApiKey = "api-key",
        VirtualTryOnModel = "gemini-test",
        TimeoutSeconds = 5
      }));

  private static async Task<string> ReadPromptAsync(StubHttpMessageHandler handler)
  {
    var body = await handler.Requests[0].Content!.ReadAsStringAsync();
    using var document = JsonDocument.Parse(body);
    return document.RootElement
      .GetProperty("contents")[0]
      .GetProperty("parts")[0]
      .GetProperty("text")
      .GetString()!;
  }

  private static HttpResponseMessage CreateImageResponse() =>
    new(HttpStatusCode.OK)
    {
      Content = new StringContent(
        "[{\"candidates\":[{\"content\":{\"parts\":[{\"inlineData\":{\"mimeType\":\"image/png\",\"data\":\"AQID\"}}]}}]}]",
        Encoding.UTF8,
        "application/json")
    };

  private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
  {
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var capturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);
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
