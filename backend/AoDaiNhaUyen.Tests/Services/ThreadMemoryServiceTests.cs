using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Services;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class ThreadMemoryServiceTests
{
  private readonly ThreadMemoryService service = new();

  [Fact]
  public void ApplyUserTurn_OnlyTracksLatestPersonAttachment()
  {
    var memory = new ThreadMemoryStateDto
    {
      Scenario = "du-tiec",
      ColorFamily = "pink",
      MaterialKeyword = "gấm",
      BudgetCeiling = 2_500_000m
    };
    var attachments = new List<ChatAttachment>
    {
      new ChatAttachment
      {
        Id = 12,
        Kind = "user_image",
        FileUrl = "/upload/chat/1/test.png",
        MimeType = "image/png"
      }
    };

    service.ApplyUserTurn(memory, attachments);

    Assert.Equal("du-tiec", memory.Scenario);
    Assert.Equal("pink", memory.ColorFamily);
    Assert.Equal("gấm", memory.MaterialKeyword);
    Assert.Equal(2_500_000m, memory.BudgetCeiling);
    Assert.Equal(12, memory.LatestPersonAttachmentId);
  }

  [Fact]
  public void Persist_RoundTripsStructuredMemory()
  {
    var thread = new ChatThread
    {
      Id = 7
    };
    var state = new ThreadMemoryStateDto
    {
      Scenario = "le-tet",
      BudgetCeiling = 4_000_000m,
      ColorFamily = "red",
      MaterialKeyword = "gấm",
      ShortlistedProductIds = new List<long> { 11, 12 },
      GarmentShortlistedProductIds = new List<long> { 11, 12 },
      AccessoryShortlistedProductIds = new List<long> { 31, 32 },
      SelectedGarmentProductId = 11,
      SelectedAccessoryProductIds = new List<long> { 31 },
      PendingTryOnRequirements = new List<string> { "upload_person_image" }
    };

    service.Persist(thread, state, 101);
    var loaded = service.Read(thread.Memory);

    Assert.Equal("le-tet", loaded.Scenario);
    Assert.Equal(4_000_000m, loaded.BudgetCeiling);
    Assert.Equal("red", loaded.ColorFamily);
    Assert.Equal("gấm", loaded.MaterialKeyword);
    Assert.Equal(new long[] { 11, 12 }, loaded.ShortlistedProductIds);
    Assert.Equal(new long[] { 11, 12 }, loaded.GarmentShortlistedProductIds);
    Assert.Equal(new long[] { 31, 32 }, loaded.AccessoryShortlistedProductIds);
    Assert.Empty(loaded.ShownProductIds);
    Assert.Equal(11, loaded.SelectedGarmentProductId);
    Assert.Equal(new long[] { 31 }, loaded.SelectedAccessoryProductIds);
    Assert.Equal(new[] { "upload_person_image" }, loaded.PendingTryOnRequirements);
  }

  [Fact]
  public void ApplyAssistantTurn_AccumulatesShownProductIds()
  {
    var memory = new ThreadMemoryStateDto
    {
      ShownProductIds = new List<long> { 1, 2 }
    };

    service.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("outfit_recommendation", null, null, null, null, "ao_dai", [], false),
      new ChatStructuredPayloadDto(
        "recommendations",
        null,
        false,
        false,
        101,
        [201],
        [],
        [new ChatRecommendationItemDto(101, "Áo dài A", "ao-dai", "ao_dai", 1200000m, null, null, null, "Phù hợp.")],
        [new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200000m, null, null, null, "Đi kèm.")],
        [
          new ChatRecommendationItemDto(101, "Áo dài A", "ao-dai", "ao_dai", 1200000m, null, null, null, "Phù hợp."),
          new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200000m, null, null, null, "Đi kèm.")
        ]),
      null,
      null);

    Assert.Equal(new long[] { 1, 2, 101, 201 }, memory.ShownProductIds);
  }

  [Fact]
  public void ApplyAssistantTurn_PreservesGarmentShortlist_WhenPayloadOnlyContainsAccessories()
  {
    var memory = new ThreadMemoryStateDto
    {
      ShortlistedProductIds = new List<long> { 101, 102 },
      GarmentShortlistedProductIds = new List<long> { 101, 102 },
      SelectedGarmentProductId = 101
    };

    service.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("accessory_recommendation", null, null, null, null, "phu_kien", [], false),
      new ChatStructuredPayloadDto(
        "recommendations",
        null,
        false,
        false,
        101,
        [201],
        [],
        [new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm nhẹ nhàng.")],
        [],
        [new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm nhẹ nhàng.")]),
      null,
      null);

    Assert.Equal(new long[] { 101, 102 }, memory.ShortlistedProductIds);
    Assert.Equal(new long[] { 101, 102 }, memory.GarmentShortlistedProductIds);
    Assert.Equal(new long[] { 201 }, memory.AccessoryShortlistedProductIds);
  }
}
