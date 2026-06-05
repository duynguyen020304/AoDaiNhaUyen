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
    var categoryId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var category = new Category
    {
      Id = categoryId,
      Name = "Áo dài",
      Slug = "ao-dai"
    };

    var scenarioId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    var scenario = new StyleScenario
    {
      Id = scenarioId,
      Name = "Giáo viên",
      Slug = "giao-vien"
    };

    dbContext.Categories.Add(category);
    dbContext.StyleScenarios.Add(scenario);

    var featuredProductId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    var featuredProduct = BuildProduct(
      featuredProductId,
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

    var coverageProductId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    var coverageProduct = BuildProduct(
      coverageProductId,
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

    Assert.Equal([coverageProductId, featuredProductId], results.Select(item => item.ProductId).ToArray());
    Assert.Contains("trùng", results[0].Rationale);
  }

  private static Product BuildProduct(
    Guid productId,
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
          Id = Guid.NewGuid(),
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
