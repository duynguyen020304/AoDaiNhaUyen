using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Domain.SeedData;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AoDaiNhaUyen.Infrastructure.Data;

public sealed class SeedDataService(
  AppDbContext dbContext,
  IPasswordHasher passwordHasher,
  IUploadStoragePathResolver uploadStoragePathResolver) : ISeedDataService
{
  private const string CuratedTryOnRoot = "upload/tryon-curated";

  public async Task SeedAllAsync()
  {
    await dbContext.Database.MigrateAsync();

    await SeedRolesAsync();
    await SeedCustomersAsync();
    await SeedCategoriesAsync();
    await SeedProductsAsync();
    await SeedStyleScenariosAsync();
    await SeedProductStyleDataAsync();
    await SeedProductAiAssetsAsync();
    await RemoveStaleCategoriesAsync();
  }

  private async Task SeedRolesAsync()
  {
    foreach (var roleName in DefaultRoles.Items)
    {
      var exists = await dbContext.Roles.AnyAsync(r => r.Name == roleName);
      if (!exists)
      {
        dbContext.Roles.Add(new Role { Name = roleName });
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedCategoriesAsync()
  {
    var parents = DefaultCategories.Items.Where(x => x.ParentSlug is null).ToList();
    foreach (var item in parents)
    {
      var existing = await dbContext.Categories.FirstOrDefaultAsync(x => x.Slug == item.Slug);
      if (existing is null)
      {
        dbContext.Categories.Add(new Category
        {
          Name = item.Name,
          Slug = item.Slug,
          Description = null,
          Parent = null,
          SortOrder = item.SortOrder,
          IsActive = true
        });
        continue;
      }

      existing.Name = item.Name;
      existing.Parent = null;
      existing.SortOrder = item.SortOrder;
      existing.IsActive = true;
    }

    await dbContext.SaveChangesAsync();

    var children = DefaultCategories.Items.Where(x => x.ParentSlug is not null).ToList();
    foreach (var item in children)
    {
      var parent = await dbContext.Categories.FirstAsync(c => c.Slug == item.ParentSlug);
      var existing = await dbContext.Categories.FirstOrDefaultAsync(x => x.Slug == item.Slug);

      if (existing is null)
      {
        dbContext.Categories.Add(new Category
        {
          Name = item.Name,
          Slug = item.Slug,
          Description = null,
          Parent = parent.Id,
          SortOrder = item.SortOrder,
          IsActive = true
        });
        continue;
      }

      existing.Name = item.Name;
      existing.Parent = parent.Id;
      existing.SortOrder = item.SortOrder;
      existing.IsActive = true;
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedCustomersAsync()
  {
    var customerRole = await dbContext.Roles.FirstAsync(x => x.Name == "customer");

    foreach (var item in DefaultCustomers.Items)
    {
      var user = await dbContext.Users
        .Include(x => x.UserRoles)
        .FirstOrDefaultAsync(x => x.Email == item.Email);

      if (user is null)
      {
        user = new User
        {
          FullName = item.FullName,
          Email = item.Email,
          Phone = item.Phone,
          Gender = item.Gender,
          Status = "active",
          EmailVerifiedAt = DateTime.UtcNow,
          PhoneVerifiedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
      }
      else
      {
        user.FullName = item.FullName;
        user.Phone = item.Phone;
        user.Gender = item.Gender;
        user.Status = "active";
        user.EmailVerifiedAt ??= DateTime.UtcNow;
        user.PhoneVerifiedAt ??= DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
      }

      if (!user.UserRoles.Any(x => x.RoleId == customerRole.Id))
      {
        user.UserRoles.Add(new UserRole { User = user, RoleId = customerRole.Id });
      }

      var normalizedEmail = item.Email.Trim().ToLowerInvariant();
      var credentialsAccount = await dbContext.UserAccounts.FirstOrDefaultAsync(
        x => x.Provider == "credentials" && x.ProviderAccountId == normalizedEmail,
        CancellationToken.None);

      if (credentialsAccount is null)
      {
        dbContext.UserAccounts.Add(new UserAccount
        {
          User = user,
          Provider = "credentials",
          ProviderAccountId = normalizedEmail,
          PasswordHash = passwordHasher.HashPassword(item.Password),
          IsVerified = true
        });
      }
      else
      {
        credentialsAccount.PasswordHash = passwordHasher.HashPassword(item.Password);
        credentialsAccount.IsVerified = true;
        credentialsAccount.UpdatedAt = DateTime.UtcNow;
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedProductsAsync()
  {
    await RemoveStaleProductsAsync();

    var materialsBySlug = DefaultMaterials.Items.ToDictionary(x => x.Slug, x => x.Name);

    foreach (var item in DefaultProducts.Items)
    {
      var category = await dbContext.Categories.FirstAsync(x => x.Slug == item.CategorySlug);
      var material = item.MaterialSlug is not null && materialsBySlug.TryGetValue(item.MaterialSlug, out var materialName)
        ? materialName
        : null;

      var productType = await ResolveProductTypeAsync(category);
      var product = await dbContext.Products
        .Include(x => x.Variants)
        .Include(x => x.Images)
        .FirstOrDefaultAsync(x => x.Slug == item.Slug);

      if (product is null)
      {
        product = new Product
        {
          CategoryId = category.Id,
          Name = item.Name,
          Slug = item.Slug,
          ProductType = productType,
          ShortDescription = item.ShortDescription,
          Description = item.LongDescription,
          Material = material,
          Brand = "Nha Uyen",
          Origin = "Viet Nam",
          Status = "active",
          IsFeatured = item.IsFeatured
        };

        dbContext.Products.Add(product);
      }
      else
      {
        product.CategoryId = category.Id;
        product.Name = item.Name;
        product.ProductType = productType;
        product.ShortDescription = item.ShortDescription;
        product.Description = item.LongDescription;
        product.Material = material;
        product.Brand = "Nha Uyen";
        product.Origin = "Viet Nam";
        product.Status = "active";
        product.IsFeatured = item.IsFeatured;
        product.UpdatedAt = DateTime.UtcNow;
      }

      foreach (var variantSeed in item.Variants)
      {
        UpsertVariant(product, variantSeed);
      }

      var defaultVariant = product.Variants.FirstOrDefault(x => x.IsDefault) ?? product.Variants.First();
      foreach (var image in item.Images)
      {
        var existingImage = product.Images.FirstOrDefault(x => x.ImageUrl == image.ImageUrl);
        if (existingImage is null)
        {
          product.Images.Add(new ProductImage
          {
            ImageUrl = image.ImageUrl,
            AltText = image.AltText,
            Variant = image.IsPrimary ? defaultVariant : null,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary
          });
          continue;
        }

        existingImage.AltText = image.AltText;
        existingImage.Variant = image.IsPrimary ? defaultVariant : null;
        existingImage.SortOrder = image.SortOrder;
        existingImage.IsPrimary = image.IsPrimary;
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedStyleScenariosAsync()
  {
    var defaults = new[]
    {
      new { Slug = "giao-vien", Name = "Giáo viên", Description = "Trang phục nền nã, chỉn chu cho môi trường học đường." },
      new { Slug = "le-tet", Name = "Lễ Tết", Description = "Trang phục nổi bật, tươi sáng cho dịp lễ và chụp hình." },
      new { Slug = "du-tiec", Name = "Dự tiệc", Description = "Phối đồ sang trọng cho các sự kiện trang trọng." },
      new { Slug = "chup-anh", Name = "Chụp ảnh", Description = "Trang phục có điểm nhấn để lên hình đẹp." }
    };

    foreach (var item in defaults)
    {
      var scenario = await dbContext.StyleScenarios.FirstOrDefaultAsync(x => x.Slug == item.Slug);
      if (scenario is null)
      {
        dbContext.StyleScenarios.Add(new StyleScenario
        {
          Slug = item.Slug,
          Name = item.Name,
          Description = item.Description,
          IsActive = true
        });
        continue;
      }

      scenario.Name = item.Name;
      scenario.Description = item.Description;
      scenario.IsActive = true;
      scenario.UpdatedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedProductStyleDataAsync()
  {
    var products = await dbContext.Products
      .Include(product => product.StyleProfiles)
      .Include(product => product.Scenarios)
      .ToListAsync();

    var scenarios = await dbContext.StyleScenarios.ToDictionaryAsync(item => item.Slug);

    foreach (var product in products)
    {
      var profile = product.StyleProfiles.FirstOrDefault();
      if (profile is null)
      {
        profile = new ProductStyleProfile
        {
          Product = product
        };
        product.StyleProfiles.Add(profile);
      }

      var (primaryColor, secondaryColor) = InferColorFamilies(product);
      profile.Formality = InferFormality(product);
      profile.Silhouette = product.ProductType == "ao_dai" ? "ao-dai-truyen-thong" : "accessory";
      profile.PrimaryColorFamily = primaryColor;
      profile.SecondaryColorFamily = secondaryColor;
      profile.Notes = product.ProductType == "ao_dai"
        ? "Ưu tiên tư vấn theo dịp sử dụng và phụ kiện đi kèm."
        : "Dùng để hoàn thiện set áo dài trong chat stylist và try-on.";
      profile.StyleKeywordsJsonb = JsonSerializer.Serialize(InferStyleKeywords(product));
      profile.UpdatedAt = DateTime.UtcNow;

      product.Scenarios.Clear();
      foreach (var scenarioSeed in InferScenarioScores(product))
      {
        if (!scenarios.TryGetValue(scenarioSeed.Slug, out var scenario))
        {
          continue;
        }

        product.Scenarios.Add(new ProductScenario
        {
          Product = product,
          Scenario = scenario,
          Score = scenarioSeed.Score,
          Notes = scenarioSeed.Notes,
          UpdatedAt = DateTime.UtcNow
        });
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedProductAiAssetsAsync()
  {
    var products = await dbContext.Products
      .Include(product => product.Images)
      .Include(product => product.Variants)
      .Include(product => product.AiAssets)
      .ToListAsync();

    foreach (var product in products)
    {
      var primaryImageUrl = product.Images
        .OrderBy(image => image.SortOrder)
        .FirstOrDefault(image => image.IsPrimary)?.ImageUrl
        ?? product.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.ImageUrl;

      if (string.IsNullOrWhiteSpace(primaryImageUrl))
      {
        continue;
      }

      var assetKind = product.ProductType == "ao_dai" ? "tryon_garment" : "tryon_accessory";
      var curatedAssetKind = product.ProductType == "ao_dai" ? "tryon_garment_curated" : "tryon_accessory_curated";
      var defaultVariantId = product.ProductType == "ao_dai"
        ? product.Variants.OrderByDescending(variant => variant.IsDefault).ThenBy(variant => variant.Id).Select(variant => (long?)variant.Id).FirstOrDefault()
        : null;
      var mimeType = ResolveMimeType(primaryImageUrl);

      var curatedUrl = TryResolveCuratedAssetUrl(product);
      if (!string.IsNullOrWhiteSpace(curatedUrl))
      {
        UpsertAiAsset(product, curatedAssetKind, curatedUrl, ResolveMimeType(curatedUrl), defaultVariantId);
      }

      UpsertAiAsset(product, assetKind, primaryImageUrl, mimeType, defaultVariantId);
    }

    await dbContext.SaveChangesAsync();
  }

  private static void UpsertAiAsset(
    Product product,
    string assetKind,
    string fileUrl,
    string mimeType,
    long? defaultVariantId)
  {
    var existingAsset = product.AiAssets.FirstOrDefault(asset =>
      asset.AssetKind == assetKind &&
      asset.FileUrl == fileUrl);

    if (existingAsset is null)
    {
      product.AiAssets.Add(new ProductAiAsset
      {
        VariantId = defaultVariantId,
        AssetKind = assetKind,
        FileUrl = fileUrl,
        MimeType = mimeType,
        IsActive = true
      });
      return;
    }

    existingAsset.VariantId = existingAsset.VariantId ?? defaultVariantId;
    existingAsset.MimeType = mimeType;
    existingAsset.IsActive = true;
    existingAsset.UpdatedAt = DateTime.UtcNow;
  }

  private string? TryResolveCuratedAssetUrl(Product product)
  {
    var categoryFolder = product.ProductType == "ao_dai" ? "garments" : "accessories";
    foreach (var extension in new[] { ".png", ".webp", ".jpg", ".jpeg" })
    {
      var relativePath = Path.Combine(CuratedTryOnRoot, categoryFolder, $"{product.Slug}{extension}");
      var uploadRelativePath = relativePath["upload".Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      var absolutePath = uploadStoragePathResolver.GetAbsolutePathForRelativePath(uploadRelativePath);
      if (File.Exists(absolutePath))
      {
        return $"/{relativePath.Replace("\\", "/")}";
      }
    }

    return null;
  }

  private async Task RemoveStaleProductsAsync()
  {
    var currentSlugs = DefaultProducts.Items.Select(x => x.Slug).ToHashSet();
    var staleProducts = await dbContext.Products
      .Where(x => x.Brand == "Nha Uyen" && !currentSlugs.Contains(x.Slug))
      .ToListAsync();

    if (staleProducts.Count == 0)
    {
      return;
    }

    dbContext.Products.RemoveRange(staleProducts);
    await dbContext.SaveChangesAsync();
  }

  private async Task RemoveStaleCategoriesAsync()
  {
    var currentSlugs = DefaultCategories.Items.Select(x => x.Slug).ToHashSet();
    var staleCategories = await dbContext.Categories
      .Where(x => !currentSlugs.Contains(x.Slug) && !x.Products.Any())
      .OrderByDescending(x => x.Parent.HasValue)
      .ToListAsync();

    if (staleCategories.Count == 0)
    {
      return;
    }

    dbContext.Categories.RemoveRange(staleCategories);
    await dbContext.SaveChangesAsync();
  }

  private static void UpsertVariant(Product product, SeedProductVariant item)
  {
    var variant = product.Variants.FirstOrDefault(x => x.Sku == item.Sku);
    if (variant is null)
    {
      product.Variants.Add(new ProductVariant
      {
        Sku = item.Sku,
        VariantName = item.VariantName,
        Size = item.Size,
        Color = item.Color,
        Price = item.Price,
        SalePrice = item.SalePrice,
        StockQty = item.StockQty,
        IsDefault = item.IsDefault,
        Status = "active"
      });

      return;
    }

    variant.VariantName = item.VariantName;
    variant.Size = item.Size;
    variant.Color = item.Color;
    variant.Price = item.Price;
    variant.SalePrice = item.SalePrice;
    variant.StockQty = item.StockQty;
    variant.IsDefault = item.IsDefault;
    variant.Status = "active";
    variant.UpdatedAt = DateTime.UtcNow;
  }

  private async Task<string> ResolveProductTypeAsync(Category category)
  {
    if (category.Slug == "phu-kien")
    {
      return "phu_kien";
    }

    if (category.Parent is null)
    {
      return "ao_dai";
    }

    var parent = await dbContext.Categories.FirstAsync(x => x.Id == category.Parent);
    return parent.Slug == "phu-kien" ? "phu_kien" : "ao_dai";
  }

  private static string[] InferStyleKeywords(Product product)
  {
    if (product.ProductType == "phu_kien")
    {
      return ["phu-kien", product.Category.Slug, "phoi-set", "ao-dai"];
    }

    return product.Category.Slug switch
    {
      "ao-dai-cach-tan" => ["cach-tan", "tre-trung", "hien-dai"],
      "ao-dai-lua-tron" => ["toi-gian", "thanh-lich", "lua"],
      "ao-dai-theu-hoa" => ["theu-hoa", "nu-tinh", "diem-nhan"],
      _ => ["truyen-thong", "thanh-lich", "ao-dai"]
    };
  }

  private static string InferFormality(Product product)
  {
    if (product.ProductType == "phu_kien")
    {
      return "medium";
    }

    return product.Category.Slug switch
    {
      "ao-dai-cach-tan" => "medium",
      "ao-dai-lua-tron" => "medium",
      "ao-dai-theu-hoa" => "high",
      _ => "high"
    };
  }

  private static (string Primary, string Secondary) InferColorFamilies(Product product)
  {
    var slug = product.Slug.ToLowerInvariant();
    if (slug.Contains("hong"))
    {
      return ("pink", "gold");
    }

    if (slug.Contains("xanh"))
    {
      return ("blue", "white");
    }

    if (slug.Contains("do") || slug.Contains("node"))
    {
      return ("red", "gold");
    }

    return product.ProductType == "phu_kien"
      ? ("gold", "ivory")
      : ("ivory", "gold");
  }

  private static IReadOnlyList<(string Slug, decimal Score, string Notes)> InferScenarioScores(Product product)
  {
    if (product.ProductType == "phu_kien")
    {
      return
      [
        ("le-tet", 0.88m, "Phụ kiện tăng độ hoàn thiện cho set lễ Tết."),
        ("chup-anh", 0.82m, "Phụ kiện giúp set lên hình có điểm nhấn.")
      ];
    }

    return product.Category.Slug switch
    {
      "ao-dai-cach-tan" =>
      [
        ("chup-anh", 0.90m, "Phù hợp chụp ảnh và sự kiện trẻ trung."),
        ("du-tiec", 0.78m, "Phối được cho các dịp dự tiệc bán trang trọng.")
      ],
      "ao-dai-lua-tron" =>
      [
        ("giao-vien", 0.92m, "Tối giản, nền nã và phù hợp môi trường học đường."),
        ("le-tet", 0.74m, "Phù hợp lễ Tết khi phối thêm phụ kiện.")
      ],
      "ao-dai-theu-hoa" =>
      [
        ("du-tiec", 0.94m, "Thiết kế nổi bật phù hợp đi tiệc."),
        ("chup-anh", 0.89m, "Lên hình đẹp nhờ chi tiết thêu.")
      ],
      _ =>
      [
        ("giao-vien", 0.80m, "Phù hợp những dịp cần sự chỉn chu."),
        ("le-tet", 0.86m, "Trang phục nổi bật cho dịp truyền thống.")
      ]
    };
  }

  private static string ResolveMimeType(string fileUrl)
  {
    var extension = Path.GetExtension(fileUrl)?.ToLowerInvariant();
    return extension switch
    {
      ".png" => "image/png",
      ".jpg" => "image/jpeg",
      ".jpeg" => "image/jpeg",
      ".webp" => "image/webp",
      _ => "application/octet-stream"
    };
  }
}
