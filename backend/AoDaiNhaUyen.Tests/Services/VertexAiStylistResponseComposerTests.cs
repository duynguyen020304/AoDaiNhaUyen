using System.Net;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

using JsonDocument = System.Text.Json.JsonDocument;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class VertexAiStylistResponseComposerTests
{
  [Fact]
  public async Task ComposeStreamAsync_YieldsDeltaChunks_FromSseResponse()
  {
    var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(
        "event: message\n" +
        "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Xin \"}]}}]}\n\n" +
        "event: message\n" +
        "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"chào\"}]}}]}\n\n",
        Encoding.UTF8,
        "text/event-stream")
    });
    var composer = CreateComposer(handler);

    var chunks = await composer.ComposeStreamAsync(
      "Tư vấn cho mình",
      "fallback",
      "clarification",
      null,
      null,
      CancellationToken.None).ToListAsync();

    Assert.Equal(["Xin ", "chào"], chunks);
    Assert.Contains(":streamGenerateContent?alt=sse", handler.Requests[0].RequestUri?.ToString());
  }

  [Fact]
  public async Task ComposeStreamAsync_FallsBack_OnNonSuccessResponse()
  {
    var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadGateway));
    var composer = CreateComposer(handler);

    var chunks = await composer.ComposeStreamAsync(
      "Tư vấn cho mình",
      "fallback",
      "clarification",
      null,
      null,
      CancellationToken.None).ToListAsync();

    Assert.Equal(["fallback"], chunks);
  }

  [Fact]
  public async Task ComposeStreamAsync_FallsBack_OnMalformedPayload()
  {
    var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(
        "event: message\n" +
        "data: {not-json}\n\n",
        Encoding.UTF8,
        "text/event-stream")
    });
    var composer = CreateComposer(handler);

    var chunks = await composer.ComposeStreamAsync(
      "Tư vấn cho mình",
      "fallback",
      "clarification",
      null,
      null,
      CancellationToken.None).ToListAsync();

    Assert.Equal(["fallback"], chunks);
  }

  [Fact]
  public async Task ComposeAsync_SendsPromptThatForbidsDefaultGreetings()
  {
    var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"fallback\"}]}}]}", Encoding.UTF8, "application/json")
    });
    var composer = CreateComposer(handler);

    await composer.ComposeAsync(
      "xin chào bạn",
      "fallback",
      "clarification",
      null,
      null,
      CancellationToken.None);

    var body = await handler.Requests[0].Content!.ReadAsStringAsync();
    using var document = JsonDocument.Parse(body);
    var prompt = document.RootElement
      .GetProperty("contents")[0]
      .GetProperty("parts")[0]
      .GetProperty("text")
      .GetString();

    Assert.Contains("Giữ giọng văn tự nhiên như nhân viên tư vấn bán hàng giàu kinh nghiệm", prompt);
    Assert.Contains("Hạn chế lạm dụng các từ quá lễ nghi", prompt);
    Assert.Contains("Ưu tiên trả lời trực tiếp vào nhu cầu của khách", prompt);
  }

  [Fact]
  public async Task ComposeStreamAsync_ThrowsWhenCallerCancels()
  {
    var handler = new DelayedHttpMessageHandler();
    var composer = CreateComposer(handler);
    using var cancellationTokenSource = new CancellationTokenSource();
    await cancellationTokenSource.CancelAsync();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
    {
      await foreach (var _ in composer.ComposeStreamAsync(
        "Tư vấn cho mình",
        "fallback",
        "clarification",
        null,
        null,
        cancellationTokenSource.Token))
      {
      }
    });
  }

  private static VertexAiStylistResponseComposer CreateComposer(HttpMessageHandler handler)
  {
    return new VertexAiStylistResponseComposer(
      new HttpClient(handler),
      Options.Create(new GoogleCloudOptions
      {
        ApiKey = "api-key",
        StylistTextModel = "gemini-test",
        TimeoutSeconds = 5
      }));
  }

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

  private sealed class DelayedHttpMessageHandler : HttpMessageHandler
  {
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(string.Empty, Encoding.UTF8, "text/event-stream")
      };
    }
  }
}
