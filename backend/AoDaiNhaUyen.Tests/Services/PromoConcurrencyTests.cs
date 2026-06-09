using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class PromoConcurrencyTests
{
  [Fact]
  public async Task ApplyAsync_DoesNotExceedMaxUses_WhenPostgresConnectionConfigured()
  {
    var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING")
      ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "CHANGE_ME")
    {
      return;
    }

    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(connectionString)
      .Options;
    await using (var schemaContext = new AppDbContext(options))
    {
      var hasPromoTable = await schemaContext.Database
        .SqlQueryRaw<int>("SELECT 1 AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'promo_codes' LIMIT 1")
        .AnyAsync();
      if (!hasPromoTable)
      {
        return;
      }
    }

    var promoId = Guid.NewGuid();
    var code = $"T{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    await using (var setupContext = new AppDbContext(options))
    {
      setupContext.PromoCodes.Add(new PromoCode
      {
        Id = promoId,
        Code = code,
        DiscountType = "fixed",
        DiscountValue = 10000m,
        MaxUses = 1,
        CurrentUses = 0,
        StartDate = DateTime.UtcNow.AddDays(-1),
        EndDate = DateTime.UtcNow.AddDays(1)
      });
      await setupContext.SaveChangesAsync();
    }

    var tasks = Enumerable.Range(0, 8).Select(async _ =>
    {
      await using var dbContext = new AppDbContext(options);
      var service = new PromoService(dbContext);
      var result = await service.ApplyAsync(Guid.NewGuid(), code, 100000m);
      if (result.IsValid)
      {
        await dbContext.SaveChangesAsync();
      }
      return result.IsValid;
    });

    var results = await Task.WhenAll(tasks);

    await using var verifyContext = new AppDbContext(options);
    var promo = await verifyContext.PromoCodes.AsNoTracking().SingleAsync(x => x.Id == promoId);
    Assert.Equal(1, results.Count(x => x));
    Assert.Equal(1, promo.CurrentUses);
  }
}
