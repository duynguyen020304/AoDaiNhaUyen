using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class CustomerEventServiceTests
{
  [Fact]
  public async Task TrackAsync_StoresAllowedEventWithHashedClientData()
  {
    await using var dbContext = CreateDbContext();
    var service = new CustomerEventService(dbContext);

    var result = await service.TrackAsync(
      null,
      new TrackCustomerEventRequest
      {
        EventType = "viewed_product",
        AnonymousSessionId = "session-1",
        Source = "google",
        Medium = "cpc",
        Campaign = "tet",
        MetadataJson = "{\"slug\":\"ao-dai\"}"
      },
      "127.0.0.1",
      "agent");

    var saved = await dbContext.CustomerEvents.SingleAsync();
    Assert.Equal(result.Id, saved.Id);
    Assert.Equal("viewed_product", saved.EventType);
    Assert.Equal("session-1", saved.AnonymousSessionId);
    Assert.Equal("google", saved.Source);
    Assert.NotNull(saved.IpHash);
    Assert.NotEqual("127.0.0.1", saved.IpHash);
  }

  [Fact]
  public async Task TrackAsync_RejectsUnknownEvent()
  {
    await using var dbContext = CreateDbContext();
    var service = new CustomerEventService(dbContext);

    await Assert.ThrowsAsync<ArgumentException>(() => service.TrackAsync(
      null,
      new TrackCustomerEventRequest { EventType = "bad_event" },
      null,
      null));
  }

  [Fact]
  public async Task TrackAsync_ClampsClientOccurredAt()
  {
    await using var dbContext = CreateDbContext();
    var service = new CustomerEventService(dbContext);
    var tooOld = DateTime.UtcNow.AddDays(-90);

    var result = await service.TrackAsync(
      null,
      new TrackCustomerEventRequest { EventType = "viewed_product", OccurredAt = tooOld },
      null,
      null);

    Assert.True(result.OccurredAt > DateTime.UtcNow.AddDays(-31));
  }

  private static AppDbContext CreateDbContext()
  {
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
  }
}
