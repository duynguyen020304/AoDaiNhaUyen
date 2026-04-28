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
      new AiTryOnAccessoryImageDto("khan-lua-do", "Khăn lụa đỏ", [7, 8, 9], "image/png", "unknown")
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
      new AiTryOnAccessoryImageDto("khan-lua-do", "", [7, 8, 9], "image/png", "unknown")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("khan-lua-do", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PromptContainsConditionalBackgroundInstruction()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("background của ảnh 1", prompt);
    Assert.Contains("nếu background đó phù hợp", prompt);
    Assert.Contains("dùng background của ảnh 2", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PromptContainsMaleHandlingInstruction()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("bất kỳ giới tính nào", prompt);
    Assert.Contains("nam", prompt);
    Assert.Contains("tự nhiên, thanh lịch", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PromptDoesNotHardcodeGarmentBackground()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([]));

    var prompt = await ReadPromptAsync(handler);
    Assert.DoesNotContain("Final image phải dùng background", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PreservesImagePartOrdering()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("khan-lua-do", "Khăn lụa đỏ", [7, 8, 9], "image/png", "unknown")
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

  [Fact]
  public async Task GenerateAsync_PromptContainsHairAccessoryInstruction()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("tram-cai-hoa-don", "Trâm cài hoa đơn", [7, 8, 9], "image/png", "tram-cai")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("phụ kiện cài tóc", prompt);
    Assert.Contains("điều chỉnh kiểu tóc", prompt);
    Assert.Contains("Trâm cài hoa đơn", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PromptContainsFanHandheldInstruction()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("quat-bau", "Quạt bầu", [7, 8, 9], "image/png", "quat")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("phụ kiện cầm tay", prompt);
    Assert.Contains("điều chỉnh dáng tay", prompt);
    Assert.Contains("Quạt bầu", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PromptContainsBagHandheldInstruction()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("tui-theu-hoa", "Túi thêu hoa", [7, 8, 9], "image/png", "tui-sach")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("phụ kiện cầm tay", prompt);
    Assert.Contains("điều chỉnh dáng tay", prompt);
    Assert.Contains("Túi thêu hoa", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PromptContainsMultipleAccessoryCategoryInstructions()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("tram-cai-hoa-don", "Trâm cài hoa đơn", [7, 8, 9], "image/png", "tram-cai"),
      new AiTryOnAccessoryImageDto("quat-bau", "Quạt bầu", [10, 11, 12], "image/png", "quat")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("điều chỉnh kiểu tóc", prompt);
    Assert.Contains("điều chỉnh dáng tay", prompt);
  }

  [Fact]
  public async Task GenerateAsync_PromptUsesGenericInstructionForUnknownAccessoryCategory()
  {
    var handler = new StubHttpMessageHandler(CreateImageResponse());
    var service = CreateService(handler);

    await service.GenerateAsync(CreateRequest([
      new AiTryOnAccessoryImageDto("khan-lua-do", "Khăn lụa đỏ", [7, 8, 9], "image/png", "unknown")
    ]));

    var prompt = await ReadPromptAsync(handler);
    Assert.Contains("phụ kiện khác", prompt);
    Assert.DoesNotContain("điều chỉnh kiểu tóc", prompt);
    Assert.DoesNotContain("điều chỉnh dáng tay", prompt);
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
