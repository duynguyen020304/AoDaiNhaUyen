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
    var attachmentId = Guid.NewGuid();
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
        Id = attachmentId,
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
    Assert.Equal(attachmentId, memory.LatestPersonAttachmentId);
  }

  [Fact]
  public void Persist_RoundTripsStructuredMemory()
  {
    var threadId = Guid.NewGuid();
    var thread = new ChatThread { Id = threadId };
    var garment1 = Guid.NewGuid();
    var garment2 = Guid.NewGuid();
    var accessory1 = Guid.NewGuid();
    var accessory2 = Guid.NewGuid();
    var msgId = Guid.NewGuid();
    var state = new ThreadMemoryStateDto
    {
      Scenario = "le-tet",
      BudgetCeiling = 4_000_000m,
      ColorFamily = "red",
      MaterialKeyword = "gấm",
      ShortlistedProductIds = new List<Guid> { garment1, garment2 },
      GarmentShortlistedProductIds = new List<Guid> { garment1, garment2 },
      AccessoryShortlistedProductIds = new List<Guid> { accessory1, accessory2 },
      SelectedGarmentProductId = garment1,
      SelectedAccessoryProductIds = new List<Guid> { accessory1 },
      PendingTryOnRequirements = new List<string> { "upload_person_image" }
    };

    service.Persist(thread, state, msgId);
    var loaded = service.Read(thread.Memory);

    Assert.Equal("le-tet", loaded.Scenario);
    Assert.Equal(4_000_000m, loaded.BudgetCeiling);
    Assert.Equal("red", loaded.ColorFamily);
    Assert.Equal("gấm", loaded.MaterialKeyword);
    Assert.Equal(new[] { garment1, garment2 }, loaded.ShortlistedProductIds);
    Assert.Equal(new[] { garment1, garment2 }, loaded.GarmentShortlistedProductIds);
    Assert.Equal(new[] { accessory1, accessory2 }, loaded.AccessoryShortlistedProductIds);
    Assert.Empty(loaded.ShownProductIds);
    Assert.Equal(garment1, loaded.SelectedGarmentProductId);
    Assert.Equal(new[] { accessory1 }, loaded.SelectedAccessoryProductIds);
    Assert.Equal(new[] { "upload_person_image" }, loaded.PendingTryOnRequirements);
  }

  [Fact]
  public void ApplyAssistantTurn_AccumulatesShownProductIds()
  {
    var shown1 = Guid.NewGuid();
    var shown2 = Guid.NewGuid();
    var garment1 = Guid.NewGuid();
    var accessory1 = Guid.NewGuid();
    var memory = new ThreadMemoryStateDto
    {
      ShownProductIds = new List<Guid> { shown1, shown2 }
    };

    service.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("outfit_recommendation", null, null, null, null, "ao_dai", [], false),
      new ChatStructuredPayloadDto(
        "recommendations",
        null,
        false,
        false,
        garment1,
        [accessory1],
        [],
        [new ChatRecommendationItemDto(garment1, "Áo dài A", "ao-dai", "ao_dai", 1200000m, null, null, null, "Phù hợp.")],
        [new ChatRecommendationItemDto(accessory1, "Khăn lụa", "phu-kien", "phu_kien", 200000m, null, null, null, "Đi kèm.")],
        [
          new ChatRecommendationItemDto(garment1, "Áo dài A", "ao-dai", "ao_dai", 1200000m, null, null, null, "Phù hợp."),
          new ChatRecommendationItemDto(accessory1, "Khăn lụa", "phu-kien", "phu_kien", 200000m, null, null, null, "Đi kèm.")
        ]),
      null,
      null);

    Assert.Equal(new[] { shown1, shown2, garment1, accessory1 }, memory.ShownProductIds);
  }

  [Fact]
  public void ApplyAssistantTurn_PreservesGarmentShortlist_WhenPayloadOnlyContainsAccessories()
  {
    var garment1 = Guid.NewGuid();
    var garment2 = Guid.NewGuid();
    var accessory1 = Guid.NewGuid();
    var memory = new ThreadMemoryStateDto
    {
      ShortlistedProductIds = new List<Guid> { garment1, garment2 },
      GarmentShortlistedProductIds = new List<Guid> { garment1, garment2 },
      SelectedGarmentProductId = garment1
    };

    service.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("accessory_recommendation", null, null, null, null, "phu_kien", [], false),
      new ChatStructuredPayloadDto(
        "recommendations",
        null,
        false,
        false,
        garment1,
        [accessory1],
        [],
        [new ChatRecommendationItemDto(accessory1, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm nhẹ nhàng.")],
        [],
        [new ChatRecommendationItemDto(accessory1, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm nhẹ nhàng.")]),
      null,
      null);

    Assert.Equal(new[] { garment1, garment2 }, memory.ShortlistedProductIds);
    Assert.Equal(new[] { garment1, garment2 }, memory.GarmentShortlistedProductIds);
    Assert.Equal(new[] { accessory1 }, memory.AccessoryShortlistedProductIds);
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
    var garment1 = Guid.NewGuid();
    var garment2 = Guid.NewGuid();
    var accessory1 = Guid.NewGuid();
    var memory = new ThreadMemoryStateDto
    {
      GarmentShortlistedProductIds = new List<Guid> { garment1, garment2 },
      AccessoryShortlistedProductIds = new List<Guid> { accessory1 }
    };

    service.ApplyUserConversationTurn(memory, "Mình thích mẫu này, lấy mẫu này nhé");

    Assert.Equal("selection_feedback", memory.LastUserRequestType);
    Assert.Equal("decide", memory.ConversationStage);
    Assert.Equal(new[] { garment1, garment2, accessory1 }, memory.LikedProductIds);
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
      service.ApplyUserConversationTurn(memory, $"Guid {i}");
    }

    Assert.Equal("1. Guid 0\n2. Guid 1\n3. Guid 2\n4. Guid 3\n5. Guid 4\n6. Guid 5\n7. Guid 6\n8. Guid 7\n9. Guid 8\n10. Guid 9\n11. Guid 10\n12. Guid 11\n13. Guid 12\n14. Guid 13\n15. Guid 14\n16. Guid 15\n17. Guid 16\n18. Guid 17\n19. Guid 18\n20. Guid 19", memory.UserConversationSummary);
    Assert.Equal(["Guid 20"], memory.RecentUserMessages);
  }

  [Fact]
  public void Persist_RoundTripsExtendedTurnState()
  {
    var threadId = Guid.NewGuid();
    var thread = new ChatThread { Id = threadId };
    var garment1 = Guid.NewGuid();
    var accessory1 = Guid.NewGuid();
    var msgId = Guid.NewGuid();
    var state = new ThreadMemoryStateDto
    {
      Scenario = "giao-vien",
      ConversationStage = "refine",
      LastUserRequestType = "alternative_request",
      ShownProductIds = new List<Guid> { garment1, accessory1 },
      ShownGarmentProductIds = new List<Guid> { garment1 },
      ShownAccessoryProductIds = new List<Guid> { accessory1 },
      LikedProductIds = new List<Guid> { garment1 }
    };

    service.Persist(thread, state, msgId);
    var loaded = service.Read(thread.Memory);

    Assert.Equal("refine", loaded.ConversationStage);
    Assert.Equal("alternative_request", loaded.LastUserRequestType);
    Assert.Equal(new[] { garment1, accessory1 }, loaded.ShownProductIds);
    Assert.Equal(new[] { garment1 }, loaded.ShownGarmentProductIds);
    Assert.Equal(new[] { accessory1 }, loaded.ShownAccessoryProductIds);
    Assert.Equal(new[] { garment1 }, loaded.LikedProductIds);
  }

  [Fact]
  public void ApplyAssistantTurn_TracksShownGarmentsAccessories_AndConversationStage()
  {
    var garment1 = Guid.NewGuid();
    var accessory1 = Guid.NewGuid();
    var memory = new ThreadMemoryStateDto();

    service.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("outfit_recommendation", "le-tet", null, null, null, "ao_dai", [], false),
      new ChatStructuredPayloadDto(
        "recommendations",
        "le-tet",
        false,
        false,
        garment1,
        [accessory1],
        [],
        [
          new ChatRecommendationItemDto(garment1, "Áo dài A", "ao-dai", "ao_dai", 1_200_000m, null, null, null, "Phù hợp."),
          new ChatRecommendationItemDto(accessory1, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm.")
        ],
        [
          new ChatRecommendationItemDto(garment1, "Áo dài A", "ao-dai", "ao_dai", 1_200_000m, null, null, null, "Phù hợp.")
        ],
        [
          new ChatRecommendationItemDto(accessory1, "Khăn lụa", "phu-kien", "phu_kien", 200_000m, null, null, null, "Đi kèm.")
        ]),
      null,
      null);

    Assert.Equal(new[] { garment1 }, memory.ShownGarmentProductIds);
    Assert.Equal(new[] { accessory1 }, memory.ShownAccessoryProductIds);
    Assert.Equal("relevance_first", memory.LastRecommendationStrategy);
    Assert.Equal("discovery", memory.ConversationStage);
  }
}
