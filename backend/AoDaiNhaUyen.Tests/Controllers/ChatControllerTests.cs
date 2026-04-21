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
      10,
      "Tư vấn áo dài",
      "active",
      "web",
      now,
      now,
      [
        new ChatMessageDto(
          99,
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
            101,
            [201],
            [],
            [new ChatRecommendationItemDto(101, "Áo dài đỏ", "ao-dai", "ao_dai", 1200000m, null, null, null, "Hợp dịp Tết.")],
            [new ChatRecommendationItemDto(101, "Áo dài đỏ", "ao-dai", "ao_dai", 1200000m, null, null, null, "Hợp dịp Tết.")],
            [new ChatRecommendationItemDto(201, "Trâm cài", "phu-kien", "phu_kien", 200000m, null, null, null, "Đi cùng áo dài tốt.")]))
      ]);

    var controller = BuildController(new StubStylistChatService { ThreadDetail = thread });

    var result = await controller.AddMessage(10, "Cho mình set đi Tết", "client-1", null, CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<ApiResponse<ChatThreadDetailDto>>(ok.Value);
    Assert.True(response.Success);
    Assert.Equal("Gửi tin nhắn thành công", response.Message);
    Assert.Equal(10, response.Data!.Id);
    Assert.Equal("recommendations", response.Data.Messages.Last().StructuredPayload!.Kind);
  }

  [Fact]
  public async Task AddMessage_WhenMessageMissing_ReturnsBadRequestFailure()
  {
    var controller = BuildController(new StubStylistChatService());

    var result = await controller.AddMessage(10, null, null, null, CancellationToken.None);

    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
    Assert.False(response.Success);
    Assert.Equal("missing_message", response.Errors!.Single().Code);
  }

  [Fact]
  public async Task ExecuteTryOn_ReturnsWrappedAssistantMessage()
  {
    var controller = BuildController(new StubStylistChatService());

    var result = await controller.ExecuteTryOn(10, new ChatController.ExecuteTryOnRequest(101, [201, 202]), CancellationToken.None);

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
      12,
      "Tư vấn follow-up",
      "active",
      "web",
      now,
      now,
      [
        new ChatMessageDto(
          100,
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
            103,
            [203, 202],
            [],
            [
              new ChatRecommendationItemDto(103, "Áo dài đỏ", "ao-dai", "ao_dai", 1300000m, null, null, null, "Mẫu mới phù hợp."),
              new ChatRecommendationItemDto(102, "Áo dài xanh", "ao-dai", "ao_dai", 1350000m, null, null, null, "Mẫu mới khác."),
              new ChatRecommendationItemDto(104, "Áo dài tím", "ao-dai", "ao_dai", 1400000m, null, null, null, "Đổi gu rõ hơn.")
            ],
            [
              new ChatRecommendationItemDto(103, "Áo dài đỏ", "ao-dai", "ao_dai", 1300000m, null, null, null, "Mẫu mới phù hợp."),
              new ChatRecommendationItemDto(102, "Áo dài xanh", "ao-dai", "ao_dai", 1350000m, null, null, null, "Mẫu mới khác."),
              new ChatRecommendationItemDto(104, "Áo dài tím", "ao-dai", "ao_dai", 1400000m, null, null, null, "Đổi gu rõ hơn.")
            ],
            [
              new ChatRecommendationItemDto(203, "Kẹp tóc", "phu-kien", "phu_kien", 120000m, null, null, null, "Đi cùng áo dài tốt."),
              new ChatRecommendationItemDto(202, "Băng đô", "phu-kien", "phu_kien", 150000m, null, null, null, "Tạo feel trẻ hơn.")
            ]))
      ]);

    var controller = BuildController(new StubStylistChatService { ThreadDetail = thread });

    var result = await controller.AddMessage(12, "Cho mình set khác", "client-alt-1", null, CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<ApiResponse<ChatThreadDetailDto>>(ok.Value);
    var payload = response.Data!.Messages.Last().StructuredPayload!;
    Assert.Equal("recommendations", payload.Kind);
    Assert.Equal([103, 102, 104], payload.GarmentProducts!.Select(item => item.ProductId).ToArray());
    Assert.Equal([203, 202], payload.AccessoryProducts!.Select(item => item.ProductId).ToArray());
    Assert.Equal(103, payload.SelectedGarmentProductId);
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

  private sealed class StubStylistChatService : IStylistChatService
  {
    public ChatThreadDetailDto ThreadDetail { get; set; } = new(
      1,
      "Stub thread",
      "active",
      "web",
      DateTime.UtcNow,
      DateTime.UtcNow,
      []);

    public Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(long? userId, string? guestKey, CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<ChatThreadSummaryDto>>([]);

    public Task<ChatThreadDetailDto> CreateThreadAsync(long? userId, string? guestKey, CancellationToken cancellationToken = default) =>
      Task.FromResult(ThreadDetail);

    public Task<ChatThreadDetailDto> GetThreadAsync(long threadId, long? userId, string? guestKey, CancellationToken cancellationToken = default) =>
      Task.FromResult(ThreadDetail);

    public Task<ChatThreadDetailDto> AddMessageAsync(long threadId, long? userId, string? guestKey, string message, string? clientMessageId, IReadOnlyList<IncomingChatAttachmentDto> attachments, CancellationToken cancellationToken = default) =>
      Task.FromResult(ThreadDetail);

    public async IAsyncEnumerable<SseChatEvent> AddMessageStreamAsync(long threadId, long? userId, string? guestKey, string message, string? clientMessageId, IReadOnlyList<IncomingChatAttachmentDto> attachments, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      yield return new SseChatEvent.Done();
      await Task.CompletedTask;
    }

    public Task<ChatMessageDto> ExecuteTryOnAsync(long threadId, long? userId, string? guestKey, long? garmentProductId, IReadOnlyList<long> accessoryProductIds, CancellationToken cancellationToken = default) =>
      Task.FromResult(new ChatMessageDto(1, "assistant", "done", "tryon_execute", DateTime.UtcNow, [], null));
  }
}
