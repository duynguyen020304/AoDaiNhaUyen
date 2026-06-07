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
  IUploadStoragePathResolver uploadStoragePathResolver,
  IStorageService storageService) : ISeedDataService
{
  private const string CuratedTryOnRoot = "upload/tryon-curated";

  public async Task SeedAllAsync()
  {
    await dbContext.Database.MigrateAsync();

    ValidateS3Configuration();

    await SeedRolesAsync();
    await SeedAdminAsync();
    await SeedCustomersAsync();
    await SeedCategoriesAsync();
    await SeedProductImagesToS3Async();
    await SeedCuratedTryOnAssetsToS3Async();
    await SeedProductsAsync();
    await SeedStyleScenariosAsync();
    await SeedProductStyleDataAsync();
    await SeedProductAiAssetsAsync();
    await SeedPromoCodesAsync();
    await SeedDemoOrdersAsync();
    await SeedLowStockVariantsAsync();
    await RemoveStaleCategoriesAsync();
  }

  private void ValidateS3Configuration()
  {
    if (!storageService.IsConfigured())
    {
      throw new InvalidOperationException(
        "S3Storage chưa được cấu hình. Vui lòng đặt S3Storage__BucketName, S3Storage__Region (hoặc S3Storage__ServiceUrl) trong .env trước khi chạy seed.");
    }
  }

  private async Task SeedProductImagesToS3Async()
  {
    var uploadRoot = uploadStoragePathResolver.UploadRootPath;

    foreach (var product in DefaultProducts.Items)
    {
      foreach (var image in product.Images)
      {
        // image.ImageUrl now stores S3 object key like "aodainhauyen/private/products/{slug}.webp"
        var objectKey = image.ImageUrl;

        // Extract slug from object key: aodainhauyen/private/products/{slug}.webp
        var fileName = Path.GetFileName(objectKey);
        var localPath = Path.Combine(uploadRoot, fileName);

        if (!File.Exists(localPath))
        {
          continue; // Skip if local file doesn't exist (already migrated or missing)
        }

        if (!await storageService.ExistsAsync(objectKey))
        {
          using var stream = File.OpenRead(localPath);
          await storageService.PutObjectWithKeyAsync(objectKey, stream, "image/webp", ct: CancellationToken.None);
        }
        
        // Ensure the public copy exists since seeded products are 'active' by default
        var publicObjectKey = $"aodainhauyen/public/products/{fileName}";
        if (!await storageService.ExistsAsync(publicObjectKey))
        {
          await storageService.CopyToPublicAsync(objectKey, CancellationToken.None);
        }
      }
    }
  }

  private async Task SeedCuratedTryOnAssetsToS3Async()
  {
    var uploadRoot = uploadStoragePathResolver.UploadRootPath;
    var categories = new[] { "garments", "accessories" };

    foreach (var category in categories)
    {
      var localFolder = Path.Combine(uploadRoot, "tryon-curated", category);
      if (!Directory.Exists(localFolder))
      {
        continue;
      }

      var s3Prefix = category == "garments"
        ? DefaultProducts.S3PrivateTryOnGarmentsPrefix
        : DefaultProducts.S3PrivateTryOnAccessoriesPrefix;

      foreach (var filePath in Directory.GetFiles(localFolder))
      {
        var fileName = Path.GetFileName(filePath);
        var objectKey = $"{s3Prefix}/{fileName}";

        if (await storageService.ExistsAsync(objectKey))
        {
          continue;
        }

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
          ".png" => "image/png",
          ".jpg" or ".jpeg" => "image/jpeg",
          ".webp" => "image/webp",
          _ => "application/octet-stream"
        };

        using var stream = File.OpenRead(filePath);
        await storageService.PutObjectWithKeyAsync(objectKey, stream, contentType, ct: CancellationToken.None);
      }
    }
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

  private async Task SeedAdminAsync()
  {
    var adminEmail = Environment.GetEnvironmentVariable("AdminSeed__Email")?.Trim();
    var adminPassword = Environment.GetEnvironmentVariable("AdminSeed__Password")?.Trim();

    if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
      return;

    var adminRole = await dbContext.Roles.FirstAsync(x => x.Name == "admin");
    var normalizedEmail = adminEmail.Trim().ToLowerInvariant();

    var user = await dbContext.Users
      .Include(x => x.UserRoles)
      .FirstOrDefaultAsync(x => x.Email == normalizedEmail);

    if (user is null)
    {
      user = new User
      {
        FullName = "Admin",
        Email = normalizedEmail,
        Phone = "",
        Gender = "other",
        Status = "active",
        EmailVerifiedAt = DateTime.UtcNow
      };
      dbContext.Users.Add(user);
    }

    if (!user.UserRoles.Any(x => x.RoleId == adminRole.Id))
    {
      user.UserRoles.Add(new UserRole { User = user, RoleId = adminRole.Id });
    }

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
        PasswordHash = passwordHasher.HashPassword(adminPassword),
        IsVerified = true
      });
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
          IsFeatured = item.IsFeatured,
          IsPublic = true
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
        product.IsPublic = true;
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
            IsPrimary = image.IsPrimary,
            IsPublic = true,
            PublicObjectKey = $"aodainhauyen/public/products/{image.ImageUrl[(image.ImageUrl.LastIndexOf('/') + 1)..]}"
          });
          continue;
        }

        existingImage.AltText = image.AltText;
        existingImage.Variant = image.IsPrimary ? defaultVariant : null;
        existingImage.SortOrder = image.SortOrder;
        existingImage.IsPrimary = image.IsPrimary;
        existingImage.IsPublic = true;
        existingImage.PublicObjectKey = $"aodainhauyen/public/products/{image.ImageUrl[(image.ImageUrl.LastIndexOf('/') + 1)..]}";
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
    var seedBySlug = DefaultProducts.Items
      .Where(x => x.AiMetadata != null)
      .ToDictionary(x => x.Slug, x => x.AiMetadata!);

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

      var hasSeed = seedBySlug.TryGetValue(product.Slug, out var seedMeta);
      var colors = hasSeed
        ? (seedMeta!.PrimaryColorFamily, seedMeta.SecondaryColorFamily)
        : InferColorFamilies(product);
      var primaryColor = colors.Item1;
      var secondaryColor = colors.Item2;

      profile.Formality = hasSeed ? seedMeta!.Formality : InferFormality(product);
      profile.Silhouette = product.ProductType == "ao_dai" ? "ao-dai-truyen-thong" : "accessory";
      profile.PrimaryColorFamily = primaryColor;
      profile.SecondaryColorFamily = secondaryColor;
      profile.Notes = hasSeed ? seedMeta!.ProfileNotes : (product.ProductType == "ao_dai"
        ? "Ưu tiên tư vấn theo dịp sử dụng và phụ kiện đi kèm."
        : "Dùng để hoàn thiện set áo dài trong chat stylist và try-on.");
      profile.StyleKeywordsJsonb = JsonSerializer.Serialize(hasSeed ? seedMeta!.StyleKeywords : InferStyleKeywords(product));
      profile.UpdatedAt = DateTime.UtcNow;

      product.Scenarios.Clear();
      var scenarioSeeds = hasSeed
        ? seedMeta!.ScenarioScores.Select(s => (s.Slug, s.Score, s.Notes)).ToList()
        : InferScenarioScores(product);

      foreach (var scenarioSeed in scenarioSeeds)
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
        ? product.Variants.OrderByDescending(variant => variant.IsDefault).ThenBy(variant => (Guid?)variant.Id).Select(variant => (Guid?)variant.Id).FirstOrDefault()
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
    Guid? defaultVariantId)
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

      if (storageService.IsConfigured())
      {
        // Use S3 key if configured — seed uploaded local files to S3, so check local existence
        if (File.Exists(absolutePath))
        {
          var s3Prefix = categoryFolder == "garments"
            ? DefaultProducts.S3PrivateTryOnGarmentsPrefix
            : DefaultProducts.S3PrivateTryOnAccessoriesPrefix;
          var fileName = $"{product.Slug}{extension}";
          return $"{s3Prefix}/{fileName}";
        }

        continue;
      }

      // Legacy fallback: local filesystem
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

  private async Task SeedPromoCodesAsync()
  {
    var now = DateTime.UtcNow;
    var promoData = new[]
    {
      new { Code = "CHAOMUNG", Type = "percentage", Value = 15m, MinOrder = 200000m, MaxUses = 100, FreeShipping = false },
      new { Code = "FREESHIP", Type = "percentage", Value = 0m, MinOrder = 300000m, MaxUses = 0, FreeShipping = true },
      new { Code = "NHANQUA", Type = "fixed", Value = 50000m, MinOrder = 500000m, MaxUses = 50, FreeShipping = false },
    };

    foreach (var data in promoData)
    {
      var exists = await dbContext.PromoCodes.AnyAsync(p => p.Code == data.Code);
      if (exists) continue;

      dbContext.PromoCodes.Add(new PromoCode
      {
        Code = data.Code,
        DiscountType = data.Type,
        DiscountValue = data.Value,
        MinOrderAmount = data.MinOrder,
        MaxUses = data.MaxUses,
        CurrentUses = 0,
        StartDate = now.AddDays(-1),
        EndDate = now.AddDays(30),
        IsActive = true,
        FreeShipping = data.FreeShipping,
        CreatedAt = now,
        UpdatedAt = now
      });
    }

    await dbContext.SaveChangesAsync();
  }
  private async Task SeedDemoOrdersAsync()
  {
    var hasOrders = await dbContext.Orders.AnyAsync();
    if (hasOrders) return;

    var customer = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "ha.an@example.com");
    if (customer is null) return;

    var variants = await dbContext.ProductVariants
      .Include(v => v.Product)
      .Where(v => v.Product != null && !v.Product.IsDeleted)
      .Take(6)
      .ToListAsync();
    if (variants.Count == 0) return;

    var now = DateTime.UtcNow;

    var statuses = new[]
    {
      new { Status = "completed",    DaysAgo = 10, HasShipment = true,  ShipStatus = "delivered" },
      new { Status = "shipping",     DaysAgo = 3,  HasShipment = true,  ShipStatus = "shipped"   },
      new { Status = "processing",   DaysAgo = 1,  HasShipment = false, ShipStatus = ""          },
      new { Status = "confirmed",    DaysAgo = 0,  HasShipment = false, ShipStatus = ""          },
      new { Status = "pending",      DaysAgo = 0,  HasShipment = false, ShipStatus = ""          },
      new { Status = "cancelled",    DaysAgo = 5,  HasShipment = false, ShipStatus = ""          },
    };

    for (var i = 0; i < Math.Min(statuses.Length, variants.Count); i++)
    {
      var s = statuses[i];
      var variant = variants[i];
      var unitPrice = variant.SalePrice ?? variant.Price;
      var quantity = 2;
      var subtotal = unitPrice * quantity;
      var shippingFee = subtotal >= 500000m ? 0m : 25000m;
      var placedAt = now.AddDays(-s.DaysAgo).AddHours(-i);

      var order = new Order
      {
        OrderCode = $"AD-{placedAt:yyyyMMddHHmmss}{i}",
        UserId = customer.Id,
        RecipientName = customer.FullName,
        RecipientPhone = customer.Phone ?? "0901000001",
        Province = "TP. Hồ Chí Minh",
        District = "Quận 1",
        Ward = "Phường Bến Nghé",
        AddressLine = $"{100 + i} Nguyễn Huệ",
        Subtotal = subtotal,
        DiscountAmount = 0m,
        ShippingFee = shippingFee,
        TotalAmount = subtotal + shippingFee,
        OrderStatus = s.Status,
        PlacedAt = placedAt,
        CreatedAt = placedAt,
        UpdatedAt = placedAt
      };

      if (s.Status is "confirmed" or "processing" or "shipping" or "completed")
        order.ConfirmedAt = placedAt.AddHours(1);
      if (s.Status is "completed")
        order.CompletedAt = placedAt.AddDays(3);
      if (s.Status is "cancelled")
        order.CancelledAt = placedAt.AddHours(2);

      order.Items.Add(new OrderItem
      {
        ProductId = variant.ProductId,
        VariantId = variant.Id,
        ProductName = variant.Product!.Name,
        Sku = variant.Sku,
        Size = variant.Size,
        Color = variant.Color,
        UnitPrice = unitPrice,
        Quantity = quantity,
        LineTotal = subtotal,
        CreatedAt = placedAt
      });

      order.Payment = new Payment
      {
        Amount = subtotal + shippingFee,
        PaidAt = placedAt,
        Note = "paid_successfully",
        CreatedAt = placedAt
      };

      if (s.HasShipment)
      {
        order.Shipments.Add(new Shipment
        {
          ShippingStatus = s.ShipStatus,
          Carrier = "GHN",
          TrackingNumber = $"GHN{placedAt:yyyyMMdd}{i:D4}",
          ShippedAt = s.ShipStatus is "shipped" or "delivered" ? placedAt.AddHours(6) : null,
          DeliveredAt = s.ShipStatus == "delivered" ? placedAt.AddDays(3) : null,
          CreatedAt = placedAt
        });
      }

      dbContext.Orders.Add(order);
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedLowStockVariantsAsync()
  {
    var hasLowStock = await dbContext.ProductVariants.AnyAsync(v => v.StockQty <= 3);
    if (hasLowStock) return;

    var variants = await dbContext.ProductVariants
      .OrderBy(v => v.CreatedAt)
      .Take(4)
      .ToListAsync();

    if (variants.Count == 0) return;

    var lowStockValues = new[] { 1, 2, 2, 3 };
    for (var i = 0; i < variants.Count; i++)
    {
      variants[i].StockQty = lowStockValues[i];
      variants[i].UpdatedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync();
  }

}
