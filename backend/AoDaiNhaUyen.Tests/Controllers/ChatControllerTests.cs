using System.Security.Claims;
using AoDaiNhaUyen.Api.Controllers;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AoDaiNhaUyen.Tests.Controllers;

public sealed class ChatControllerTests
{
  [Fact]
  public async Task AddMessage_ReturnsWrappedThreadDetail()
  {
    var now = DateTime.UtcNow;
    var thread = new ChatThreadDetailDto(
      T1,
      "Tư vấn áo dài",
      "active",
      "web",
      now,
      now,
      [
        new ChatMessageDto(
          Guid.NewGuid(),
          "assistant",
          "Mình gợi ý 2 set mới cho bạn.",
          "outfit_recommendation",
          now,
          [],
          new ChatStructuredPayloadDto(
            "recommendations",
            "le-tet",
            false,
            false,
            G101,
            [A201],
            [],
            [new ChatRecommendationItemDto(G101, "Áo dài đỏ", "ao-dai", "ao_dai", 1200000m, null, null, null, "Hợp dịp Tết.")],
            [new ChatRecommendationItemDto(G101, "Áo dài đỏ", "ao-dai", "ao_dai", 1200000m, null, null, null, "Hợp dịp Tết.")],
            [new ChatRecommendationItemDto(A201, "Trâm cài", "phu-kien", "phu_kien", 200000m, null, null, null, "Đi cùng áo dài tốt.")]))
      ]);

    var controller = BuildController(new StubStylistChatService { ThreadDetail = thread });

    var result = await controller.AddMessage(T1, "Cho mình set đi Tết", "client-1", null, CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<ApiResponse<ChatThreadDetailDto>>(ok.Value);
    Assert.True(response.Success);
    Assert.Equal("Gửi tin nhắn thành công", response.Message);
    Assert.Equal(T1, response.Data!.Id);
    Assert.Equal("recommendations", response.Data.Messages.Last().StructuredPayload!.Kind);
  }

  [Fact]
  public async Task AddMessage_WhenMessageMissing_ReturnsBadRequestFailure()
  {
    var controller = BuildController(new StubStylistChatService());

    var result = await controller.AddMessage(T1, null, null, null, CancellationToken.None);

    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
    Assert.False(response.Success);
    Assert.Equal("missing_message", response.Errors!.Single().Code);
  }

  [Fact]
  public async Task ExecuteTryOn_ReturnsWrappedAssistantMessage()
  {
    var controller = BuildController(new StubStylistChatService());

    var result = await controller.ExecuteTryOn(T1, new ChatController.ExecuteTryOnRequest(G101, [A201, A202]), CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<ApiResponse<ChatMessageDto>>(ok.Value);
    Assert.True(response.Success);
    Assert.Equal("Tạo ảnh thử đồ trong chat thành công", response.Message);
    Assert.Equal("tryon_execute", response.Data!.Intent);
  }

  [Fact]
  public async Task AddMessage_PreservesRecommendationPayloadForAlternativeFlow()
  {
    var now = DateTime.UtcNow;
    var thread = new ChatThreadDetailDto(
      T2,
      "Tư vấn follow-up",
      "active",
      "web",
      now,
      now,
      [
        new ChatMessageDto(
          Guid.NewGuid(),
          "assistant",
          "Look 1 — Thanh lịch nổi bật\n- Khác biệt: ưu tiên mẫu chưa trùng với các gợi ý trước",
          "outfit_recommendation",
          now,
          [],
          new ChatStructuredPayloadDto(
            "recommendations",
            "giao-vien",
            false,
            false,
            G103,
            [A203, A202],
            [],
            [
              new ChatRecommendationItemDto(G103, "Áo dài đỏ", "ao-dai", "ao_dai", 1300000m, null, null, null, "Mẫu mới phù hợp."),
              new ChatRecommendationItemDto(G102, "Áo dài xanh", "ao-dai", "ao_dai", 1350000m, null, null, null, "Mẫu mới khác."),
              new ChatRecommendationItemDto(G104, "Áo dài tím", "ao-dai", "ao_dai", 1400000m, null, null, null, "Đổi gu rõ hơn.")
            ],
            [
              new ChatRecommendationItemDto(G103, "Áo dài đỏ", "ao-dai", "ao_dai", 1300000m, null, null, null, "Mẫu mới phù hợp."),
              new ChatRecommendationItemDto(G102, "Áo dài xanh", "ao-dai", "ao_dai", 1350000m, null, null, null, "Mẫu mới khác."),
              new ChatRecommendationItemDto(G104, "Áo dài tím", "ao-dai", "ao_dai", 1400000m, null, null, null, "Đổi gu rõ hơn.")
            ],
            [
              new ChatRecommendationItemDto(A203, "Kẹp tóc", "phu-kien", "phu_kien", 120000m, null, null, null, "Đi cùng áo dài tốt."),
              new ChatRecommendationItemDto(A202, "Băng đô", "phu-kien", "phu_kien", 150000m, null, null, null, "Tạo feel trẻ hơn.")
            ]))
      ]);

    var controller = BuildController(new StubStylistChatService { ThreadDetail = thread });

    var result = await controller.AddMessage(T2, "Cho mình set khác", "client-alt-1", null, CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<ApiResponse<ChatThreadDetailDto>>(ok.Value);
    var payload = response.Data!.Messages.Last().StructuredPayload!;
    Assert.Equal("recommendations", payload.Kind);
    Assert.Equal([G103, G102, G104], payload.GarmentProducts!.Select(item => item.ProductId).ToArray());
    Assert.Equal([A203, A202], payload.AccessoryProducts!.Select(item => item.ProductId).ToArray());
    Assert.Equal(G103, payload.SelectedGarmentProductId);
  }

  private static ChatController BuildController(IStylistChatService stylistChatService)
  {
    var controller = new ChatController(stylistChatService)
    {
      ControllerContext = new ControllerContext
      {
        HttpContext = new DefaultHttpContext()
      }
    };

    controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
      new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "TestAuth"));

    return controller;
  }

  private static readonly Guid T1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid T2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
  private static readonly Guid G101 = Guid.Parse("b1011011-0110-1101-1011-011011011011");
  private static readonly Guid G102 = Guid.Parse("b1021021-0210-2102-1021-021021021021");
  private static readonly Guid G103 = Guid.Parse("b1031031-0310-3103-1031-031031031031");
  private static readonly Guid G104 = Guid.Parse("b1041041-0410-4104-1041-041041041041");
  private static readonly Guid A201 = Guid.Parse("c2012012-0120-1201-2012-012012012012");
  private static readonly Guid A202 = Guid.Parse("c2022022-0220-2202-2022-022022022022");
  private static readonly Guid A203 = Guid.Parse("c2032032-0320-3203-2032-032032032032");

  private sealed class StubStylistChatService : IStylistChatService
  {
    public ChatThreadDetailDto ThreadDetail { get; set; } = new(
      Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
      "Stub thread",
      "active",
      "web",
      DateTime.UtcNow,
      DateTime.UtcNow,
      []);

    public Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(Guid? userId, string? guestKey, CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatThreadSummaryDto>>([]);

    public Task<ChatThreadDetailDto> CreateThreadAsync(Guid? userId, string? guestKey, CancellationToken cancellationToken = default) =>
      Task.FromResult(ThreadDetail);

    public Task<ChatThreadDetailDto> GetThreadAsync(Guid threadId, Guid? userId, string? guestKey, CancellationToken cancellationToken = default) =>
      Task.FromResult(ThreadDetail);

    public Task<ChatThreadDetailDto> AddMessageAsync(Guid threadId, Guid? userId, string? guestKey, string message, string? clientMessageId, IReadOnlyList<IncomingChatAttachmentDto> attachments, CancellationToken cancellationToken = default) =>
      Task.FromResult(ThreadDetail);

    public async IAsyncEnumerable<SseChatEvent> AddMessageStreamAsync(Guid threadId, Guid? userId, string? guestKey, string message, string? clientMessageId, IReadOnlyList<IncomingChatAttachmentDto> attachments, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      yield return new SseChatEvent.Done();
      await Task.CompletedTask;
    }

    public Task<ChatMessageDto> ExecuteTryOnAsync(Guid threadId, Guid? userId, string? guestKey, Guid? garmentProductId, IReadOnlyList<Guid> accessoryProductIds, CancellationToken cancellationToken = default) =>
      Task.FromResult(new ChatMessageDto(Guid.NewGuid(), "assistant", "done", "tryon_execute", DateTime.UtcNow, [], null));
  }
}
