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
    string? productType,
    int limit,
    CancellationToken cancellationToken = default)
  {
    var normalizedQuery = ChatTextUtils.Normalize(query);
    var queryTokens = ExtractQueryTokens(normalizedQuery);
    var products = await LoadProductsAsync(productType, cancellationToken);

    var strictMatches = GetStrictMatches(products, normalizedQuery);
    var tokenMatches = strictMatches.Count > 0
      ? strictMatches
      : products.Where(product => CountTokenMatches(product, queryTokens) > 0).ToList();

    var primaryCandidates = tokenMatches.Count > 0 ? tokenMatches : products;
    var primaryResults = RankLookupCandidates(primaryCandidates, normalizedQuery, queryTokens, scenario, budgetCeiling, colorFamily, materialKeyword, limit, strictMatches.Count > 0);
    if (primaryResults.Count > 0)
    {
      return primaryResults;
    }

    if (!string.IsNullOrWhiteSpace(materialKeyword))
    {
      var withoutMaterial = RankLookupCandidates(products, normalizedQuery, queryTokens, scenario, budgetCeiling, colorFamily, null, limit, false);
      if (withoutMaterial.Count > 0)
      {
        return withoutMaterial;
      }
    }

    if (!string.IsNullOrWhiteSpace(scenario))
    {
      var withoutScenario = RankLookupCandidates(products, normalizedQuery, queryTokens, null, budgetCeiling, colorFamily, null, limit, false);
      if (withoutScenario.Count > 0)
      {
        return withoutScenario;
      }
    }

    if (budgetCeiling.HasValue)
    {
      var withoutBudget = RankLookupCandidates(products, normalizedQuery, queryTokens, null, null, colorFamily, null, limit, false);
      if (withoutBudget.Count > 0)
      {
        return withoutBudget;
      }
    }

    return RankLookupCandidates(products, normalizedQuery, queryTokens, null, null, null, null, limit, false);
  }

  private static List<Product> GetStrictMatches(IReadOnlyList<Product> products, string normalizedQuery)
  {
    return products
      .Where(product =>
      {
        var haystack = BuildLookupHaystack(product);
        return haystack.Contains(normalizedQuery) || QueryMatchesExpandedTerms(normalizedQuery, haystack);
      })
      .ToList();
  }

  private List<ChatRecommendationItemDto> RankLookupCandidates(
    IReadOnlyList<Product> candidates,
    string normalizedQuery,
    IReadOnlyList<string> queryTokens,
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    int limit,
    bool hasStrictMatches)
  {
    return candidates
      .Select(product => new
      {
        Product = product,
        TokenMatches = CountTokenMatches(product, queryTokens),
        Score = ScoreProduct(product, scenario, budgetCeiling, colorFamily, materialKeyword) + GetLookupBoost(product, normalizedQuery, queryTokens),
        CoverageBoost = CalculateCoverageBoost(product)
      })
      .Where(item => item.Score > 0)
      .OrderByDescending(item => item.Score + item.CoverageBoost + (item.TokenMatches * 0.15m))
      .ThenByDescending(item => item.TokenMatches)
      .ThenByDescending(item => GetAvailableStock(item.Product))
      .ThenByDescending(item => item.Product.IsFeatured)
      .ThenBy(item => item.Product.Name)
      .Take(limit)
      .Select(item => MapProduct(item.Product, BuildLookupRationale(item.Product, normalizedQuery, item.TokenMatches, item.CoverageBoost, hasStrictMatches)))
      .ToList();
  }

  private static decimal GetLookupBoost(Product product, string normalizedQuery, IReadOnlyList<string> queryTokens)
  {
    var haystack = BuildLookupHaystack(product);
    if (!string.IsNullOrWhiteSpace(normalizedQuery) && (haystack.Contains(normalizedQuery) || QueryMatchesExpandedTerms(normalizedQuery, haystack)))
    {
      return 1.5m;
    }

    var tokenMatches = CountTokenMatches(product, queryTokens);
    return tokenMatches > 0 ? Math.Min(1.2m, tokenMatches * 0.35m) : 0.25m;
  }

  private static string BuildLookupRationale(Product product, string normalizedQuery, int tokenMatches, decimal coverageBoost, bool hasStrictMatches)
  {
    if (hasStrictMatches)
    {
      return coverageBoost > 0
        ? PickOne(
          "Mẫu đang có trong catalog và mang lại lựa chọn khác biệt hơn.",
          "Mẫu này khá sát nhu cầu hiện tại, lại cho cảm giác mới hơn một chút.",
          "Mẫu này hợp nhu cầu hiện tại và vẫn đủ khác để bạn dễ cân nhắc thêm.")
        : PickOne(
          "Mẫu đang có trong catalog và khá sát nhu cầu của bạn.",
          "Mẫu này đang khá đúng hướng bạn tìm.",
          "Mẫu này bám khá sát nhu cầu hiện tại của bạn.");
    }

    if (tokenMatches > 0)
    {
      return coverageBoost > 0
        ? PickOne(
          "Mẫu khá sát nhu cầu hiện tại và mang lại lựa chọn khác biệt hơn.",
          "Mẫu này hợp hướng bạn đang tìm và vẫn tạo cảm giác mới mẻ hơn.",
          "Mẫu này khá hợp nhu cầu hiện tại, lại không bị quá `giống các lựa chọn trước.")
        : PickOne(
          "Mẫu khá sát nhu cầu hiện tại của bạn.",
          "Mẫu này đang khá hợp với thứ bạn tìm.",
          "Mẫu này là một lựa chọn khá sát nhu cầu hiện tại.");
    }

    return PickOne(
      "Mẫu gần với nhu cầu hiện tại, dễ phối và vẫn đáng cân nhắc.",
      "Mẫu này chưa phải khớp nhất nhưng vẫn là lựa chọn đáng để thử.",
      "Mẫu này khá dễ phối và vẫn nằm trong nhóm nên cân nhắc.");
  }

  private static IReadOnlyList<string> ExtractQueryTokens(string normalizedQuery)
  {
    return normalizedQuery
      .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Where(token => token.Length >= 3)
      .Distinct(StringComparer.Ordinal)
      .ToList();
  }

  private static int CountTokenMatches(Product product, IReadOnlyList<string> queryTokens)
  {
    if (queryTokens.Count == 0)
    {
      return 0;
    }

    var haystack = BuildLookupHaystack(product);
    return queryTokens.Count(haystack.Contains);
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
      var currentQuery when currentQuery.Contains("tui xach") => new[] { normalizedQuery.Replace("tui xach", "tui sach") },
      var currentQuery when currentQuery.Contains("tui sach") => new[] { normalizedQuery.Replace("tui sach", "tui xach") },
      var currentQuery when currentQuery.Contains("theu hoa sen") => new[] { normalizedQuery.Replace("theu hoa sen", "hoa sen") },
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
    var activeVariants = product.Variants
      .Where(item => item.Status == "active")
      .OrderByDescending(item => item.IsDefault)
      .ThenBy(item => item.Id)
      .ToList();
    var variant = activeVariants.FirstOrDefault() ?? product.Variants.OrderByDescending(item => item.IsDefault).ThenBy(item => item.Id).FirstOrDefault();

    if (product.Variants.Count > 0 && activeVariants.Count == 0)
    {
      return 0m;
    }

    if (GetAvailableStock(product) > 0)
    {
      score += 0.2m;
    }

    if (!string.IsNullOrWhiteSpace(scenario))
    {
      score += (product.Scenarios.FirstOrDefault(item => item.Scenario.Slug == scenario)?.Score ?? 0.2m) * 1.2m;
    }

    if (!string.IsNullOrWhiteSpace(colorFamily) &&
        (string.Equals(profile?.PrimaryColorFamily, colorFamily, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(profile?.SecondaryColorFamily, colorFamily, StringComparison.OrdinalIgnoreCase)))
    {
      score += 0.8m;
    }

    if (!string.IsNullOrWhiteSpace(materialKeyword) &&
        !string.IsNullOrWhiteSpace(product.Material) &&
        ChatTextUtils.Normalize(product.Material).Contains(ChatTextUtils.Normalize(materialKeyword)))
    {
      score += 0.7m;
    }

    if (budgetCeiling.HasValue && variant is not null)
    {
      score += variant.Price <= budgetCeiling.Value ? 0.8m : -0.2m;
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
    var formality = profile?.Formality ?? "medium";
    var color = profile?.PrimaryColorFamily ?? "chưa gắn";
    return PickOne(
      $"Thiên về độ trang trọng {formality}, tông {color}.",
      $"Phong cách khá {formality}, tông màu chính là {color}.",
      $"Mẫu này có độ trang trọng {formality} và nghiêng về tông {color}.");
  }

  private static string PickOne(params string[] options)
  {
    return options[Random.Shared.Next(options.Length)];
  }
}
