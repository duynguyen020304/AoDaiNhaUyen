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

  [Fact]
  public void ApplyUserConversationTurn_MarksAlternativeRequestAsRefineStage()
  {
    var memory = new ThreadMemoryStateDto
    {
      RecentUserMessages = new List<string>()
    };

    service.ApplyUserConversationTurn(memory, "Cho mình bộ khác đi");

    Assert.Equal("alternative_request", memory.LastUserRequestType);
    Assert.Equal("refine", memory.ConversationStage);
    Assert.Contains("Cho mình bộ khác đi", memory.RecentUserMessages);
  }

  [Fact]
  public void ApplyUserConversationTurn_MarksLikedProductsAsDecisionStage()
  {
    var memory = new ThreadMemoryStateDto
    {
      GarmentShortlistedProductIds = new List<long> { 101, 102 },
      AccessoryShortlistedProductIds = new List<long> { 201 }
    };

    service.ApplyUserConversationTurn(memory, "Mình thích mẫu này, lấy mẫu này nhé");

    Assert.Equal("selection_feedback", memory.LastUserRequestType);
    Assert.Equal("decide", memory.ConversationStage);
    Assert.Equal(new long[] { 101, 102, 201 }, memory.LikedProductIds);
  }

  [Fact]
  public void ApplyUserConversationTurn_CapsSummaryAndKeepsNewestContent()
  {
    var memory = new ThreadMemoryStateDto();

    for (var i = 0; i < 40; i++)
    {
      service.ApplyUserConversationTurn(memory, $"tin nhắn người dùng {i} {new string('x', 120)}");
    }

    Assert.True(memory.UserConversationSummary!.Length <= 3000);
    Assert.Contains("tin nhắn người dùng 39", string.Join("\n", memory.RecentUserMessages));
    Assert.Contains("tin nhắn người dùng 19", memory.UserConversationSummary);
    Assert.DoesNotContain("tin nhắn người dùng 20", memory.UserConversationSummary);
    Assert.Equal(20, memory.RecentUserMessages.Count);
    Assert.Equal("tin nhắn người dùng 20 " + new string('x', 120), memory.RecentUserMessages[0]);
  }

  [Fact]
  public void ApplyAssistantTurn_CapsSummaryAndKeepsNewestContent()
  {
    var memory = new ThreadMemoryStateDto();
    var classification = new IntentClassificationDto("clarification", null, null, null, null, null, [], false);

    for (var i = 0; i < 40; i++)
    {
      service.ApplyAssistantTurn(memory, classification, null, null, null, $"tin nhắn assistant {i} {new string('y', 120)}");
    }

    Assert.True(memory.AssistantConversationSummary!.Length <= 3000);
    Assert.Contains("tin nhắn assistant 39", string.Join("\n", memory.RecentAssistantMessages));
    Assert.Contains("tin nhắn assistant 19", memory.AssistantConversationSummary);
    Assert.DoesNotContain("tin nhắn assistant 20", memory.AssistantConversationSummary);
    Assert.Equal(20, memory.RecentAssistantMessages.Count);
    Assert.Equal("tin nhắn assistant 20 " + new string('y', 120), memory.RecentAssistantMessages[0]);
  }

  [Fact]
  public void ApplyUserConversationTurn_LeavesShortSummaryUnchanged()
  {
    var memory = new ThreadMemoryStateDto();

    for (var i = 0; i < 21; i++)
    {
      service.ApplyUserConversationTurn(memory, $"short {i}");
    }

    Assert.Equal("1. short 0\n2. short 1\n3. short 2\n4. short 3\n5. short 4\n6. short 5\n7. short 6\n8. short 7\n9. short 8\n10. short 9\n11. short 10\n12. short 11\n13. short 12\n14. short 13\n15. short 14\n16. short 15\n17. short 16\n18. short 17\n19. short 18\n20. short 19", memory.UserConversationSummary);
    Assert.Equal(["short 20"], memory.RecentUserMessages);
  }

  [Fact]
  public void Persist_RoundTripsExtendedTurnState()
  {
    var thread = new ChatThread { Id = 9 };
    var state = new ThreadMemoryStateDto
    {
      Scenario = "giao-vien",
      ConversationStage = "refine",
      LastUserRequestType = "alternative_request",
      ShownProductIds = new List<long> { 101, 201 },
      ShownGarmentProductIds = new List<long> { 101 },
      ShownAccessoryProductIds = new List<long> { 201 },
      LikedProductIds = new List<long> { 101 }
    };

    service.Persist(thread, state, 33);
    var loaded = service.Read(thread.Memory);

    Assert.Equal("refine", loaded.ConversationStage);
    Assert.Equal("alternative_request", loaded.LastUserRequestType);
    Assert.Equal(new long[] { 101, 201 }, loaded.ShownProductIds);
    Assert.Equal(new long[] { 101 }, loaded.ShownGarmentProductIds);
    Assert.Equal(new long[] { 201 }, loaded.ShownAccessoryProductIds);
    Assert.Equal(new long[] { 101 }, loaded.LikedProductIds);
  }

  [Fact]
  public void ApplyAssistantTurn_TracksShownGarmentsAccessories_AndConversationStage()
  {
    var memory = new ThreadMemoryStateDto();

    service.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("outfit_recommendation", "le-tet", null, null, null, "ao_dai", [], false),
      new ChatStructuredPayloadDto(
        "recommendations",
        "le-tet",
        false,
        false,
        101,
        [201],
        [],
        [
          new ChatRecommendationItemDto(101, "Áo dài A", "ao-dai", "ao_dai", 1_200_000m, null, null, null, "Phù hợp."),
          new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm.")
        ],
        [
          new ChatRecommendationItemDto(101, "Áo dài A", "ao-dai", "ao_dai", 1_200_000m, null, null, null, "Phù hợp.")
        ],
        [
          new ChatRecommendationItemDto(201, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm.")
        ]),
      null,
      null);

    Assert.Equal(new long[] { 101 }, memory.ShownGarmentProductIds);
    Assert.Equal(new long[] { 201 }, memory.ShownAccessoryProductIds);
    Assert.Equal("relevance_first", memory.LastRecommendationStrategy);
    Assert.Equal("discovery", memory.ConversationStage);
  }
}
