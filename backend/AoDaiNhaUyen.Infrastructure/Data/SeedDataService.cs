using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
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
    await SeedDemoReviewsAsync();
    await SeedBlogPostsAsync();
    await SeedToolRiskConfigsAsync();
    await SeedEmailTemplatesAsync();
    await RemoveStaleCategoriesAsync();
  }

  private async Task SeedEmailTemplatesAsync()
  {
    if (await dbContext.EmailTemplates.AnyAsync(x => x.Key.StartsWith("marketing.")))
    {
      return;
    }

    var now = DateTime.UtcNow;
    var templates = new[]
    {
      new EmailTemplate { Id = Guid.NewGuid(), Key = "marketing.confirm_subscription", Name = "Xác nhận đăng ký nhận tin", Subject = "{{subject}}", Preheader = "Xác nhận để nhận bộ sưu tập mới và ưu đãi từ Ao Dai Nha Uyen", HtmlBody = "<h2>Xác nhận đăng ký nhận tin</h2><p>Chào bạn,</p><p>Cảm ơn bạn đã đăng ký nhận tin từ <strong>Ao Dai Nha Uyen</strong>. Chỉ còn một bước nữa.</p><p style=\"text-align:center;margin:24px 0;\"><a href=\"{{confirmUrl}}\" style=\"display:inline-block;background:#8B4513;color:#fff;padding:12px 32px;border-radius:6px;text-decoration:none;font-weight:600;\">Xác nhận đăng ký</a></p><p style=\"font-size:13px;color:#6b7280;\">Nếu nút không hoạt động, vui lòng sao chép link sau vào trình duyệt: {{confirmUrl}}</p><p>Bạn sẽ nhận được các thông tin về:</p><ul><li>Bộ sưu tập áo dài mới</li><li>Khuyến mãi đặc biệt</li><li>Sự kiện và workshop</li></ul>", TextBody = "Xác nhận đăng ký nhận tin tại: {{confirmUrl}}", Locale = "vi-VN", Version = 1, CreatedAt = now, UpdatedAt = now },
      new EmailTemplate { Id = Guid.NewGuid(), Key = "marketing.welcome", Name = "Chào mừng đăng ký nhận tin", Subject = "Chào mừng bạn đến với Ao Dai Nha Uyen", Preheader = "Cảm ơn bạn đã gia nhập cộng đồng yêu áo dài", HtmlBody = "<h2>Chào mừng bạn, {{name}}!</h2><p>Cảm ơn bạn đã xác nhận đăng ký nhận tin từ <strong>Ao Dai Nha Uyen</strong>.</p><p>Chúng tôi sẽ gửi đến bạn:</p><ul><li>Bộ sưu tập áo dài mới nhất</li><li>Ưu đãi đặc biệt dành riêng cho thành viên</li><li>Lịch sự kiện và workshop</li><li>Mẹo phối đồ và bảo quản áo dài</li></ul><p style=\"text-align:center;margin:24px 0;\"><a href=\"{{shopUrl}}\" style=\"display:inline-block;background:#8B4513;color:#fff;padding:12px 32px;border-radius:6px;text-decoration:none;font-weight:600;\">Khám phá bộ sưu tập</a></p>", TextBody = "Chào mừng bạn đến với Ao Dai Nha Uyen. Khám phá bộ sưu tập tại {{shopUrl}}", Locale = "vi-VN", Version = 1, CreatedAt = now, UpdatedAt = now },
      new EmailTemplate { Id = Guid.NewGuid(), Key = "marketing.promo", Name = "Khuyến mãi", Subject = "{{subject}}", Preheader = "Ưu đãi đặc biệt từ Ao Dai Nha Uyen", HtmlBody = "<h2>{{heading}}</h2><p>{{body}}</p><p style=\"text-align:center;margin:24px 0;\"><a href=\"{{ctaUrl}}\" style=\"display:inline-block;background:#8B4513;color:#fff;padding:12px 32px;border-radius:6px;text-decoration:none;font-weight:600;\">{{ctaText}}</a></p><p style=\"font-size:12px;color:#9ca3af;\">Áp dụng đến {{expiryDate}}. Điều kiện áp dụng.</p>", TextBody = "{{heading}}\n{{body}}\n{{ctaText}}: {{ctaUrl}}\nÁp dụng đến {{expiryDate}}.", Locale = "vi-VN", Version = 1, CreatedAt = now, UpdatedAt = now }
    };

    dbContext.EmailTemplates.AddRange(templates);
    await dbContext.SaveChangesAsync();
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

  private async Task SeedBlogPostsAsync()
  {
    if (await dbContext.BlogPosts.AnyAsync(p => p.Slug == "lich-su-va-y-nghia-cua-ao-dai-viet-nam"))
    {
      return;
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

    var productSlugs = await dbContext.Products.AsNoTracking().OrderBy(p => p.Name).Select(p => p.Slug).Take(4).ToListAsync();
    if (productSlugs.Count == 0)
    {
      productSlugs = ["ao-dai-truyen-thong", "ao-dai-lua-tron", "ao-dai-theu-hoa"];
    }

    var posts = new List<BlogPost>
    {
      new()
      {
        Title = "Lịch sử và ý nghĩa của Áo Dài Việt Nam",
        Slug = "lich-su-va-y-nghia-cua-ao-dai-viet-nam",
        Excerpt = "Hành trình của áo dài từ trang phục truyền thống đến biểu tượng thanh lịch của phụ nữ Việt.",
        FeaturedImage = await BlogImageUrlAsync("home-ao-dai-truyen-thong.webp", true),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.StandardArticle,
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Áo dài trong dòng chảy văn hóa Việt" },
          new { type = "paragraph", content = "Áo dài không chỉ là trang phục, mà còn là ký ức văn hóa được gìn giữ qua nhiều thế hệ." },
          new { type = "image", src = await BlogImageUrlAsync("home-ao-dai-truyen-thong-node.webp", true), alt = "Người mẫu mặc áo dài truyền thống Việt Nam thanh lịch", caption = "Dáng áo truyền thống tôn nét mềm mại." },
          new { type = "paragraph", content = "Từ tà áo trắng học trò đến áo dài cưới, mỗi biến thể đều mang một câu chuyện riêng về sự duyên dáng và tự hào dân tộc." },
          new { type = "quote", content = "Một tà áo đẹp là sự cân bằng giữa chất liệu, phom dáng và cảm xúc của người mặc.", attribution = "Áo Dài Nhà Uyên" },
          new { type = "divider" },
          new { type = "callout", variant = "info", content = "Chọn áo dài nên bắt đầu từ hoàn cảnh sử dụng: lễ cưới, đi tiệc, chụp ảnh hay mặc hằng ngày." }
        }),
        Tags = Json(new[] { "áo dài", "văn hóa Việt", "truyền thống" }),
        AuthorNameOverride = "Nhà Uyên Editorial",
        AuthorBio = "Đội ngũ biên tập thời trang Áo Dài Nhà Uyên.",
        ReviewedBy = "Uyên Nguyễn",
        Status = BlogPostStatus.Published,
        PublishedAt = now.AddDays(-12),
        MetaTitle = "Lịch sử và ý nghĩa Áo Dài Việt Nam",
        MetaDescription = "Tìm hiểu lịch sử, ý nghĩa và nét đẹp văn hóa của áo dài Việt Nam.",
        CreatedAt = now.AddDays(-12),
        UpdatedAt = now.AddDays(-12)
      },
      new()
      {
        Title = "Bộ sưu tập Áo Dài xuân hè 2025",
        Slug = "bo-suu-tap-ao-dai-xuan-he-2025",
        Excerpt = "Những gợi ý màu sắc, chất liệu và họa tiết nổi bật cho mùa xuân hè 2025.",
        FeaturedImage = await BlogImageUrlAsync("ao-dai-cach-tan-1.webp", true),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.PhotoGallery,
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Sắc màu nhẹ, phom dáng hiện đại" },
          new { type = "paragraph", content = "Xuân hè 2025 ưu tiên chất liệu thoáng, sắc pastel và chi tiết thêu tinh tế." },
          new { type = "gallery", images = new object[]
            {
              new { src = await BlogImageUrlAsync("ao-dai-cach-tan-3.webp", true), alt = "Áo dài cách tân màu sáng cho xuân hè", caption = "Cách tân nhẹ nhàng" },
              new { src = await BlogImageUrlAsync("ao-dai-lua-tron-1.webp", true), alt = "Áo dài lụa trơn mềm mại màu pastel", caption = "Lụa trơn thanh lịch" },
              new { src = await BlogImageUrlAsync("ao-dai-theu-hoa-1.webp", true), alt = "Áo dài thêu hoa tinh tế cho dịp lễ", caption = "Thêu hoa nữ tính" },
              new { src = await BlogImageUrlAsync("ao-dai-lua-tron-4.webp", true), alt = "Áo dài lụa trơn dáng suông sang trọng", caption = "Dáng suông dễ mặc" }
            }
          },
          new { type = "paragraph", content = "Một bộ sưu tập tốt không chạy theo xu hướng quá nhanh, mà chọn điểm nhấn đủ bền để mặc nhiều dịp." }
        }),
        Tags = Json(new[] { "bộ sưu tập", "xuân hè", "áo dài cách tân" }),
        AuthorNameOverride = "Nhà Uyên Editorial",
        Status = BlogPostStatus.Published,
        PublishedAt = now.AddDays(-8),
        MetaTitle = "Bộ sưu tập Áo Dài xuân hè 2025",
        MetaDescription = "Khám phá xu hướng áo dài xuân hè 2025 qua bộ sưu tập ảnh chọn lọc.",
        CreatedAt = now.AddDays(-8),
        UpdatedAt = now.AddDays(-8)
      },
      new()
      {
        Title = "Hậu trường buổi chụp hình BST Áo Dài",
        Slug = "hau-truong-buoi-chup-hinh-bst-ao-dai",
        Excerpt = "Một góc nhìn hậu trường về ánh sáng, chất liệu và chuyển động tà áo trong buổi chụp hình.",
        FeaturedImage = await BlogImageUrlAsync("ao-dai-truyen-thong-1.webp", false),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.VideoFeature,
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Khi tà áo chuyển động trước ống kính" },
          new { type = "paragraph", content = "Buổi chụp tập trung vào chuyển động tự nhiên, ánh sáng mềm và cách chất liệu bắt sáng." },
          new { type = "embed", url = "https://www.youtube.com/embed/dQw4w9WgXcQ", caption = "Video hậu trường minh họa" },
          new { type = "quote", content = "Chúng tôi luôn thử nhiều nhịp bước để tà áo rơi đúng khoảnh khắc đẹp nhất.", attribution = "Đội ngũ styling" }
        }),
        Tags = Json(new[] { "hậu trường", "video", "bộ sưu tập" }),
        AuthorNameOverride = "Nhà Uyên Studio",
        Status = BlogPostStatus.Draft,
        CreatedAt = now.AddDays(-6),
        UpdatedAt = now.AddDays(-6)
      },
      new()
      {
        Title = "Áo Dài cưới - Lựa chọn hoàn hảo cho ngày trọng đại",
        Slug = "ao-dai-cuoi-lua-chon-hoan-hao-cho-ngay-trong-dai",
        Excerpt = "Cách chọn áo dài cưới hài hòa với dáng người, màu da và phong cách lễ gia tiên.",
        FeaturedImage = await BlogImageUrlAsync("ao-dai-theu-hoa-5.webp", true),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.ProductSpotlight,
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Áo dài cưới cần vừa trang trọng vừa thoải mái" },
          new { type = "paragraph", content = "Ngày cưới kéo dài nhiều giờ, nên áo dài cần đẹp khi đứng, ngồi, di chuyển và chụp ảnh." },
          new { type = "product_spotlight", productSlugs = productSlugs.Take(4).ToArray() },
          new { type = "image", src = await BlogImageUrlAsync("ao-dai-theu-hoa-6.webp", true), alt = "Áo dài thêu hoa sang trọng dành cho cô dâu", caption = "Họa tiết thêu tạo điểm nhấn cho lễ gia tiên." },
          new { type = "paragraph", content = "Ưu tiên phom ôm vừa, chất liệu có độ đứng nhẹ và họa tiết tập trung ở cổ, tay hoặc tà áo." }
        }),
        Tags = Json(new[] { "áo dài cưới", "cô dâu", "product spotlight" }),
        AuthorNameOverride = "Nhà Uyên Bridal",
        ReviewedBy = "Uyên Nguyễn",
        Status = BlogPostStatus.Published,
        PublishedAt = now.AddDays(-4),
        MetaTitle = "Cách chọn Áo Dài cưới đẹp cho ngày trọng đại",
        MetaDescription = "Gợi ý chọn áo dài cưới theo dáng người, chất liệu và phong cách lễ gia tiên.",
        CreatedAt = now.AddDays(-4),
        UpdatedAt = now.AddDays(-4)
      },
      new()
      {
        Title = "Cách chọn và bảo quản Áo Dài đúng cách",
        Slug = "cach-chon-va-bao-quan-ao-dai-dung-cach",
        Excerpt = "Hướng dẫn chọn size, thử áo, giặt và cất giữ áo dài để giữ phom dáng lâu bền.",
        FeaturedImage = await BlogImageUrlAsync("ao-dai-lua-tron-extra.webp", false),
        FeaturedImageWidth = 1200,
        FeaturedImageHeight = 800,
        Template = BlogPostTemplate.HowTo,
        Content = Json(new object[]
        {
          new { type = "heading", level = 2, content = "Quy trình 6 bước chăm sóc áo dài" },
          new { type = "paragraph", content = "Áo dài đẹp lâu khi được chọn đúng số đo và bảo quản đúng chất liệu." },
          new { type = "step", stepNumber = 1, title = "Đo số đo cơ bản", content = "Ghi lại vòng ngực, eo, mông, vai và chiều dài áo trước khi chọn size.", tip = "Nên đo khi mặc đồ mỏng để số đo sát hơn." },
          new { type = "step", stepNumber = 2, title = "Thử cử động", content = "Khi thử áo, hãy ngồi xuống, đưa tay nhẹ và bước vài bước để kiểm tra độ thoải mái." },
          new { type = "step", stepNumber = 3, title = "Giặt nhẹ", content = "Ưu tiên giặt tay bằng nước mát, tránh vò mạnh phần thêu hoặc đính kết." },
          new { type = "callout", variant = "tip", content = "Không phơi áo dài dưới nắng gắt. Lộn trái áo và phơi nơi thoáng mát." },
          new { type = "image", src = await BlogImageUrlAsync("phu-kien-quat.webp", false), alt = "Phụ kiện quạt kết hợp cùng áo dài truyền thống", caption = "Bảo quản phụ kiện riêng để tránh móc vào vải áo." }
        }),
        Tags = Json(new[] { "hướng dẫn", "bảo quản áo dài", "chọn size" }),
        AuthorNameOverride = "Nhà Uyên Care",
        Status = BlogPostStatus.Draft,
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
