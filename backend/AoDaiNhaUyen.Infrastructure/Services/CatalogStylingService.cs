using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class CatalogStylingService(AppDbContext dbContext) : ICatalogStylingService
{
  public async Task<IReadOnlyList<ChatRecommendationItemDto>> RecommendAsync(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    int limit,
    IReadOnlyList<long>? excludeProductIds = null,
    CancellationToken cancellationToken = default)
  {
    var excludedIds = excludeProductIds?.Count > 0 ? excludeProductIds.ToHashSet() : null;
    var products = await LoadProductsAsync(productType ?? "ao_dai", cancellationToken);

    return products
      .Where(product => excludedIds is null || !excludedIds.Contains(product.Id))
      .Select(product => new
      {
        Product = product,
        Score = ScoreProduct(product, scenario, budgetCeiling, colorFamily, materialKeyword),
        CoverageBoost = CalculateCoverageBoost(product)
      })
      .Where(item => item.Score > 0)
      .OrderByDescending(item => item.Score + item.CoverageBoost)
      .ThenByDescending(item => GetAvailableStock(item.Product))
      .ThenByDescending(item => item.Product.IsFeatured)
      .ThenBy(item => item.Product.Name)
      .Take(limit)
      .Select(item => MapProduct(item.Product, BuildRationale(item.Product, scenario, budgetCeiling, colorFamily, materialKeyword, item.CoverageBoost)))
      .ToList();
  }

  public async Task<IReadOnlyList<ChatRecommendationItemDto>> LookupAsync(
    string query,
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    int limit,
    CancellationToken cancellationToken = default)
  {
    var normalizedQuery = ChatTextUtils.Normalize(query);
    var products = await LoadProductsAsync(null, cancellationToken);

    return products
      .Where(product =>
      {
        var haystack = BuildLookupHaystack(product);
        return haystack.Contains(normalizedQuery) || QueryMatchesExpandedTerms(normalizedQuery, haystack);
      })
      .Select(product => new
      {
        Product = product,
        Score = ScoreProduct(product, scenario, budgetCeiling, colorFamily, materialKeyword) + 1.5m,
        CoverageBoost = CalculateCoverageBoost(product)
      })
      .OrderByDescending(item => item.Score + item.CoverageBoost)
      .ThenByDescending(item => GetAvailableStock(item.Product))
      .ThenByDescending(item => item.Product.IsFeatured)
      .ThenBy(item => item.Product.Name)
      .Take(limit)
      .Select(item => MapProduct(item.Product, item.CoverageBoost > 0 ? "Khớp trực tiếp với mô tả hiện tại trong catalog live và giúp mở rộng lựa chọn ít trùng hơn." : "Khớp trực tiếp với mô tả hiện tại trong catalog live."))
      .ToList();
  }

  public async Task<IReadOnlyList<ChatRecommendationItemDto>> CompareAsync(
    IReadOnlyList<long> productIds,
    CancellationToken cancellationToken = default)
  {
    if (productIds.Count == 0)
    {
      return [];
    }

    var products = await LoadProductsAsync(null, cancellationToken);
    var order = productIds.Select((id, index) => new { id, index }).ToDictionary(item => item.id, item => item.index);

    return products
      .Where(product => order.ContainsKey(product.Id))
      .OrderBy(product => order[product.Id])
      .Select(product => MapProduct(product, BuildComparisonRationale(product)))
      .ToList();
  }

  public async Task<IReadOnlyList<long>> ResolveProductReferencesAsync(
    string message,
    IReadOnlyList<long> shortlistedProductIds,
    CancellationToken cancellationToken = default)
  {
    var ordinalMatches = ChatTextUtils.ResolveOrdinalReferences(message, shortlistedProductIds);
    if (ordinalMatches.Count > 0)
    {
      return ordinalMatches;
    }

    var normalizedMessage = ChatTextUtils.Normalize(message);
    var products = await dbContext.Products
      .AsNoTracking()
      .Where(product => product.Status == "active")
      .Select(product => new { product.Id, product.Name, product.Slug })
      .ToListAsync(cancellationToken);

    return products
      .Where(product =>
      {
        var haystack = ChatTextUtils.Normalize($"{product.Name} {product.Slug}");
        return normalizedMessage.Contains(haystack);
      })
      .Select(product => product.Id)
      .Take(3)
      .ToList();
  }

  private static string BuildLookupHaystack(Product product)
  {
    return ChatTextUtils.Normalize(string.Join(' ', new[]
    {
      product.Name,
      product.Slug,
      product.Category.Slug,
      product.Category.Name,
      product.ShortDescription,
      product.Description,
      product.Material,
      product.ProductType,
      product.StyleProfiles.FirstOrDefault()?.Notes
    }.Where(value => !string.IsNullOrWhiteSpace(value))));
  }

  private static bool QueryMatchesExpandedTerms(string normalizedQuery, string haystack)
  {
    var expandedTerms = normalizedQuery switch
    {
      var query when query.Contains("tui xach") => new[] { normalizedQuery.Replace("tui xach", "tui sach") },
      var query when query.Contains("tui sach") => new[] { normalizedQuery.Replace("tui sach", "tui xach") },
      var query when query.Contains("theu hoa sen") => new[] { normalizedQuery.Replace("theu hoa sen", "hoa sen") },
      _ => Array.Empty<string>()
    };

    return expandedTerms.Any(haystack.Contains);
  }

  private async Task<List<Product>> LoadProductsAsync(string? productType, CancellationToken cancellationToken)
  {
    var query = dbContext.Products
      .AsNoTracking()
      .Include(product => product.Category)
      .Include(product => product.Variants)
      .Include(product => product.Images)
      .Include(product => product.StyleProfiles)
      .Include(product => product.Scenarios)
        .ThenInclude(productScenario => productScenario.Scenario)
      .Where(product => product.Status == "active");

    if (!string.IsNullOrWhiteSpace(productType))
    {
      query = query.Where(product => product.ProductType == productType);
    }

    return await query.ToListAsync(cancellationToken);
  }

  private static decimal ScoreProduct(
    Product product,
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword)
  {
    var score = 1m;
    var profile = product.StyleProfiles.FirstOrDefault();
    var variant = product.Variants.OrderByDescending(item => item.IsDefault).ThenBy(item => item.Id).FirstOrDefault();

    if (variant is not null && variant.Status != "active")
    {
      return 0m;
    }

    if (variant is not null && variant.StockQty > 0)
    {
      score += 0.2m;
    }

    if (!string.IsNullOrWhiteSpace(scenario))
    {
      score += (product.Scenarios.FirstOrDefault(item => item.Scenario.Slug == scenario)?.Score ?? 0m) * 2m;
    }

    if (!string.IsNullOrWhiteSpace(colorFamily) &&
        (string.Equals(profile?.PrimaryColorFamily, colorFamily, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(profile?.SecondaryColorFamily, colorFamily, StringComparison.OrdinalIgnoreCase)))
    {
      score += 1.2m;
    }

    if (!string.IsNullOrWhiteSpace(materialKeyword) &&
        !string.IsNullOrWhiteSpace(product.Material) &&
        ChatTextUtils.Normalize(product.Material).Contains(ChatTextUtils.Normalize(materialKeyword)))
    {
      score += 1m;
    }

    if (budgetCeiling.HasValue && variant is not null)
    {
      score += variant.Price <= budgetCeiling.Value ? 1.1m : -0.75m;
    }

    return score;
  }

  private static ChatRecommendationItemDto MapProduct(Product product, string rationale)
  {
    var variant = product.Variants.OrderByDescending(item => item.IsDefault).ThenBy(item => item.Id).FirstOrDefault();
    var image = product.Images.OrderBy(item => item.SortOrder).FirstOrDefault(item => item.IsPrimary)?.ImageUrl
      ?? product.Images.OrderBy(item => item.SortOrder).FirstOrDefault()?.ImageUrl;

    return new ChatRecommendationItemDto(
      product.Id,
      product.Name,
      product.Category.Slug,
      product.ProductType,
      variant?.Price ?? 0m,
      variant?.SalePrice,
      image,
      variant?.Id,
      rationale);
  }

  private static string BuildRationale(
    Product product,
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    decimal coverageBoost)
  {
    var reasons = new List<string>();
    var profile = product.StyleProfiles.FirstOrDefault();
    var variant = product.Variants.OrderByDescending(item => item.IsDefault).ThenBy(item => item.Id).FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(scenario))
    {
      reasons.Add($"hợp dịp {scenario.Replace('-', ' ')}");
    }

    if (!string.IsNullOrWhiteSpace(colorFamily) &&
        (string.Equals(profile?.PrimaryColorFamily, colorFamily, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(profile?.SecondaryColorFamily, colorFamily, StringComparison.OrdinalIgnoreCase)))
    {
      reasons.Add($"đúng tông {colorFamily}");
    }

    if (!string.IsNullOrWhiteSpace(materialKeyword) && !string.IsNullOrWhiteSpace(product.Material))
    {
      reasons.Add($"có chất liệu {product.Material}");
    }

    if (budgetCeiling.HasValue && variant is not null && variant.Price <= budgetCeiling.Value)
    {
      reasons.Add("nằm trong ngân sách");
    }

    if (variant is not null && variant.StockQty > 0)
    {
      reasons.Add("còn sẵn để chốt nhanh");
    }

    if (coverageBoost > 0)
    {
      reasons.Add("là lựa chọn đáng thử nếu bạn muốn đổi sang một mood ít trùng hơn");
    }

    return reasons.Count == 0
      ? "Đây là mẫu nổi bật và còn hàng trong catalog."
      : $"Điểm mạnh: {string.Join(", ", reasons)}.";
  }

  private static decimal CalculateCoverageBoost(Product product)
  {
    var profile = product.StyleProfiles.FirstOrDefault();
    var scenarioCount = product.Scenarios.Count;
    var hasSecondaryColor = !string.IsNullOrWhiteSpace(profile?.SecondaryColorFamily);
    var silhouette = profile?.Silhouette;
    var stock = GetAvailableStock(product);

    var boost = 0m;
    if (!product.IsFeatured)
    {
      boost += 0.15m;
    }

    if (scenarioCount <= 1)
    {
      boost += 0.1m;
    }

    if (hasSecondaryColor)
    {
      boost += 0.05m;
    }

    if (!string.IsNullOrWhiteSpace(silhouette))
    {
      boost += 0.05m;
    }

    if (stock > 0 && stock <= 3)
    {
      boost -= 0.05m;
    }

    return boost;
  }

  private static int GetAvailableStock(Product product)
  {
    return product.Variants
      .Where(item => item.Status == "active")
      .Sum(item => item.StockQty);
  }

  private static string BuildComparisonRationale(Product product)
  {
    var profile = product.StyleProfiles.FirstOrDefault();
    return $"độ trang trọng {profile?.Formality ?? "medium"}, tông {profile?.PrimaryColorFamily ?? "chưa gắn"}";
  }
}
