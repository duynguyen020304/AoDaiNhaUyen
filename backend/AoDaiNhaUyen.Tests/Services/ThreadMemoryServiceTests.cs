using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Services;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class ThreadMemoryServiceTests
{
  private readonly ThreadMemoryService service = new();

  [Fact]
  public void ApplyUserTurn_PromotesAllowedFactsOnly()
  {
    var memory = new ThreadMemoryStateDto();
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

    service.ApplyUserTurn(
      memory,
      "Mình cần áo dài đi dạy màu xanh, chất liệu lụa, ngân sách 2.5 triệu",
      attachments);

    Assert.Equal("giao-vien", memory.Scenario);
    Assert.Equal("blue", memory.ColorFamily);
    Assert.Equal("lụa", memory.MaterialKeyword);
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
    Assert.Equal(11, loaded.SelectedGarmentProductId);
    Assert.Equal(new long[] { 31 }, loaded.SelectedAccessoryProductIds);
    Assert.Equal(new[] { "upload_person_image" }, loaded.PendingTryOnRequirements);
  }
}
