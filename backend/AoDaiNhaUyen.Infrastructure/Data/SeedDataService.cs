using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Domain.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AoDaiNhaUyen.Infrastructure.Data;

public sealed class SeedDataService(
  AppDbContext dbContext,
  IPasswordHasher passwordHasher,
  IUploadStoragePathResolver uploadStoragePathResolver,
  IStorageService storageService,
  IOptions<AdminSeedOptions> adminSeedOptions) : ISeedDataService
{
  // ── Demo seed helper data ──────────────────────────

  private const string DemoPassword = "demo123";

  private static readonly string[] DemoFamilyNames =
    ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ",
     "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Trịnh", "Đoàn", "Mai", "Tô"];

  private static readonly string[] DemoMiddleNamesFemale =
    ["Thị", "Ngọc", "Thanh", "Minh", "Lê", "Hồng", "Kim", "Bích", "Diệu", "Mỹ"];

  private static readonly string[] DemoMiddleNamesMale =
    ["Văn", "Thanh", "Minh", "Đức", "Quang", "Hữu", "Thế", "Công", "Xuân", "Tiến"];

  private static readonly string[] DemoFemaleGivenNames =
    ["Anh", "An", "Bình", "Chi", "Diễm", "Dung", "Giang", "Hà", "Hạnh", "Hiền",
     "Hoa", "Hồng", "Hương", "Khánh", "Lan", "Linh", "Mai", "My", "Ngọc", "Nhung",
     "Phương", "Quỳnh", "Thảo", "Thanh", "Thi", "Thoa", "Thư", "Thùy", "Trang", "Tú",
     "Uyên", "Vân", "Vy", "Yến", "Ánh", "Diệu", "Kiều", "Loan", "Nga", "Nhi",
     "Phúc", "Quyên", "Tâm", "Thắm", "Thúy", "Trâm", "Xuân", "Như"];

  private static readonly string[] DemoMaleGivenNames =
    ["An", "Bách", "Bảo", "Cường", "Dũng", "Dương", "Đạt", "Đức", "Gia", "Hải",
     "Hiếu", "Hoàng", "Hùng", "Huy", "Khang", "Kiên", "Long", "Luân", "Minh", "Nam",
     "Nghĩa", "Nguyên", "Nhân", "Phát", "Phong", "Phú", "Quang", "Quốc", "Sơn", "Tài",
     "Tâm", "Thắng", "Thành", "Thế", "Thiện", "Thuận", "Tín", "Trí", "Trung", "Tuấn",
     "Tùng", "Việt", "Vũ", "Anh"];

  private static readonly (string Province, string District, string[] Wards)[] DemoAddressTemplates =
    [("TP. Hồ Chí Minh", "Quận 1",
      ["Phường Bến Nghé", "Phường Bến Thành", "Phường Cô Giang", "Phường Cầu Kho", "Phường Nguyễn Cư Trinh"]),
     ("TP. Hồ Chí Minh", "Quận 3",
      ["Phường Võ Thị Sáu", "Phường 6", "Phường 7", "Phường 10"]),
     ("TP. Hồ Chí Minh", "Quận Bình Thạnh",
      ["Phường 1", "Phường 7", "Phường 12", "Phường 19", "Phường 22"]),
     ("TP. Hồ Chí Minh", "Quận Phú Nhuận",
      ["Phường 2", "Phường 4", "Phường 9", "Phường 11"]),
     ("TP. Hà Nội", "Quận Hoàn Kiếm",
      ["Phường Hàng Bạc", "Phường Hàng Buồm", "Phường Hàng Đào"]),
     ("TP. Hà Nội", "Quận Đống Đa",
      ["Phường Cát Linh", "Phường Kim Liên", "Phường Quốc Tử Giám"]),
     ("TP. Đà Nẵng", "Quận Hải Châu",
      ["Phường Hải Châu 1", "Phường Thạch Thang", "Phường Thanh Bình", "Phường Thuận Phước"]),
     ("TP. Cần Thơ", "Quận Ninh Kiều",
      ["Phường An Phú", "Phường An Nghiệp", "Phường Tân An"]),
     ("TP. Huế", "Quận Thuận Hóa",
      ["Phường Vỹ Dạ", "Phường Phú Hội", "Phường Phú Nhuận"]),
     ("Tỉnh Bình Dương", "TP. Thủ Dầu Một",
      ["Phường Phú Cường", "Phường Chánh Nghĩa", "Phường Phú Hòa"])];

  private static readonly string[] DemoStreetNames =
    ["Nguyễn Huệ", "Lê Lợi", "Điện Biên Phủ", "Hai Bà Trưng", "Nguyễn Đình Chiểu",
     "Trần Hưng Đạo", "Võ Văn Tần", "Nguyễn Trãi", "Đinh Tiên Hoàng", "Phạm Ngọc Thạch",
     "Xô Viết Nghệ Tĩnh", "Nam Kỳ Khởi Nghĩa", "Cách Mạng Tháng Tám", "Lê Văn Sỹ",
     "Nguyễn Văn Trỗi", "Hoàng Diệu", "Nguyễn Tri Phương", "Lý Tự Trọng", "Bà Triệu",
     "Trường Chinh", "Phan Đăng Lưu", "Ngô Gia Tự", "Hùng Vương", "Nguyễn Kiệm"];

  private static readonly string[] DemoPositiveReviewTexts =
    ["Áo dài đẹp tuyệt vời! Chất liệu cao cấp, đường may tinh tế. Mình rất hài lòng và sẽ mua thêm.",
     "Giao hàng nhanh, đóng gói cẩn thận. Áo dài đúng như mô tả, màu sắc đẹp hơn cả mong đợi.",
     "Lần đầu mua áo dài online mà trải nghiệm quá tốt. Form áo chuẩn, vải mềm mịn. Rất đáng tiền!",
     "Chị Uyên tư vấn rất nhiệt tình, giúp mình chọn được size ưng ý. Áo đẹp, sang trọng đúng kiểu mình tìm.",
     "Mua tặng mẹ, mẹ mình rất thích. Áo dài thêu hoa tinh xảo, màu sắc trang nhã. Cảm ơn shop!",
     "Áo dài cưới đẹp xuất sắc! Thiết kế tinh tế, chất liệu lụa cao cấp. Mình đã giới thiệu cho bạn bè.",
     "Đúng là áo dài Nha Uyên - chất lượng miễn bàn. Đường chỉ may sắc nét, họa tiết thêu sống động.",
     "Mình đặt áo dài cách tân đi dự tiệc, ai cũng khen. Form dáng tôn lên mọi đường cong. 10 điểm!"];

  private static readonly string[] DemoNeutralReviewTexts =
    ["Áo đẹp nhưng giao hàng hơi chậm hơn dự kiến 2 ngày. Nhìn chung vẫn hài lòng về sản phẩm.",
     "Chất lượng ổn, vừa vặn. Màu sắc hơi khác so với ảnh một chút nhưng vẫn đẹp.",
     "Áo dài đẹp, vải tốt. Chỉ tiếc là size M hơi rộng hơn mình tưởng. Lần sau mình sẽ chọn size S.",
     "Sản phẩm tốt, giá hợp lý. Mong shop có thêm nhiều mẫu áo dài truyền thống hơn."];

  private static readonly string[] DemoNegativeReviewTexts =
    ["Áo bị lỗi chỉ ở phần cổ, phải tự sửa lại mới mặc được. Hơi thất vọng vì giá không rẻ.",
     "Màu áo khác hoàn toàn so với ảnh trên web. Mình đặt màu đỏ đô mà nhận được áo màu đỏ cam. Không hài lòng!",
     "Giao nhầm size, mình đặt size M mà giao size L. Đã liên hệ đổi trả nhưng xử lý chậm.",
     "Chất liệu vải không như mô tả, hơi cứng và nóng. Không phù hợp mặc mùa hè."];

  private static readonly string[] DemoQuestionComments =
    ["Cho mình hỏi áo này có size XL không ạ?",
     "Màu xanh này lên người có bị tối không shop?",
     "Áo này có thể đặt may theo số đo riêng không ạ?",
     "Khi nào shop có hàng size S về lại ạ?",
     "Áo dài này có ship về Huế không shop? Bao lâu thì nhận được ạ?",
     "Mình muốn đặt áo cưới gấp trong 5 ngày, shop hỗ trợ được không?",
     "Chất liệu lụa tơ tằm có cần giặt khô không ạ?",
     "Shop có nhận đặt may áo dài cho bé gái 10 tuổi không?"];

  private const string CuratedTryOnRoot = "upload/tryon-curated";

  public async Task SeedAllAsync()
  {
    await dbContext.Database.MigrateAsync();

    await SeedRolesAsync();
    await SeedAdminAsync();
    await SeedEmailTemplatesAsync();

    if (await HasExistingCatalogDataAsync())
    {
      return;
    }

    ValidateS3Configuration();

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
    await SeedDemoReviewsAsync();
    await SeedBlogPostsAsync();
    await SeedToolRiskConfigsAsync();
    await RemoveStaleCategoriesAsync();
  }

  private Task<bool> HasExistingCatalogDataAsync()
  {
    return dbContext.Products.AsNoTracking().AnyAsync();
  }

  private async Task SeedEmailTemplatesAsync()
  {
    var now = DateTime.UtcNow;
    var templates = new[]
    {
      new DevEmailTemplateSeed(
        Key: "marketing.promo",
        Name: "Khuyến mãi",
        Subject: "Ưu đãi áo dài dành riêng cho bạn",
        Preheader: "Khám phá ưu đãi mới nhất từ Áo Dài Nhã Uyên",
        TemplateType: "marketing.promo",
        Config: new Dictionary<string, string>
        {
          ["heading"] = "Ưu đãi áo dài cuối tuần",
          ["intro"] = "Một lựa chọn tinh tế cho những khoảnh khắc đặc biệt.",
          ["body"] = "Nhận ưu đãi cho các thiết kế áo dài mới, chất liệu mềm mại và phom dáng tôn nét Việt.",
          ["ctaText"] = "Xem ưu đãi",
          ["ctaUrl"] = "https://aodainhauyen.io.vn/products",
          ["footerNote"] = "Ưu đãi có thể kết thúc sớm khi hết số lượng."
        }),
      new DevEmailTemplateSeed(
        Key: "marketing.newsletter",
        Name: "Newsletter",
        Subject: "Bản tin Áo Dài Nhã Uyên",
        Preheader: "Cảm hứng mặc đẹp và câu chuyện áo dài mới nhất",
        TemplateType: "marketing.newsletter",
        Config: new Dictionary<string, string>
        {
          ["heading"] = "Cảm hứng áo dài trong tuần",
          ["intro"] = "Những gợi ý phối áo dài, câu chuyện chất liệu và thiết kế mới.",
          ["body"] = "Nhã Uyên chọn lọc các thiết kế trang nhã cho sự kiện gia đình, lễ hội và khoảnh khắc thường ngày.",
          ["ctaText"] = "Đọc thêm",
          ["ctaUrl"] = "https://aodainhauyen.com/blog"
        }),
      new DevEmailTemplateSeed(
        Key: "subscriber.welcome",
        Name: "Chào mừng đăng ký nhận tin",
        Subject: "Chào mừng bạn đến với Áo Dài Nhã Uyên",
        Preheader: "Cảm ơn bạn đã gia nhập cộng đồng yêu áo dài",
        TemplateType: "subscriber.welcome",
        Config: new Dictionary<string, string>
        {
          ["heading"] = "Chào mừng bạn đến với Áo Dài Nhã Uyên",
          ["intro"] = "Cảm ơn bạn đã đăng ký nhận tin.",
          ["body"] = "Bạn sẽ nhận cảm hứng mặc đẹp, mẹo chăm sóc áo dài và ưu đãi riêng.",
          ["ctaText"] = "Khám phá bộ sưu tập",
          ["ctaUrl"] = "https://aodainhauyen.io.vn/products"
        }),
      new DevEmailTemplateSeed(
        Key: "order.confirmation",
        Name: "Xác nhận đơn hàng",
        Subject: "Nhã Uyên đã nhận đơn hàng của bạn",
        Preheader: "Thông tin đơn hàng và bước xử lý tiếp theo",
        TemplateType: "order.confirmation",
        Config: new Dictionary<string, string>
        {
          ["heading"] = "Xác nhận đơn hàng",
          ["intro"] = "Cảm ơn bạn đã tin chọn Áo Dài Nhã Uyên.",
          ["body"] = "Chúng tôi đã nhận được đơn hàng và sẽ liên hệ khi đơn được xử lý.",
          ["ctaText"] = "Xem đơn hàng",
          ["ctaUrl"] = "https://aodainhauyen.com/account/orders",
          ["orderCode"] = "ADNU-2026-0001"
        })
    };

    foreach (var seed in templates)
    {
      var existing = await dbContext.EmailTemplates
        .FirstOrDefaultAsync(x => x.Key == seed.Key && x.Locale == "vi-VN" && x.Version == 1);

      var configJson = JsonSerializer.Serialize(seed.Config);
      if (existing is null)
      {
        dbContext.EmailTemplates.Add(new EmailTemplate
        {
          Id = Guid.NewGuid(),
          Key = seed.Key,
          Name = seed.Name,
          Subject = seed.Subject,
          Preheader = seed.Preheader,
          HtmlBody = string.Empty,
          TextBody = null,
          TemplateType = seed.TemplateType,
          ConfigJson = configJson,
          IsSystem = true,
          Locale = "vi-VN",
          Version = 1,
          IsActive = true,
          IsDeleted = false,
          CreatedAt = now,
          UpdatedAt = now
        });
        continue;
      }

      existing.Name = seed.Name;
      existing.Subject = seed.Subject;
      existing.Preheader = seed.Preheader;
      existing.HtmlBody = string.Empty;
      existing.TextBody = null;
      existing.TemplateType = seed.TemplateType;
      existing.ConfigJson = configJson;
      existing.IsSystem = true;
      existing.IsActive = true;
      existing.IsDeleted = false;
      existing.DeletedAt = null;
      existing.UpdatedAt = now;
    }

    var legacyWelcomeTemplates = await dbContext.EmailTemplates
      .Where(x => x.Key == "marketing.welcome")
      .ToListAsync();
    foreach (var legacy in legacyWelcomeTemplates)
    {
      legacy.IsActive = false;
      legacy.IsDeleted = true;
      legacy.DeletedAt = now;
      legacy.UpdatedAt = now;
    }

    await dbContext.SaveChangesAsync();
  }

  private sealed record DevEmailTemplateSeed(
    string Key,
    string Name,
    string Subject,
    string Preheader,
    string TemplateType,
    IReadOnlyDictionary<string, string> Config);

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

  private async Task SeedBlogPostsAsync()
  {
    // Clear existing blog posts/categories to update with the new premium seeded content
    var existingPosts = await dbContext.BlogPosts.ToListAsync();
    if (existingPosts.Count > 0)
    {
      dbContext.BlogPosts.RemoveRange(existingPosts);
      await dbContext.SaveChangesAsync();
    }

    var existingCategories = await dbContext.BlogCategories.ToListAsync();
    if (existingCategories.Count > 0)
    {
      dbContext.BlogCategories.RemoveRange(existingCategories);
      await dbContext.SaveChangesAsync();
    }

    var uploadRoot = uploadStoragePathResolver.UploadRootPath;
    var now = DateTime.UtcNow;

    async Task<string> BlogImageUrlAsync(string fileName, bool published)
    {
      var privateKey = $"aodainhauyen/private/blog/{fileName}";
      var localPath = Path.Combine(uploadRoot, fileName);
      if (File.Exists(localPath) && !await storageService.ExistsAsync(privateKey))
      {
        await using var stream = File.OpenRead(localPath);
        await storageService.PutObjectWithKeyAsync(privateKey, stream, "image/webp");
      }

      if (!published)
      {
        return privateKey;
      }

      var publicKey = $"aodainhauyen/public/blog/{fileName}";
      if (!await storageService.ExistsAsync(publicKey))
      {
        await storageService.CopyToPublicBlogAsync(privateKey);
      }

      return storageService.BuildCanonicalUrl(publicKey);
    }

    string Json(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    var blogCategories = new[]
    {
      new BlogCategory { Name = "Văn hóa áo dài", Slug = "van-hoa-ao-dai", Description = "Lịch sử, di sản và câu chuyện sau tà áo dài Việt.", SortOrder = 1, CreatedAt = now, UpdatedAt = now },
      new BlogCategory { Name = "Lookbook & xu hướng", Slug = "lookbook-xu-huong", Description = "Bộ ảnh, cảm hứng phối đồ và xu hướng theo mùa.", SortOrder = 2, CreatedAt = now, UpdatedAt = now },
      new BlogCategory { Name = "Áo dài cưới", Slug = "ao-dai-cuoi", Description = "Tư vấn áo dài cô dâu, lễ gia tiên và vu quy.", SortOrder = 3, CreatedAt = now, UpdatedAt = now },
      new BlogCategory { Name = "Hướng dẫn chăm sóc", Slug = "huong-dan-cham-soc", Description = "Đo size, bảo quản lụa gấm và chăm sóc áo dài tại nhà.", SortOrder = 4, CreatedAt = now, UpdatedAt = now }
    };
    dbContext.BlogCategories.AddRange(blogCategories);
    await dbContext.SaveChangesAsync();

    var blogCategorySlugs = blogCategories.Select(category => category.Slug).ToArray();
    var blogCategoryBySlug = await dbContext.BlogCategories
      .AsNoTracking()
      .Where(category => blogCategorySlugs.Contains(category.Slug))
      .ToDictionaryAsync(category => category.Slug, category => category.Id);

    var productSlugs = await dbContext.Products.AsNoTracking().OrderBy(p => p.Name).Select(p => p.Slug).Take(4).ToListAsync();
    if (productSlugs.Count == 0)
    {
      productSlugs = ["ao-dai-truyen-thong", "ao-dai-lua-tron", "ao-dai-theu-hoa"];
    }

    var posts = new List<BlogPost>
    {
      new()
      {
        Title = "Lịch sử Áo Dài Việt Nam: Hành trình di sản ngàn năm thăng trầm",
        Slug = "lich-su-va-y-nghia-cua-ao-dai-viet-nam",
        Excerpt = "Tìm hiểu chi tiết lịch sử hình thành và phát triển của tà áo dài Việt Nam qua các thời kỳ lịch sử. Từ chiếc áo Giao Lĩnh cổ xưa thời Lý-Trần, áo ngũ thân thời Chúa Nguyễn, áo tứ thân Bắc Bộ mộc mạc, đến cuộc cách tân áo dài Lemur thời Pháp thuộc và kỹ thuật ráp vai Raglan Sài Gòn mang lại phom dáng hoàn mỹ ngày nay.",
        FeaturedImage = await BlogImageUrlAsync("home-ao-dai-truyen-thong.webp", true),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.StandardArticle,
        BlogCategoryId = blogCategoryBySlug["van-hoa-ao-dai"],
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "1. Khởi nguồn từ chiếc áo Giao Lĩnh cổ xưa thế kỷ 11 - 15" },
          new { type = "paragraph", content = "Theo các tư liệu khảo cổ và bia ký thời Lý - Trần - Lê, tiền thân lâu đời nhất của tà áo dài Việt Nam chính là chiếc áo Giao Lĩnh, hay còn được dân gian gọi với cái tên mộc mạc là áo đối lĩnh. Cấu trúc nguyên bản của y phục này gồm thân áo dài buông rộng xuôi thẳng xuống gót chân, cổ áo xẻ rộng hình chữ Y sang trọng, vạt trái đè chéo lên vạt phải khi mặc và được cố định chắc chắn bằng dải thắt lưng sồi buộc nhẹ ngang hông. Đây là y phục đại diện cho xã hội lúa nước Đông Nam Á buổi đầu tự chủ, dung hợp sâu sắc tính thực dụng trong lao động đồng áng dãi dầu mưa nắng và tính lễ nghi tôn nghiêm mỗi khi bước vào triều đường tế tự tổ tiên." },
          new { type = "image", src = await BlogImageUrlAsync("home-ao-dai-truyen-thong-node.webp", true), alt = "Bản phục dựng áo Giao Lĩnh cổ xưa Việt Nam thời Lý Trần", caption = "Áo Giao Lĩnh phục dựng - Biểu tượng mộc mạc, tôn nghiêm buổi đầu sơ khởi của trang phục Việt." },
          new { type = "paragraph", content = "Chất liệu dệt may thời bấy giờ chủ yếu xuất phát từ tơ tre dệt thô, tơ chuối hoặc sợi vải lanh thiên nhiên, được nhuộm màu sẫm từ củ nâu hoặc các loại lá rừng để phù hợp với môi trường sinh hoạt đồng áng dãi dầu mưa nắng. Cổ nhân coi y phục là thước đo của lễ nghĩa gia quy, do đó chiếc áo Giao Lĩnh không chỉ đơn thuần để bảo vệ cơ thể trước thời tiết mà còn là công cụ phân định trật tự trong cung đình phong kiến, biểu thị lòng tôn kính trời đất và tổ tiên của người Việt xưa." },
          new { type = "paragraph", content = "Mặc dù phom dáng ban đầu còn thô mộc và rộng rãi, áo Giao Lĩnh phản chiếu triết lý sống hài hòa với thiên nhiên của người Việt cổ. Từng đường cắt xẻ bên hông giúp người mặc dễ dàng bước đi, cúi gập cấy cày gieo mạ mà vẫn giữ được sự đoan trang thanh nhã cần thiết." },
          
          new { type = "heading", level = 2, content = "2. Cải cách trang phục Đàng Trong của Chúa Nguyễn Phúc Khoát năm 1744" },
          new { type = "paragraph", content = "Bước ngoặt lịch sử quan trọng nhất kiến tạo nên cấu trúc khép vạt chéo nách của áo dài hiện đại thuộc về thế kỷ 18. Nhằm khẳng định sự độc lập về chính trị và văn hóa của Đàng Trong, Chúa Nguyễn Phúc Khoát đã ban hành sắc dụ lịch sử quy định toàn dân từ quý tộc đến thứ dân đều phải mặc áo ngũ thân cổ đứng kết hợp với quần hai ống rộng phủ chân. Áo ngũ thân được ghép từ 5 vạt vải, trong đó bốn vạt ngoài đại diện cho tứ thân phụ mẫu là cha mẹ đẻ và cha mẹ chồng hoặc cha mẹ vợ, vạt thứ năm ẩn bên trong đại diện cho chính người mặc. Thiết kế cài khuy bên nách phải kín đáo này hướng tới việc che giấu khéo léo những đường cong cơ thể, hướng trọn vẹn nét thẩm mỹ vào vẻ đoan trang, nghiêm cẩn và gìn giữ gia phong nho học cũ." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-truyen-thong-4.webp", true), alt = "Người mẫu mặc áo dài ngũ thân truyền thống", caption = "Áo dài ngũ thân cổ đứng cài năm cúc - Nền tảng cấu trúc của chiếc áo dài truyền thống ngày nay." },
          new { type = "paragraph", content = "Sự xuất hiện của áo ngũ thân đi liền với sự phát triển rực rỡ của các làng nghề dệt lụa tơ tằm danh tiếng như Vạn Phúc ở Hà Đông hay Mã Châu ở Quảng Nam. Chất liệu gấm dệt hoa văn chìm, lụa đũi tơ tự nhiên bóng mịn bắt đầu được ứng dụng rộng rãi. Chi tiết năm cúc cài làm từ kim loại hoặc ngọc bích biểu thị ngũ thường bao gồm Nhân, Nghĩa, Lễ, Trí, Tín nhắc nhở người mặc luôn giữ gìn chuẩn mực đạo đức đạo làm người. Đây là tiền thân trực tiếp nhất của cấu trúc áo dài chúng ta đang mặc hôm nay." },
          new { type = "paragraph", content = "Dưới triều Nguyễn sau này, chiếc áo ngũ thân được nâng tầm thành quy chuẩn hoàng gia. Những chất liệu thượng hạng như sa, đoạn, nhiễu, gấm từ các xưởng dệt nội phủ dành riêng cho vua chúa, quan lại được trang trí thêu thùa hoa văn mây nước, rồng phượng tinh xảo. Đây chính là thời kỳ áo dài xác lập vị thế tôn nghiêm bậc nhất của mình." },
          
          new { type = "heading", level = 2, content = "3. Áo tứ thân Bắc Bộ: Hồn cốt mộc mạc của người con gái lao động" },
          new { type = "paragraph", content = "Song hành cùng dòng chảy quý tộc phong kiến, chiếc áo tứ thân ra đời và trở thành biểu tượng tinh thần bất diệt của phụ nữ nông thôn Bắc Bộ dạo xưa. Thiết kế gồm bốn vạt vải xẻ giữa ở thân trước và thân sau. Hai vạt sau được khâu liền thành sống lưng áo, hai vạt trước để tự do buông lơi hoặc thắt nút gọn gàng ngang bụng khi làm việc đồng áng. Kết hợp cùng yếm đào thắm đượm thêu hoa, chiếc thắt lưng sồi xanh lục rực rỡ và chiếc nón quai thao che nắng gió che chở khuôn mặt trái xoan, áo tứ thân tạo nên hình ảnh vừa tần tảo chịu thương chịu khó vừa duyên dáng quyến rũ kỳ lạ của người con gái thôn quê dạo ấy." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-truyen-thong-5.webp", true), alt = "Áo dài tứ thân yếm đào truyền thống", caption = "Nét đẹp đằm thắm của áo tứ thân Bắc Bộ kết hợp yếm đào thanh xuân quyến rũ." },
          new { type = "paragraph", content = "Màu nhuộm của áo tứ thân chủ yếu được lấy từ củ nâu nhuộm đất bùn sông Hồng để tạo ra màu nâu trầm hoặc đen sẫm, giúp giữ áo luôn sạch sẽ và bền bỉ trong suốt quá trình lao động mệt nhọc ngoài đồng ruộng. Tuy nhiên, trong những dịp hội làng xuân hè, các cô gái lại khéo léo khoác thêm những tà áo tứ thân nhuộm màu tươi sáng hơn như màu vàng mỡ gà, màu xanh lục bích tạo nên bức tranh văn hóa lễ hội rực rỡ sắc màu." },
          new { type = "paragraph", content = "Áo tứ thân không chỉ phục vụ lao động. Trong các làn điệu dân ca Quan họ Kinh Bắc ngọt ngào, tà áo tứ thân tung bay theo nhịp phách tiền, nón ba tầm trao duyên gửi gắm ân tình. Nó đại diện cho vẻ đẹp bình dị, thuần hậu và tâm hồn đầy nhạc họa của người dân lao động Bắc Bộ qua nhiều thế hệ." },
          
          new { type = "heading", level = 2, content = "4. Cuộc cách mạng Lemur Tây hóa và sự giải phóng hình thể phụ nữ thập niên 1930" },
          new { type = "paragraph", content = "Bước vào thập niên 1930, trào lưu Âu hóa thổi một luồng sinh khí mới mẻ vào đời sống đô thị Việt Nam. Họa sĩ Cát Tường, bút danh Lemur, thuộc nhóm trí thức Tự Lực Văn Đoàn đã khởi xướng một cuộc cách tân táo bạo chưa từng có. Ông loại bỏ cấu trúc rộng thùng thình của áo ngũ thân cổ điển để tạo ra chiếc áo dài Lemur ôm khít lấy ngực và eo phụ nữ. Cổ áo đứng cổ điển được thay thế bằng cổ lá sen bẻ rộng, cổ tròn khoét sâu gợi cảm, vai áo may bồng nhẹ kiểu đầm phương Tây và gấu áo dài quét đất thướt tha. Dù gặp phải làn sóng phản đối dữ dội từ giới nho học cựu trào coi đây là sự lai căng mất thuần phong mỹ tục, áo dài Lemur đã nhanh chóng chinh phục hoàn toàn giới nữ sinh, quý cô tân tiến Hà thành và Sài thành, đặt nền móng cho bước chuyển mình hiện đại." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-truyen-thong-2.webp", true), alt = "Áo dài Lemur cách tân tân thời Hà Nội xưa", caption = "Sự giải phóng hình thể nữ giới đầy táo bạo thông qua đường cắt ôm sát quyến rũ của áo dài Lemur." },
          new { type = "paragraph", content = "Sau áo dài Lemur, họa sĩ Lê Phổ đã có những điều chỉnh khôn ngoan để dung hòa giữa nét tân thời Âu Mỹ và vẻ kín đáo cổ truyền. Ông loại bỏ các chi tiết bồng vai hay khoét cổ quá sâu của Lemur, nhưng giữ nguyên đường cắt ôm sát hông eo thon thả. Sự dung hòa này giúp áo dài Lê Phổ nhận được sự đồng thuận rộng rãi từ mọi tầng lớp xã hội, biến áo dài thành trang phục thanh lịch chính thức của phụ nữ đô thị Việt Nam." },
          new { type = "paragraph", content = "Những quý cô phố Phái Hà Nội xưa trong tà áo dài Lê Phổ kết hợp với tóc vấn cao, khuyên tai hột xoàn và guốc cao gót thanh mảnh đã trở thành biểu tượng thời trang bất hủ. Sự giao thoa Đông - Tây lúc này không làm mất đi bản sắc Việt mà ngược lại nâng tầm vẻ quyến rũ Á Đông lên một tầm cao mới." },
          
          new { type = "heading", level = 2, content = "5. Raglan và áo dài chít eo thập niên 1960: Đỉnh cao kỹ thuật may đo tôn dáng" },
          new { type = "paragraph", content = "Thập niên 1960 ghi nhận đỉnh cao chói lọi trong kỹ nghệ may đo áo dài tại Sài Gòn. Nhà may Dung ở Đa Kao đã sáng tạo ra chiếc áo dài Raglan, hay còn gọi là ráp lăng, với kỹ thuật nối tay áo xéo từ cổ xuống nách. Cải tiến vĩ đại này loại bỏ hoàn toàn các nếp gấp nhăn nheo, đùn vải dưới cánh tay của các phom áo cũ, tạo nên vùng ngực áo phẳng phiu hoàn mỹ. Đi liền với đó là trào lưu chít eo sâu quyến rũ của các minh tinh màn ảnh và quần lụa ống rộng phủ gót chân kiêu sa. Sự kết hợp này phô diễn trọn vẹn đường cong chữ S nóng bỏng nhưng vẫn giữ được phong thái tao nhã kiêu kỳ vốn có của người phụ nữ Việt Nam." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-truyen-thong-1.webp", true), alt = "Áo dài Raglan chít eo Sài Gòn xưa sang trọng", caption = "Áo dài ráp Raglan chít eo sâu - Biểu tượng gợi cảm đỉnh cao của thời trang Việt Nam thập niên 1960." },
          new { type = "paragraph", content = "Hình ảnh các quý cô Sài Gòn xưa đeo kính râm bản lớn, tóc bồng cao kiêu kỳ thả dáng thướt tha trong tà áo dài chít eo Raglan trên đường phố tựa như một biểu tượng văn hóa bất hủ của vẻ đẹp hiện đại, thanh lịch và quý phái. Kỹ thuật Raglan từ đó đến nay vẫn là tiêu chuẩn may đo áo dài truyền thống đỉnh cao nhất mà các nhà may thời trang cao cấp áp dụng." },
          new { type = "paragraph", content = "Nhờ có ráp vai Raglan, tà áo dài có thể ôm khít sườn ngực mà không hề cản trở cử động cánh tay của người mặc. Đây được coi là phát minh mang tính cách mạng cho trang phục Việt, đưa áo dài gia nhập hàng ngũ những y phục ôm sát quyến rũ nhất thế giới nhưng vẫn đảm bảo sự kín kẽ thanh cao." },
          
          new { type = "quote", content = "Chiếc áo dài Việt Nam mang một sức sống kỳ diệu. Nó không bao giờ đứng im thụ động mà luôn biến đổi nhịp nhàng theo hơi thở thời đại, tự làm mới mình nhưng vẫn giữ nguyên vẹn hồn cốt thanh cao và sự tự tôn dân tộc Việt.", attribution = "Nhà thiết kế Uyên Nguyễn - Sáng lập Áo Dài Nhà Uyên" },
          
          new { type = "heading", level = 2, content = "6. Sự trỗi dậy của Áo Dài Cách Tân trong kỷ nguyên hiện đại" },
          new { type = "paragraph", content = "Vào những năm đầu thế kỷ 21, nhịp sống bận rộn hối hả đòi hỏi trang phục phải năng động và tiện dụng hơn. Áo dài cách tân xuất hiện như một lời giải hoàn hảo. Với tà áo ngắn ngang gối, quần ống hẹp hoặc kết hợp phá cách cùng chân váy xòe, áo dài cách tân mang lại sự thoải mái tối đa cho người mặc trong đời sống công sở hay đi chơi dạo phố ngày thường." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-cach-tan-6.webp", true), alt = "Mẫu áo dài cách tân hiện đại năng động cho giới trẻ", caption = "Áo dài cách tân tà ngắn phối cùng phụ kiện hiện đại mang lại vẻ tươi mới trẻ trung." },
          new { type = "paragraph", content = "Tuy có nhiều ý kiến trái chiều về giới hạn của sự cách tân, không thể phủ nhận rằng áo dài cách tân đã kéo giới trẻ lại gần hơn với trang phục truyền thống. Bằng việc đơn giản hóa các chi tiết rườm rà nhưng giữ lại tinh thần cổ áo đứng và khuy bấm bên hông, áo dài đã tự tin bước vào cuộc sống thường nhật của thế hệ trẻ năng động." },
          
          new { type = "heading", level = 2, content = "7. Giá trị đương đại và biểu tượng văn hóa ngoại giao toàn cầu" },
          new { type = "paragraph", content = "Trải qua ngàn năm lịch sử thăng trầm dâu bể, chiếc áo dài ngày nay đã vượt qua ranh giới của một bộ trang phục thường nhật để trở thành quốc phục chính thức, đại diện cho bản sắc ngoại giao và văn hóa Việt Nam trên vũ đài quốc tế. Tà áo dài xuất hiện trong học đường, văn phòng làm việc đến các sự kiện chính trị tầm cỡ quốc gia, khẳng định vị thế kiêu hãnh bền bỉ cùng thời gian. Gìn giữ và phát triển tà áo dài là cách chúng ta bảo vệ cội nguồn tinh hoa văn hiến ngàn năm của ông cha để lại." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-lua-tron-extra.webp", true), alt = "Áo dài đen trơn lụa tơ tằm cao cấp ngoại giao", caption = "Sự quý phái vượt thời gian của áo dài lụa tơ tằm trơn đen tuyền tối giản." },
          new { type = "paragraph", content = "Mỗi khi đón tiếp các nguyên thủ quốc gia hay đại diện ngoại giao nước ngoài, tà áo dài Việt Nam luôn là sứ giả văn hóa gửi đi thông điệp về một đất nước Việt Nam hòa bình, hiếu khách, đậm đà bản sắc nhưng cũng vô cùng cởi mở hội nhập. Sức mạnh mềm ấy được dệt từ từng sợi tơ lụa mỏng manh." },
          
          new { type = "divider" },
          new { type = "callout", variant = "info", content = "Lời gửi gắm từ Áo Dài Nhà Uyên: Tại xưởng may của chúng tôi, mỗi tà áo dài không chỉ là một món hàng thương mại mà là một tác phẩm chứa đựng niềm đam mê bảo tồn di sản. Chúng tôi dệt may bằng lụa tơ tằm tự nhiên 100%, kết hợp kỹ thuật cắt rập chít eo Raglan chuẩn xác và thêu tay thủ công tinh xảo của các nghệ nhân làng nghề truyền thống để mang đến cho khách hàng những sản phẩm chất lượng nhất." },
          new { type = "callout", variant = "warning", content = "Lưu ý về bản quyền di sản: Các tư liệu lịch sử và hình ảnh phục dựng trong bài viết thuộc sở hữu trí tuệ của Áo Dài Nhà Uyên và các đối tác khảo cổ học. Nghiêm cấm mọi hành vi sao chép, trích dẫn thiếu nguồn phục vụ cho mục đích thương mại phi pháp." }
        }),
        Tags = Json(new[] { "áo dài truyền thống", "lịch sử áo dài", "di sản Việt Nam", "văn hóa đọc", "kiến thức thời trang", "cổ phục Việt" }),
        AuthorNameOverride = "Nhà Uyên Editorial",
        AuthorBio = "Ban biên tập chuyên đề văn hóa và thời trang Việt Nam tại Áo Dài Nhà Uyên, chuyên nghiên cứu sâu sắc về di sản trang phục cổ xưa, kỹ thuật dệt lụa và các phong cách áo dài qua các thời kỳ lịch sử.",
        ReviewedBy = "Uyên Nguyễn",
        Status = BlogPostStatus.Published,
        PublishedAt = now.AddDays(-12),
        MetaTitle = "Lịch sử Áo Dài Việt Nam qua các triều đại | Áo Dài Nhà Uyên",
        MetaDescription = "Khám phá chi tiết lịch sử áo dài Việt Nam từ áo Giao Lĩnh cổ xưa đến áo dài Raglan chít eo quyến rũ thế kỷ 20 và vị thế quốc phục đương đại.",
        CreatedAt = now.AddDays(-12),
        UpdatedAt = now.AddDays(-12)
      },
      new()
      {
        Title = "Bộ sưu tập Áo Dài Xuân Hè 2025: Nàng Thơ Xứ Huế Cổ Kính",
        Slug = "bo-suu-tap-ao-dai-xuan-he-2025",
        Excerpt = "Chiêm ngưỡng trọn vẹn bộ ảnh lookbook nghệ thuật của bộ sưu tập Áo Dài Xuân Hè 2025 mang tên 'Nàng Thơ Xứ Huế'. Nơi nét đẹp đằm thắm dịu dàng của lụa tơ tằm tự nhiên hòa quyện tinh tế với rêu phong cổ kính của cố đô Huế.",
        FeaturedImage = await BlogImageUrlAsync("ao-dai-cach-tan-1.webp", true),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.PhotoGallery,
        BlogCategoryId = blogCategoryBySlug["lookbook-xu-huong"],
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Khúc giao hòa lãng mạn giữa kiến trúc cố cung và tơ lụa" },
          new { type = "paragraph", content = "Bộ sưu tập Xuân Hè 2025 của Áo Dài Nhà Uyên với tên gọi 'Nàng Thơ Xứ Huế' là bức họa đầy chất thơ tôn vinh vẻ đẹp trầm mặc của kinh thành cố kính. Ekip thiết kế đã dành nhiều tháng nghiên cứu các họa tiết cung đình Huế kết hợp với chất liệu lụa tơ tằm tự nhiên cao cấp, dệt thủ công tại làng nghề truyền thống. Nhẹ nhàng, thoáng mát, bay bổng chính là tinh thần cốt lõi mà bộ sưu tập muốn gửi gắm đến giới mộ điệu thời trang áo dài." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-cach-tan-5.webp", true), alt = "Thiết kế áo dài đen thêu chỉ vàng quý phái", caption = "Phom dáng hiện đại kết hợp họa tiết thêu hoàng gia nổi bật bên tường gạch cổ rêu phong." },
          new { type = "paragraph", content = "Chúng tôi chọn Huế làm cái nôi cảm hứng bởi nơi đây lưu giữ trọn vẹn những hoài niệm vàng son của triều đại phong kiến cuối cùng. Nắng chiều xiên qua những mái ngói âm dương cổ kính, phản chiếu lên thớ vải lụa bóng mịn tạo nên hiệu ứng thị giác mê hoặc lòng người. Từng thiết kế trong bộ sưu tập là sự nâng niu bản sắc cổ kính đan xen nét hiện đại phóng khoáng của cuộc sống đương đại." },
          new { type = "paragraph", content = "Sự tương phản giữa bức tường thành rêu phong xám xịt mục nát theo thời gian và tà áo dài rực rỡ óng ánh tơ lụa tạo nên một sức hút thẩm mỹ kỳ lạ. Người mặc áo dài tựa như một đóa hoa bừng nở giữa lòng di sản cổ kính." },
          
          new { type = "heading", level = 2, content = "Thư viện ảnh Lookbook: Nàng thơ dạo chơi giữa cung điện hoàng gia" },
          new { type = "gallery", images = new object[]
            {
              new { src = await BlogImageUrlAsync("ao-dai-cach-tan-3.webp", true), alt = "Người mẫu trình diễn áo dài cách tân màu xanh ngọc bích", caption = "Áo dài cách tân màu xanh ngọc bích với phom dáng ôm tôn dáng hoàn hảo." },
              new { src = await BlogImageUrlAsync("ao-dai-lua-tron-1.webp", true), alt = "Áo dài lụa trơn màu hồng phấn nguyên bản", caption = "Chất liệu lụa tơ tằm trơn màu hồng phấn nguyên bản, mềm mướt bay bổng dưới nắng." },
              new { src = await BlogImageUrlAsync("ao-dai-theu-hoa-1.webp", true), alt = "Chi tiết thêu hoa mai trên áo dài đỏ đô", caption = "Đường nét thêu tay thủ công tỉ mỉ từng sợi chỉ kim tuyến lấp lánh." },
              new { src = await BlogImageUrlAsync("ao-dai-lua-tron-extra.webp", true), alt = "Áo dài lụa trơn đen huyền bí", caption = "Sắc đen tuyền quý phái phối cùng vòng ngọc trai truyền thống sang trọng." },
              new { src = await BlogImageUrlAsync("ao-dai-truyen-thong-6.webp", true), alt = "Mẫu áo dài hoa mai thêu nổi cao cấp", caption = "Họa tiết hoa mai thêu nổi 3D tinh xảo trên ngực áo quyến rũ." },
              new { src = await BlogImageUrlAsync("ao-dai-theu-hoa-6.webp", true), alt = "Cận cảnh họa tiết thêu sen hồng thanh khiết", caption = "Họa tiết đóa sen hồng bung nở mang ý nghĩa an lành và thuần khiết." },
              new { src = await BlogImageUrlAsync("ao-dai-cach-tan-4.webp", true), alt = "Mẫu cách tân vai bồng nhã nhặn", caption = "Tay áo bồng nhẹ phối ren thanh thoát giúp cô gái trẻ trung năng động." },
              new { src = await BlogImageUrlAsync("ao-dai-lua-tron-2.webp", true), alt = "Áo dài lụa trơn sắc xanh bạc hà", caption = "Màu xanh pastel dịu mắt xua tan nắng nóng mùa hè rực rỡ." },
              new { src = await BlogImageUrlAsync("ao-dai-lua-tron-3.webp", true), alt = "Áo dài tơ sen thướt tha mềm mượt", caption = "Dòng lụa tơ sen nhẹ tênh ôm ấp làn da vô cùng thân thiện." },
              new { src = await BlogImageUrlAsync("ao-dai-theu-hoa-3.webp", true), alt = "Áo dài thêu cúc họa mi tinh khôi", caption = "Những cánh cúc thêu nổi trắng ngần gợi vẻ trong sáng ngây thơ." }
            }
          },
          
          new { type = "heading", level = 2, content = "Đặc trưng nghệ thuật thêu thủ công 3D đỉnh cao" },
          new { type = "paragraph", content = "Điểm nhấn đắt giá nhất của bộ sưu tập lần này nằm ở công nghệ thêu nổi 3D độc quyền từ bàn tay nghệ nhân Nhà Uyên. Từng cành mai mảnh khảnh, cánh hoa đào mỏng manh hay chiếc lá sen đọng sương đều hiện lên sinh động, có chiều sâu ấn tượng trên bề mặt lụa mướt mịn. Chúng tôi tin rằng thời trang cao cấp không chỉ nằm ở phom dáng tôn lên đường cong của người mặc mà còn thể hiện ở mức độ tinh tế của các chi tiết nhỏ nhất." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-theu-hoa-5.webp", true), alt = "Cận cảnh họa tiết thêu nổi trên tà áo dài", caption = "Độ nổi khối và chuyển màu uyển chuyển của cành mai thêu tay truyền thống." },
          new { type = "paragraph", content = "Mỗi sản phẩm thêu tay tiêu tốn của người nghệ nhân từ 40 đến 80 giờ làm việc liên tục. Sự kiên nhẫn và đam mê đổ dồn vào từng mũi kim chính là giá trị vô hình tạo nên đẳng cấp của Áo Dài Nhà Uyên. Chúng tôi từ chối thêu máy hàng loạt để bảo tồn vẻ đẹp độc bản và cá tính riêng cho mỗi người sở hữu. Sợi chỉ được nhuộm hữu cơ thực vật mang đến dải phổ màu tự nhiên có độ sâu vượt trội so với sợi chỉ polyester công nghiệp." },
          new { type = "image", src = await BlogImageUrlAsync("phu-kien-tu-xach-theu-hoa.webp", true), alt = "Túi xách thêu sen hồng đồng bộ thời trang", caption = "Sự phối hợp hài hòa giữa áo dài thêu tay và chiếc túi xách thêu đồng concept sang quý." },
          new { type = "paragraph", content = "Ngoài ra, bộ sưu tập còn kết hợp chặt chẽ với các phụ kiện thiết kế độc bản như quạt giấy vẽ tay vân mây và guốc gỗ mộc sơn mài. Cách phối đồ này gợi nhắc phong thái của các bậc vương phi triều Nguyễn nhưng vẫn vô cùng nhẹ nhàng, dễ mặc trong các sự kiện hiện đại của thế kỷ 21. Sự phối hợp đồng điệu từ trang phục tới phụ kiện chính là đỉnh cao của thời trang di sản." },
          new { type = "image", src = await BlogImageUrlAsync("tram-cai-toc-hong-ngoc-bich.webp", true), alt = "Trâm cài tóc ngọc bích quý phái cổ điển", caption = "Trâm cài ngọc bích chế tác thủ công tôn thêm vẻ đài các của nàng thơ xứ Huế." },
          
          new { type = "quote", content = "Khi mặc tà áo dài lụa thêu tay, bạn không chỉ khoác lên mình một tấm vải, bạn đang khoác lên mình tinh hoa văn hóa, sức lao động nghệ thuật miệt mài của người nghệ nhân Việt.", attribution = "Stylist Minh Trí" },
          new { type = "paragraph", content = "Bộ sưu tập mang đến những gợi ý lý tưởng cho trang phục đi tiệc sang trọng, chụp hình kỷ niệm hay tham gia các hoạt động văn hóa nghệ thuật lớn. Hãy chọn cho mình thiết kế ưng ý để trở thành tâm điểm của mọi ánh nhìn và mang theo hơi ấm tâm hồn Việt." }
        }),
        Tags = Json(new[] { "lookbook thời trang", "xu hướng xuân hè", "áo dài cách tân", "nàng thơ xứ Huế", "áo dài lụa tơ tằm", "lookbook ảnh nghệ thuật" }),
        AuthorNameOverride = "Nhà Uyên Editorial",
        AuthorBio = "Đội ngũ nhiếp ảnh gia và cố vấn nghệ thuật thời trang tại Áo Dài Nhà Uyên, tiên phong trong các xu hướng lookbook áo dài mang đậm chiều sâu di sản.",
        ReviewedBy = "Uyên Nguyễn",
        Status = BlogPostStatus.Published,
        PublishedAt = now.AddDays(-8),
        MetaTitle = "BST Áo Dài Xuân Hè 2025: Nàng Thơ Xứ Huế | Lookbook Ảnh Đẹp",
        MetaDescription = "Chiêm ngưỡng bộ sưu tập ảnh áo dài xuân hè năm nay với các thiết kế cách tân xanh ngọc, lụa trơn hồng phấn thêu nổi 3D sang trọng tại kinh thành Huế.",
        CreatedAt = now.AddDays(-8),
        UpdatedAt = now.AddDays(-8)
      },
      new()
      {
        Title = "Áo Dài cưới gấm đỏ thêu tay hoàng gia - Lựa chọn trọn vẹn cho lễ Vu Quy",
        Slug = "ao-dai-cuoi-lua-chon-hoan-hao-cho-ngay-trong-dai",
        Excerpt = "Khám phá các thiết kế áo dài cưới màu đỏ gấm thượng hạng dệt chỉ vàng nổi bật, sự kết hợp hoàn hảo giữa phom dáng cổ điển quyến rũ và nét hiện đại sang quý cho ngày trọng đại.",
        FeaturedImage = await BlogImageUrlAsync("ao-dai-theu-hoa-5.webp", true),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.ProductSpotlight,
        BlogCategoryId = blogCategoryBySlug["ao-dai-cuoi"],
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Ý nghĩa tâm linh của sắc đỏ gấm hoa trong hôn lễ Việt" },
          new { type = "paragraph", content = "Trong ngày đại hỷ của người Việt, sắc đỏ luôn được chọn làm tông màu chủ đạo cho trang phục cưới hỏi của tân lang tân nương. Màu đỏ tượng trưng cho sự may mắn, cát tường, khởi đầu suôn sẻ ấm áp và biểu thị lời chúc cho một tình yêu nồng cháy bền chặt cùng cuộc sống gia đình ấm cúng viên mãn sau này. Hiểu được giá trị sâu sắc đó, dòng sản phẩm Áo Dài Cưới Gấm Đỏ của Nhà Uyên ra đời để giúp các nàng dâu tỏa sáng rạng ngời nhất trong ngày lễ gia tiên thiêng liêng." },
          new { type = "paragraph", content = "So với váy cưới phương Tây, chiếc áo dài cưới gấm đỏ mang đậm hơi thở gia tộc, thể hiện lòng tôn kính sâu sắc với tổ tiên khi làm lễ thắp hương trước bàn thờ gia tiên. Từng thớ vải gấm dệt hoa văn chữ Song Hỷ hay đôi chim phượng hoàng lấp lánh kể câu chuyện về sự thủy chung, hạnh phúc bền lâu của lứa đôi." },
          
          new { type = "product_spotlight", productSlugs = productSlugs.Take(4).ToArray() },
          
          new { type = "heading", level = 2, content = "Phom dáng chít eo sâu Raglan kế thừa di sản Sài Gòn" },
          new { type = "paragraph", content = "Một chiếc áo dài cưới hoàn hảo không chỉ cần lộng lẫy mà phải vừa vặn tuyệt đối với cơ thể của cô dâu. Đội ngũ thợ may đo lâu năm của Nhà Uyên ứng dụng kỹ thuật cắt cúp chít eo sâu kế thừa từ thời kỳ hoàng kim Raglan của Sài Gòn thập niên 1960, kết hợp với chất liệu gấm dệt hoa văn chìm có độ co giãn nhẹ và đứng dáng. Điều này giúp nâng đỡ vòng một, thắt chặt vòng hai và giữ tà áo luôn phẳng phiu, sang trọng suốt nhiều giờ làm lễ chụp ảnh cưới liên tục." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-theu-hoa-6.webp", true), alt = "Áo dài cưới gấm đỏ thêu hoa long phụng cô dâu chú rể", caption = "Cặp đôi rạng rỡ trong tà áo dài cưới gấm đỏ thêu long phụng chỉ vàng tinh xảo của thương hiệu Nhà Uyên." },
          new { type = "paragraph", content = "Chất liệu gấm tơ tằm dệt jacquard cao cấp mang lại cảm giác mềm mại với làn da, không gây ngứa rát hay khó chịu dù thời tiết oi bức và thời gian mặc kéo dài cả ngày lễ Vu Quy. Với phom dáng may sẵn nhưng được tinh chỉnh riêng theo số đo chiều cao và cân nặng từng cô dâu, tà áo dài cưới Nhà Uyên mang đến trải nghiệm thời trang may đo xa xỉ ngay trong tầm tay bạn." },
          new { type = "paragraph", content = "Quy trình thử áo dài cưới tại showroom được tiến hành chu đáo: cô dâu được hỗ trợ tinh chỉnh vai, nách và gấu quần lụa để khi di chuyển đón khách hay quỳ lạy trước bàn thờ đều vô cùng tự tin, tôn lên vóc dáng thanh xuân mảnh mai đầy kiêu sa của mình." },
          
          new { type = "heading", level = 2, content = "Họa tiết thêu long phụng song hỷ đính đá quý xà cừ" },
          new { type = "paragraph", content = "Mỗi chiếc áo dài cưới thuộc dòng cao cấp của chúng tôi đều được đắp nổi họa tiết long phụng sum vầy mang tính biểu tượng phu thê hòa hợp. Nghiêm cấm sự cẩu thả, nghệ nhân thêu sử dụng chỉ tơ tằm dát vàng lấp lánh, đan xen đính kết đá quý xà cừ nhập khẩu thủ công trên cổ áo và tà trước. Hiệu ứng phản chiếu lung linh giúp cô dâu trông cực kỳ lộng lẫy dưới ánh đèn khán phòng tiệc cưới tối." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-theu-hoa-1.webp", true), alt = "Chi tiết thêu hoa mẫu đơn thêu tay chỉ vàng", caption = "Đóa hoa mẫu đơn thêu chỉ vàng rực rỡ tượng trưng cho sự phú quý giàu sang và thịnh vượng." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-theu-hoa-2.webp", true), alt = "Mẫu áo dài cưới đỏ thêu tay hoa đào tươi sáng", caption = "Mẫu thêu cành đào rạng rỡ thêu nổi bồng bềnh mang nét trẻ trung rực lửa." },
          new { type = "paragraph", content = "Đặc biệt, dòng gấm đỏ cưới này được phối cùng các phụ kiện túi xách tơ thêu hoa sen đồng điệu và đôi guốc thắt nơ nhung đỏ thanh tao. Cô dâu sẽ có một diện mạo hoàn hảo nhất từ đầu tới chân mà không cần tốn thời gian tìm kiếm phụ kiện bên ngoài." },
          new { type = "image", src = await BlogImageUrlAsync("phu-kien-tu-xach-theu-hoa.webp", true), alt = "Túi xách cưới thêu tay cao cấp cho cô dâu", caption = "Túi xách thêu sen đỏ quý phái chứa đựng những món đồ trang điểm nhỏ xinh cho cô dâu trong ngày đại hỷ." },
          new { type = "image", src = await BlogImageUrlAsync("guoc-cao-got-no.webp", true), alt = "Guốc nhung đỏ thắt nơ tinh tế", caption = "Đôi guốc nhung đỏ đính nơ điệu đà nâng đỡ gót hồng kiêu sa cho cô dâu trong ngày Vu Quy." },
          new { type = "paragraph", content = "Để chuẩn bị chu đáo nhất, cô dâu chú rể nên tiến hành may đo sớm từ 4 đến 6 tuần trước lễ cưới. Áo Dài Nhà Uyên cung cấp dịch vụ thử áo chi tiết và chỉnh sửa phom dáng miễn phí đến khi cô dâu đạt được sự hài lòng tối đa. Hãy liên hệ hotline hoặc đặt lịch hẹn showroom ngay hôm nay để nhận được sự tư vấn tận tâm nhất từ các chuyên gia phục trang cưới." }
        }),
        Tags = Json(new[] { "áo dài cưới đỏ", "gấm cưới cao cấp", "trang phục lễ gia tiên", "cô dâu thanh lịch", "sản phẩm gợi ý", "áo dài cô dâu" }),
        AuthorNameOverride = "Nhà Uyên Bridal Team",
        AuthorBio = "Đội ngũ chuyên gia thiết kế và tư vấn trang phục cưới cao cấp tại Áo Dài Nhà Uyên, đồng hành cùng hàng ngàn cô dâu Việt tỏa sáng trong ngày trọng đại.",
        ReviewedBy = "Uyên Nguyễn",
        Status = BlogPostStatus.Published,
        PublishedAt = now.AddDays(-4),
        MetaTitle = "Áo Dài cưới gấm đỏ thêu tay hoàng gia cao cấp | Nhà Uyên",
        MetaDescription = "Khám phá các mẫu áo dài cưới màu đỏ gấm thượng hạng thêu tay hoa mai, song hỷ cát tường cực sang trọng dành cho cô dâu chú rể ngày vu quy.",
        CreatedAt = now.AddDays(-4),
        UpdatedAt = now.AddDays(-4)
      },
      new()
      {
        Title = "Cẩm nang đo size chuẩn tại nhà và bảo quản Áo Dài Lụa luôn như mới",
        Slug = "cach-chon-va-bao-quan-ao-dai-dung-cach",
        Excerpt = "Hướng dẫn chi tiết từng bước tự lấy số đo vòng ngực, eo, mông chuẩn xác và quy trình giặt hấp, ủi hơi nước đúng cách bảo vệ vải lụa tơ tằm đắt giá lâu bền không xước.",
        FeaturedImage = await BlogImageUrlAsync("ao-dai-lua-tron-extra.webp", true),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.HowTo,
        BlogCategoryId = blogCategoryBySlug["huong-dan-cham-soc"],
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Quy trình chăm sóc tà áo dài đúng kỹ thuật để kéo dài tuổi thọ vải" },
          new { type = "paragraph", content = "Sở hữu một chiếc áo dài từ lụa tơ tằm tự nhiên hay gấm thêu nổi quý phái là niềm kiêu hãnh lớn của mỗi người phụ nữ. Tuy nhiên, chất liệu tơ lụa tự nhiên rất nhạy cảm và dễ bị tổn thương nếu giặt ủi sai cách. Cẩm nang này được biên soạn bởi các chuyên gia dệt may hàng đầu tại Nhà Uyên nhằm giúp quý khách có quy trình khoa học để tự đo size chuẩn và gìn giữ tà áo luôn bền đẹp bóng bẩy theo thời gian." },
          
          new { type = "step", stepNumber = 1, title = "Tự lấy số đo cơ thể chuẩn xác từng centimet tại nhà", content = "Dùng một thước dây mềm. Khi đo vòng một, quấn thước quanh phần nở nhất của ngực. Vòng hai đo tại điểm thắt nhỏ nhất của eo, thường cách phía trên rốn khoảng 3cm. Vòng ba đo tại điểm lớn nhất của mông. Đặc biệt là chiều dài áo, đo từ đỉnh vai xuôi qua đỉnh ngực xuống thẳng gót chân. Nên mặc chiếc nội y có đệm phom chuẩn nhất mà bạn dự định mặc cùng áo dài khi tiến hành đo.", tip = "Tránh kéo quá chặt hoặc để thước quá lỏng, nên hít thở bình thường khi lấy số đo eo." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-lua-tron-extra.webp", true), alt = "Quy trình thử size áo dài tại xưởng may Nhà Uyên", caption = "Việc thử và điều chỉnh áo dài theo sát số đo cơ thể giúp tôn dáng quyến rũ tối đa." },
          
          new { type = "step", stepNumber = 2, title = "Lựa chọn phom cổ áo dài tôn vinh nét hài hòa của khuôn mặt", content = "Với người có vóc dáng đầy đặn và phần cổ ngắn, hãy tránh xa các mẫu cổ cao 4-5cm. Thay vào đó, thiết kế cổ tròn viền nhỏ, cổ thuyền nhẹ nhàng hoặc cổ chữ V thanh thoát sẽ tạo cảm giác thanh thoát cao ráo hơn. Người mảnh khảnh, cổ cao kiêu sa sẽ cực kỳ phù hợp với phom áo dài cổ cao truyền thống đứng 2.5cm đến 3cm đính cườm tỉ mỉ." },
          
          new { type = "step", stepNumber = 3, title = "Giặt tay nhẹ nhàng bằng dầu gội hoặc sữa tắm trẻ em", content = "Vải lụa tơ tằm tự nhiên chứa nhiều protein tương tự tóc người nên sẽ bị khô xơ và phai màu nhanh nếu tiếp xúc với chất tẩy có tính kiềm cao trong bột giặt thường. Hãy pha một muỗng sữa tắm trẻ em vào chậu nước mát dưới 30 độ C. Ngâm áo dài 5 phút, sau đó dùng tay bóp nhẹ để làm sạch bụi bẩn. Tuyệt đối không giặt máy, vắt xoắn hoặc vò chà mạnh vùng thêu tay đính đá." },
          new { type = "callout", variant = "tip", content = "Bí quyết vàng giữ màu lụa: Ở lần xả nước cuối cùng, hãy nhỏ thêm 2-3 thìa giấm trắng loãng. Axit nhẹ trong giấm sẽ khóa hạt màu nhuộm tự nhiên trên sợi tơ lụa, giúp màu vải luôn rực rỡ óng ả và hạn chế hiện tượng ra màu khi giặt những lần sau." },
          
          new { type = "step", stepNumber = 4, title = "Phơi áo dài trong bóng râm, tránh ánh nắng trực tiếp", content = "Nhiệt độ cao của ánh nắng mặt trời gắt sẽ phá hủy các thớ sợi tơ tằm tự nhiên làm vải bị khô giòn, xơ xác và dễ rách sau vài lần mặc. Hãy lộn trái tà áo trước khi phơi, đặt áo lên chiếc móc gỗ chuyên dụng bản dày. Nên tránh dùng móc sắt nhọn để hạn chế làm xước vải và treo phơi ở nơi râm mát đón nhiều gió tự nhiên." },
          new { type = "image", src = await BlogImageUrlAsync("phu-kien-quat-hoa-sen.webp", true), alt = "Bảo quản phụ kiện áo dài cẩn thận", caption = "Các phụ kiện sắc nhọn như quạt, trâm cài tóc cần cất riêng để tránh vướng xước tơ lụa." },
          new { type = "image", src = await BlogImageUrlAsync("phu-kien-tu-xach-thanh.webp", true), alt = "Túi xách lụa phối cùng áo dài trơn", caption = "Chiếc túi xách thanh nhã phối hợp tôn nét tối giản của lụa trơn." },
          
          new { type = "step", stepNumber = 5, title = "Ủi hơi nước ở nhiệt độ thích hợp cho tơ lụa", content = "Phương pháp lý tưởng nhất là sử dụng bàn ủi hơi nước dạng đứng để ủi khi áo dài đang treo thẳng đứng. Nếu sử dụng bàn ủi nhiệt nằm thông thường, hãy đảm bảo áo dài còn độ ẩm nhẹ, hoặc lộn trái áo và lót một tấm vải cotton mỏng sạch lên trên trước khi ủi. Luôn vặn nút chỉnh nhiệt độ về mức dành riêng cho vải Silk hoặc mức thấp nhất." },
          
          new { type = "step", stepNumber = 6, title = "Cất giữ và lưu kho lâu dài trong túi vải bạt thở khí", content = "Khi không sử dụng áo dài trong thời gian dài như sau các mùa lễ tết, tuyệt đối không để áo dài trong các túi nilon kín mít vì túi nilon giữ độ ẩm làm mốc vải và làm biến tính sợi tơ tằm tự nhiên. Hãy xếp nhẹ nhàng áo dài vào các túi vải không dệt thoáng khí, đặt kèm một thanh gỗ tuyết tùng hoặc túi thảo mộc thơm nhẹ để xua đuổi côn trùng gặm nhấm." },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-lua-tron-4.webp", true), alt = "Áo dài lụa xếp gọn trong tủ đồ bảo quản", caption = "Xếp nếp nhẹ nhàng và treo thẳng đứng trong tủ đồ có độ thông thoáng gió tốt." },
          new { type = "image", src = await BlogImageUrlAsync("phu-kien-tu-xach-hoa-van.webp", true), alt = "Cất giữ túi xách đi kèm áo dài", caption = "Bảo quản túi xách tơ thêu hoa trong hộp giấy cứng hút ẩm để giữ phom dáng bền lâu." }
        }),
        Tags = Json(new[] { "hướng dẫn bảo quản", "mẹo giặt tơ lụa", "cách chọn size", "kinh nghiệm đo áo dài", "bảo quản gấm", "tự may đo tại nhà" }),
        AuthorNameOverride = "Nhà Uyên Care Team",
        AuthorBio = "Đội ngũ chuyên viên kỹ thuật dệt may và chăm sóc khách hàng tại Áo Dài Nhà Uyên, chuyên tư vấn bảo dưỡng các sản phẩm lụa gấm cao cấp.",
        ReviewedBy = "Uyên Nguyễn",
        Status = BlogPostStatus.Published,
        PublishedAt = now.AddDays(-2),
        CreatedAt = now.AddDays(-2),
        UpdatedAt = now.AddDays(-2)
      }
    };

    dbContext.BlogPosts.AddRange(posts);
    await dbContext.SaveChangesAsync();
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
    var adminEmail = adminSeedOptions.Value.Email?.Trim();
    var adminPassword = adminSeedOptions.Value.Password?.Trim();

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
    var existingCount = await dbContext.Users
      .CountAsync(u => u.UserRoles.Any(r => r.RoleId == customerRole.Id));
    if (existingCount >= 100) return;

    var rng = new Random(42); // Deterministic seed
    var generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // First, ensure the 3 named DefaultCustomers exist
    foreach (var item in DefaultCustomers.Items)
    {
      generated.Add(item.Email);
      await UpsertCustomerAsync(item.FullName, item.Email, item.Phone, item.Gender, item.Password, customerRole);
    }

    // Generate additional customers to reach ~100
    var targetCount = 100;
    var attempts = 0;
    while (generated.Count < targetCount && attempts < 200)
    {
      attempts++;
      var isFemale = rng.Next(2) == 0;
      var gender = isFemale ? "female" : "male";

      var family = DemoFamilyNames[rng.Next(DemoFamilyNames.Length)];
      var middle = isFemale
        ? DemoMiddleNamesFemale[rng.Next(DemoMiddleNamesFemale.Length)]
        : DemoMiddleNamesMale[rng.Next(DemoMiddleNamesMale.Length)];
      var given = isFemale
        ? DemoFemaleGivenNames[rng.Next(DemoFemaleGivenNames.Length)]
        : DemoMaleGivenNames[rng.Next(DemoMaleGivenNames.Length)];

      var fullName = $"{family} {middle} {given}";
      var emailBase = $"{RemoveDiacritics(given).ToLowerInvariant()}.{RemoveDiacritics(family).ToLowerInvariant()}";
      var email = $"{emailBase}{rng.Next(100, 999)}@example.com";

      if (!generated.Add(email)) continue;

      var phone = $"09{rng.Next(10, 99):D2}{rng.Next(100000, 999999)}";
      await UpsertCustomerAsync(fullName, email, phone, gender, DemoPassword, customerRole);
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task UpsertCustomerAsync(
    string fullName, string email, string phone, string gender, string password, Role customerRole)
  {
    var user = await dbContext.Users
      .Include(x => x.UserRoles)
      .FirstOrDefaultAsync(x => x.Email == email);

    if (user is null)
    {
      user = new User
      {
        FullName = fullName,
        Email = email,
        Phone = phone,
        Gender = gender,
        Status = "active",
        EmailVerifiedAt = DateTime.UtcNow,
        PhoneVerifiedAt = DateTime.UtcNow
      };
      dbContext.Users.Add(user);
    }
    else
    {
      user.FullName = fullName;
      user.Phone = phone;
      user.Gender = gender;
      user.Status = "active";
      user.EmailVerifiedAt ??= DateTime.UtcNow;
      user.PhoneVerifiedAt ??= DateTime.UtcNow;
      user.UpdatedAt = DateTime.UtcNow;
    }

    if (!user.UserRoles.Any(x => x.RoleId == customerRole.Id))
      user.UserRoles.Add(new UserRole { User = user, RoleId = customerRole.Id });

    var normalizedEmail = email.Trim().ToLowerInvariant();
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
        PasswordHash = passwordHasher.HashPassword(password),
        IsVerified = true
      });
    }
    else
    {
      credentialsAccount.PasswordHash = passwordHasher.HashPassword(password);
      credentialsAccount.IsVerified = true;
      credentialsAccount.UpdatedAt = DateTime.UtcNow;
    }
  }

  private static string RemoveDiacritics(string text)
  {
    var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
    var sb = new System.Text.StringBuilder();
    foreach (var c in normalized)
    {
      if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
          != System.Globalization.UnicodeCategory.NonSpacingMark)
        sb.Append(c);
    }
    return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
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

    var customerRole = await dbContext.Roles.FirstAsync(x => x.Name == "customer");
    var customers = await dbContext.Users
      .Where(u => u.UserRoles.Any(r => r.RoleId == customerRole.Id))
      .Take(30)
      .ToListAsync();
    if (customers.Count == 0) return;

    var variants = await dbContext.ProductVariants
      .Include(v => v.Product)
      .Where(v => v.Product != null && !v.Product.IsDeleted)
      .ToListAsync();
    if (variants.Count == 0) return;

    var now = DateTime.UtcNow;
    var rng = new Random(123);

    // 30 orders: distributed across statuses and 30-day history
    var statusDistribution = new[]
    {
      new { Status = "completed",  Count = 8, HasShipment = true,  ShipStatus = "delivered" },
      new { Status = "shipping",   Count = 4, HasShipment = true,  ShipStatus = "shipped" },
      new { Status = "processing", Count = 4, HasShipment = false, ShipStatus = "" },
      new { Status = "confirmed",  Count = 5, HasShipment = false, ShipStatus = "" },
      new { Status = "pending",    Count = 6, HasShipment = false, ShipStatus = "" },
      new { Status = "cancelled",  Count = 3, HasShipment = false, ShipStatus = "" },
    };

    var orderIndex = 0;
    foreach (var bucket in statusDistribution)
    {
      for (var b = 0; b < bucket.Count; b++)
      {
        var customer = customers[rng.Next(customers.Count)];
        var itemCount = rng.Next(1, 4); // 1-3 items per order
        var items = new List<(ProductVariant Variant, int Qty)>();
        var usedVariantIds = new HashSet<Guid>();

        for (var j = 0; j < itemCount; j++)
        {
          var candidate = variants
            .Where(v => !usedVariantIds.Contains(v.Id))
            .OrderBy(_ => rng.Next())
            .FirstOrDefault();
          if (candidate is null) break;

          usedVariantIds.Add(candidate.Id);
          items.Add((candidate, rng.Next(1, 4))); // qty 1-3
        }

        if (items.Count == 0) continue;

        var subtotal = items.Sum(x => (x.Variant.SalePrice ?? x.Variant.Price) * x.Qty);
        var shippingFee = subtotal >= 500000m ? 0m : 25000m;
        var discountAmount = rng.Next(4) == 0 ? Math.Round(subtotal * 0.10m, 0) : 0m;
        var daysAgo = rng.Next(0, 31);
        var placedAt = now.AddDays(-daysAgo).AddHours(-rng.Next(0, 12));

        var addressIndex = rng.Next(DemoAddressTemplates.Length);
        var addr = DemoAddressTemplates[addressIndex];
        var ward = addr.Wards[rng.Next(addr.Wards.Length)];
        var streetNum = rng.Next(1, 300);
        var street = DemoStreetNames[rng.Next(DemoStreetNames.Length)];

        var order = new Order
        {
          OrderCode = $"AD-{placedAt:yyyyMMddHHmmss}{orderIndex:D2}",
          UserId = customer.Id,
          RecipientName = customer.FullName,
          RecipientPhone = customer.Phone ?? "0912345678",
          Province = addr.Province,
          District = addr.District,
          Ward = ward,
          AddressLine = $"{streetNum} {street}",
          Subtotal = subtotal,
          DiscountAmount = discountAmount,
          ShippingFee = shippingFee,
          TotalAmount = subtotal + shippingFee - discountAmount,
          OrderStatus = bucket.Status,
          PlacedAt = placedAt,
          Note = orderIndex % 4 == 0 ? "Chuyển phát giờ hành chính" : null,
          CreatedAt = placedAt,
          UpdatedAt = placedAt
        };

        if (bucket.Status is "confirmed" or "processing" or "shipping" or "completed")
          order.ConfirmedAt = placedAt.AddHours(1);
        if (bucket.Status is "completed")
          order.CompletedAt = placedAt.AddDays(3);
        if (bucket.Status is "cancelled")
          order.CancelledAt = placedAt.AddHours(2);

        foreach (var (variant, qty) in items)
        {
          var unitPrice = variant.SalePrice ?? variant.Price;
          order.Items.Add(new OrderItem
          {
            ProductId = variant.ProductId,
            VariantId = variant.Id,
            ProductName = variant.Product!.Name,
            Sku = variant.Sku,
            Size = variant.Size,
            Color = variant.Color,
            UnitPrice = unitPrice,
            Quantity = qty,
            LineTotal = unitPrice * qty,
            CreatedAt = placedAt
          });
        }

        order.Payment = new Payment
        {
          Amount = order.TotalAmount,
          PaidAt = bucket.Status != "pending" ? placedAt : default,
          Note = bucket.Status != "pending" ? "paid_successfully" : null,
          CreatedAt = placedAt
        };

        if (bucket.HasShipment)
        {
          order.Shipments.Add(new Shipment
          {
            ShippingStatus = bucket.ShipStatus,
            Carrier = rng.Next(3) switch { 0 => "GHN", 1 => "GHTK", _ => "Viettel Post" },
            TrackingNumber = $"SPX{placedAt:yyyyMMdd}{orderIndex:D5}",
            ShippedAt = bucket.ShipStatus is "shipped" or "delivered" ? placedAt.AddHours(6) : null,
            DeliveredAt = bucket.ShipStatus == "delivered" ? placedAt.AddDays(3) : null,
            CreatedAt = placedAt
          });
        }

        dbContext.Orders.Add(order);
        orderIndex++;
      }
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


  private async Task SeedDemoReviewsAsync()
  {
    var hasReviews = await dbContext.Reviews.AnyAsync();
    if (hasReviews) return;

    var customerRole = await dbContext.Roles.FirstAsync(x => x.Name == "customer");
    var customers = await dbContext.Users
      .Where(u => u.UserRoles.Any(r => r.RoleId == customerRole.Id))
      .Take(30)
      .ToListAsync();
    if (customers.Count == 0) return;

    var products = await dbContext.Products
      .Where(p => !p.IsDeleted)
      .Take(20)
      .ToListAsync();
    if (products.Count == 0) return;

    var orders = await dbContext.Orders
      .Include(o => o.Items)
      .Where(o => o.OrderStatus == "completed" || o.OrderStatus == "shipping")
      .Take(15)
      .ToListAsync();

    var now = DateTime.UtcNow;
    var rng = new Random(456);

    // ── 15 Reviews with varied ratings ──
    var reviewCount = 0;
    foreach (var product in products.Take(15))
    {
      var customer = customers[rng.Next(customers.Count)];
      var rating = rng.Next(1, 6); // 1-5 stars

      string? comment = rating switch
      {
        >= 4 => DemoPositiveReviewTexts[rng.Next(DemoPositiveReviewTexts.Length)],
        3 => DemoNeutralReviewTexts[rng.Next(DemoNeutralReviewTexts.Length)],
        _ => DemoNegativeReviewTexts[rng.Next(DemoNegativeReviewTexts.Length)]
      };

      // Try to associate with an order item from a completed order
      var orderItem = orders
        .SelectMany(o => o.Items.Select(i => new { Order = o, Item = i }))
        .FirstOrDefault(x => x.Item.ProductId == product.Id);

      var review = new Review
      {
        UserId = customer.Id,
        ProductId = product.Id,
        OrderItemId = orderItem?.Item.Id,
        Rating = rating,
        Comment = comment,
        IsVisible = true,
        CreatedAt = now.AddDays(-rng.Next(0, 30)),
        UpdatedAt = now.AddDays(-rng.Next(0, 10))
      };

      dbContext.Reviews.Add(review);
      reviewCount++;
    }

    // ── 12 Comments (questions on products) ──
    foreach (var product in products.OrderBy(_ => rng.Next()).Take(8))
    {
      var customer = customers[rng.Next(customers.Count)];

      var comment = new Comment
      {
        UserId = customer.Id,
        ProductId = product.Id,
        Content = DemoQuestionComments[rng.Next(DemoQuestionComments.Length)],
        IsVisible = true,
        CreatedAt = now.AddDays(-rng.Next(0, 20)),
        UpdatedAt = now.AddDays(-rng.Next(0, 5))
      };

      dbContext.Comments.Add(comment);
    }

    // ── 5 Admin replies to reviews ──
    var admin = await dbContext.Users
      .Include(u => u.UserRoles)
      .FirstAsync(u => u.UserRoles.Any(r => r.Role!.Name == "admin"));

    var reviewsForReply = await dbContext.Reviews
      .Where(r => r.Comment != null)
      .Take(5)
      .ToListAsync();

    var replyTemplates = new[]
    {
      "Cảm ơn chị đã đánh giá! Shop rất vui vì chị hài lòng với sản phẩm. Hy vọng được phục vụ chị lần sau ạ.",
      "Dạ shop cảm ơn chị đã góp ý. Bên em sẽ cải thiện chất lượng dịch vụ giao hàng ạ.",
      "Cảm ơn chị đã tin tưởng Áo Dài Nha Uyên. Chị có thể inbox page để được tư vấn chọn size kỹ hơn cho lần sau nhé.",
      "Dạ shop xin lỗi vì trải nghiệm chưa tốt. Bên em sẽ liên hệ hỗ trợ đổi trả cho chị ạ.",
      "Cảm ơn chị đã ủng hộ shop! Chúc chị luôn xinh đẹp trong tà áo dài Việt Nam."
    };

    foreach (var review in reviewsForReply)
    {
      var reply = new Comment
      {
        UserId = admin.Id,
        ProductId = review.ProductId,
        ParentCommentId = null, // Reply to review means standalone comment, or we can use a flag
        Content = replyTemplates[rng.Next(replyTemplates.Length)],
        IsVisible = true,
        CreatedAt = review.CreatedAt.AddHours(rng.Next(1, 24)),
        UpdatedAt = review.CreatedAt.AddHours(rng.Next(1, 24))
      };

      dbContext.Comments.Add(reply);
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedToolRiskConfigsAsync()
  {
    var hasConfigs = await dbContext.ToolRiskConfigs.AnyAsync();
    if (hasConfigs) return;

    var defaults = new List<ToolRiskConfig>
    {
      new() { ToolName = "get_dashboard_summary", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tổng quan dashboard", Category = "Dashboard" },
      new() { ToolName = "get_revenue", RiskLevel = "Read", RequiresConfirmation = false, Description = "Dữ liệu doanh thu", Category = "Dashboard" },
      new() { ToolName = "get_orders_by_status", RiskLevel = "Read", RequiresConfirmation = false, Description = "Phân phối đơn theo trạng thái", Category = "Dashboard" },
      new() { ToolName = "get_recent_orders", RiskLevel = "Read", RequiresConfirmation = false, Description = "Đơn hàng gần đây", Category = "Dashboard" },
      new() { ToolName = "get_top_products", RiskLevel = "Read", RequiresConfirmation = false, Description = "Sản phẩm bán chạy", Category = "Dashboard" },
      new() { ToolName = "list_products", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê sản phẩm", Category = "Products" },
      new() { ToolName = "get_product", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết sản phẩm", Category = "Products" },
      new() { ToolName = "create_product", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo sản phẩm mới (nháp)", Category = "Products" },
      new() { ToolName = "update_product", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật sản phẩm", Category = "Products" },
      new() { ToolName = "delete_product", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm sản phẩm", Category = "Products" },
      new() { ToolName = "toggle_product_status", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Bật/tắt trạng thái sản phẩm", Category = "Products" },
      new() { ToolName = "list_categories", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê danh mục", Category = "Categories" },
      new() { ToolName = "create_category", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo danh mục mới", Category = "Categories" },
      new() { ToolName = "update_category", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật danh mục", Category = "Categories" },
      new() { ToolName = "delete_category", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm danh mục", Category = "Categories" },
      new() { ToolName = "list_users", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê người dùng", Category = "Users" },
      new() { ToolName = "get_user", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết người dùng", Category = "Users" },
      new() { ToolName = "update_user_status", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Bật/tắt trạng thái người dùng", Category = "Users" },
      new() { ToolName = "update_user_role", RiskLevel = "High", RequiresConfirmation = true, Description = "Thay đổi vai trò người dùng", Category = "Users" },
      new() { ToolName = "list_orders", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê đơn hàng", Category = "Orders" },
      new() { ToolName = "get_order", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết đơn hàng", Category = "Orders" },
      new() { ToolName = "confirm_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Xác nhận đơn hàng", Category = "Orders" },
      new() { ToolName = "start_processing_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Bắt đầu xử lý đơn", Category = "Orders" },
      new() { ToolName = "ship_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Tạo shipment", Category = "Orders" },
      new() { ToolName = "cancel_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Hủy đơn hàng", Category = "Orders" },
      new() { ToolName = "get_inventory_summary", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tồn kho tổng quan", Category = "Inventory" },
      new() { ToolName = "get_store_health_score", RiskLevel = "Read", RequiresConfirmation = false, Description = "Điểm sức khỏe cửa hàng", Category = "Inventory" },
      new() { ToolName = "create_purchase_note", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo ghi chú nhập hàng", Category = "Inventory" },
      new() { ToolName = "list_recent_reviews", RiskLevel = "Read", RequiresConfirmation = false, Description = "Đánh giá gần đây", Category = "Reviews" },
      new() { ToolName = "list_recent_comments", RiskLevel = "Read", RequiresConfirmation = false, Description = "Bình luận gần đây", Category = "Reviews" },
      new() { ToolName = "reply_to_review", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Phản hồi đánh giá khách hàng", Category = "Reviews" },
      new() { ToolName = "reply_to_comment", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Phản hồi bình luận khách hàng", Category = "Reviews" },
      new() { ToolName = "list_promo_codes", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê mã khuyến mãi", Category = "Promotions" },
      new() { ToolName = "create_promo_code", RiskLevel = "High", RequiresConfirmation = true, Description = "Tạo mã khuyến mãi mới", Category = "Promotions" },
      new() { ToolName = "generate_product_description", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo mô tả sản phẩm bằng AI", Category = "Intelligence" },
      new() { ToolName = "generate_weekly_report", RiskLevel = "Read", RequiresConfirmation = false, Description = "Báo cáo tuần", Category = "Intelligence" },
      new() { ToolName = "generate_daily_report", RiskLevel = "Read", RequiresConfirmation = false, Description = "Báo cáo doanh thu hôm nay", Category = "Intelligence" },
      new() { ToolName = "check_inventory_alerts", RiskLevel = "Read", RequiresConfirmation = false, Description = "Cảnh báo tồn kho thấp", Category = "Intelligence" },
      new() { ToolName = "toggle_autonomy", RiskLevel = "High", RequiresConfirmation = true, Description = "Bật/tắt chế độ tự động", Category = "System" },
      new() { ToolName = "get_autonomy_status", RiskLevel = "Read", RequiresConfirmation = false, Description = "Trạng thái chế độ tự động", Category = "System" },
    };

    dbContext.ToolRiskConfigs.AddRange(defaults);
    await dbContext.SaveChangesAsync();
  }
}
