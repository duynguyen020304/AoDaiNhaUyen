using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class CatalogStylingServiceTests
{
  [Fact]
  public async Task RecommendAsync_PrefersInStockBroaderCoverageProduct_WhenScoresTie()
  {
    await using var dbContext = CreateDbContext();
    var category = new Category
    {
      Id = 1,
      Name = "Áo dài",
      Slug = "ao-dai"
    };

    var scenario = new StyleScenario
    {
      Id = 1,
      Name = "Giáo viên",
      Slug = "giao-vien"
    };

    dbContext.Categories.Add(category);
    dbContext.StyleScenarios.Add(scenario);

    var featuredProduct = BuildProduct(
      101,
      "Áo dài featured",
      category,
      isFeatured: true,
      stockQty: 2,
      scenario,
      profile: new ProductStyleProfile
      {
        PrimaryColorFamily = "blue",
        Formality = "medium"
      });

    var coverageProduct = BuildProduct(
      102,
      "Áo dài coverage",
      category,
      isFeatured: false,
      stockQty: 8,
      scenario,
      profile: new ProductStyleProfile
      {
        PrimaryColorFamily = "blue",
        SecondaryColorFamily = "ivory",
        Formality = "medium",
        Silhouette = "classic"
      });

    dbContext.Products.AddRange(featuredProduct, coverageProduct);
    await dbContext.SaveChangesAsync();

    var service = new CatalogStylingService(dbContext);

    var results = await service.RecommendAsync("giao-vien", null, "blue", null, "ao_dai", 2, cancellationToken: CancellationToken.None);

    Assert.Equal([102, 101], results.Select(item => item.ProductId).ToArray());
    Assert.Contains("mood ít trùng hơn", results[0].Rationale);
  }

  private static Product BuildProduct(
    long productId,
    string name,
    Category category,
    bool isFeatured,
    int stockQty,
    StyleScenario scenario,
    ProductStyleProfile profile)
  {
    return new Product
    {
      Id = productId,
      CategoryId = category.Id,
      Category = category,
      Name = name,
      Slug = $"product-{productId}",
      ProductType = "ao_dai",
      Status = "active",
      IsFeatured = isFeatured,
      Variants =
      [
        new ProductVariant
        {
          Id = productId * 10,
          ProductId = productId,
          Sku = $"SKU-{productId}",
          Price = 1_500_000m,
          StockQty = stockQty,
          IsDefault = true,
          Status = "active"
        }
      ],
      StyleProfiles =
      [
        profile
      ],
      Scenarios =
      [
        new ProductScenario
        {
          ProductId = productId,
          ScenarioId = scenario.Id,
          Scenario = scenario,
          Score = 1m
        }
      ]
    };
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"catalog-styling-{Guid.NewGuid():N}")
      .Options;
    return new AppDbContext(options);
  }
}
