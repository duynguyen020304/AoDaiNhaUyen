namespace AoDaiNhaUyen.Domain.SeedData;

public static class DefaultProducts
{
  private const string UploadPath = "/upload";

  public static readonly IReadOnlyList<SeedProduct> Items =
  [
    CreateAoDai("Áo dài cách tân 1", "ao-dai-cach-tan-1", "ao-dai-cach-tan", "gam-theu", 3250000m, true),
    CreateAoDai("Áo dài cách tân 3", "ao-dai-cach-tan-3", "ao-dai-cach-tan", "gam-theu", 3350000m, true),
    CreateAoDai("Áo dài cách tân 4", "ao-dai-cach-tan-4", "ao-dai-cach-tan", "gam-theu", 3450000m, false),
    CreateAoDai("Áo dài cách tân 5", "ao-dai-cach-tan-5", "ao-dai-cach-tan", "gam-theu", 3550000m, false),
    CreateAoDai("Áo dài cách tân 6", "ao-dai-cach-tan-6", "ao-dai-cach-tan", "gam-theu", 3650000m, false),

    CreateAoDai("Áo dài lụa trơn 1", "ao-dai-lua-tron-1", "ao-dai-lua-tron", "lua-to-tam", 2850000m, true),
    CreateAoDai("Áo dài lụa trơn 2", "ao-dai-lua-tron-2", "ao-dai-lua-tron", "lua-to-tam", 2950000m, true),
    CreateAoDai("Áo dài lụa trơn 3", "ao-dai-lua-tron-3", "ao-dai-lua-tron", "lua-to-tam", 3050000m, false),
    CreateAoDai("Áo dài lụa trơn 4", "ao-dai-lua-tron-4", "ao-dai-lua-tron", "lua-to-tam", 3150000m, false),
    CreateAoDai("Áo dài lụa trơn 5", "ao-dai-lua-tron-5", "ao-dai-lua-tron", "lua-to-tam", 3250000m, false),
    CreateAoDai("Áo dài lụa trơn 6", "ao-dai-lua-tron-6", "ao-dai-lua-tron", "lua-to-tam", 3350000m, false),
    CreateAoDai("Áo dài lụa trơn đặc biệt", "ao-dai-lua-tron-extra", "ao-dai-lua-tron", "lua-to-tam", 3450000m, false),

    CreateAoDai("Áo dài thêu hoa 1", "ao-dai-theu-hoa-1", "ao-dai-theu-hoa", "gam-theu", 3850000m, true),
    CreateAoDai("Áo dài thêu hoa 2", "ao-dai-theu-hoa-2", "ao-dai-theu-hoa", "gam-theu", 3950000m, true),
    CreateAoDai("Áo dài thêu hoa 3", "ao-dai-theu-hoa-3", "ao-dai-theu-hoa", "gam-theu", 4050000m, false),
    CreateAoDai("Áo dài thêu hoa 5", "ao-dai-theu-hoa-5", "ao-dai-theu-hoa", "gam-theu", 4150000m, false),
    CreateAoDai("Áo dài thêu hoa 6", "ao-dai-theu-hoa-6", "ao-dai-theu-hoa", "gam-theu", 4250000m, false),

    CreateAoDai("Áo dài truyền thống 1", "ao-dai-truyen-thong-1", "ao-dai-truyen-thong", "lua-to-tam", 3050000m, true),
    CreateAoDai("Áo dài truyền thống 2", "ao-dai-truyen-thong-2", "ao-dai-truyen-thong", "lua-to-tam", 3150000m, true),
    CreateAoDai("Áo dài truyền thống 4", "ao-dai-truyen-thong-4", "ao-dai-truyen-thong", "lua-to-tam", 3250000m, false),
    CreateAoDai("Áo dài truyền thống 5", "ao-dai-truyen-thong-5", "ao-dai-truyen-thong", "lua-to-tam", 3350000m, false),
    CreateAoDai("Áo dài truyền thống 6", "ao-dai-truyen-thong-6", "ao-dai-truyen-thong", "lua-to-tam", 3450000m, false),
    CreateAoDai("Áo dài truyền thống nơ đỏ", "home-ao-dai-truyen-thong-node", "ao-dai-truyen-thong", "lua-to-tam", 3550000m, true),
    CreateAoDai("Áo dài truyền thống", "home-ao-dai-truyen-thong", "ao-dai-truyen-thong", "lua-to-tam", 3650000m, true),

    CreateAccessory("Quạt bầu", "phu-kien-quat-bau", "quat", 420000m, true),
    CreateAccessory("Quạt gấp tua rua", "phu-kien-quat-gap-tua-rua", "quat", 460000m, true),
    CreateAccessory("Quạt hoa sen", "phu-kien-quat-hoa-sen", "quat", 440000m, false),
    CreateAccessory("Quạt tre gấp", "phu-kien-quat-tre-gap", "quat", 390000m, false),
    CreateAccessory("Quạt tròn", "phu-kien-quat-tron", "quat", 410000m, false),
    CreateAccessory("Quạt vân mây", "phu-kien-quat-van-may", "quat", 450000m, false),

    CreateAccessory("Guốc cao gót bướm trắng", "guoc-cao-got-buom-trang", "giay", 890000m, true),
    CreateAccessory("Guốc cao gót hoa cách điệu", "guoc-cao-got-hoa-cach-dieu", "giay", 920000m, true),
    CreateAccessory("Guốc cao gót hoa", "guoc-cao-got-hoa", "giay", 860000m, false),
    CreateAccessory("Guốc cao gót kiểu cao", "guoc-cao-got-kieu-cao", "giay", 960000m, false),
    CreateAccessory("Guốc cao gót nơ đế bằng", "guoc-cao-got-no-de-bang", "giay", 880000m, false),
    CreateAccessory("Guốc cao gót nơ", "guoc-cao-got-no", "giay", 900000m, false),

    CreateAccessory("Túi sách hoa văn", "phu-kien-tu-xach-hoa-van", "tui-sach", 680000m, true),
    CreateAccessory("Túi sách kiểu", "phu-kien-tu-xach-kieu", "tui-sach", 720000m, true),
    CreateAccessory("Túi sách thanh", "phu-kien-tu-xach-thanh", "tui-sach", 650000m, false),
    CreateAccessory("Túi sách thêu hoa", "phu-kien-tu-xach-theu-hoa", "tui-sach", 760000m, false),
    CreateAccessory("Túi sách thức", "phu-kien-tu-xach-thuc", "tui-sach", 690000m, false),
    CreateAccessory("Túi sách xà cừ", "phu-kien-tu-xach-xan-oc", "tui-sach", 790000m, false),

    CreateAccessory("Trâm cài hoa đơn 2", "tram-cai-hoa-don-2", "tram-cai", 360000m, true),
    CreateAccessory("Trâm cài hoa đơn sắc", "tram-cai-hoa-don-sac", "tram-cai", 340000m, true),
    CreateAccessory("Trâm cài hoa đơn", "tram-cai-hoa-don", "tram-cai", 320000m, false),
    CreateAccessory("Trâm cài tóc hồng ngọc bích", "tram-cai-toc-hong-ngoc-bich", "tram-cai", 390000m, false),
    CreateAccessory("Trâm cài tóc vàng sắc điệu", "tram-cai-toc-vang-sac-dieu", "tram-cai", 380000m, false),
    CreateAccessory("Trâm cài tóc xanh biếc", "tram-cai-toc-xanh-biec", "tram-cai", 370000m, false)
  ];

  private static SeedProduct CreateAoDai(
    string name,
    string imageSlug,
    string categorySlug,
    string materialSlug,
    decimal price,
    bool isFeatured)
  {
    return new SeedProduct(
      name,
      imageSlug,
      "Thiết kế áo dài Nha Uyên may sẵn.",
      "Mẫu áo dài được chọn lọc theo tinh thần thanh lịch, phù hợp đi tiệc, chụp ảnh và các dịp trang trọng.",
      price,
      "VND",
      categorySlug,
      materialSlug,
      isFeatured,
      [CreateImage(imageSlug, name)],
      [
        CreateVariant(imageSlug, "S", "S", price, 8, true),
        CreateVariant(imageSlug, "M", "M", price + 150000m, 10, false),
        CreateVariant(imageSlug, "L", "L", price + 300000m, 6, false)
      ]);
  }

  private static SeedProduct CreateAccessory(
    string name,
    string imageSlug,
    string categorySlug,
    decimal price,
    bool isFeatured)
  {
    return new SeedProduct(
      name,
      imageSlug,
      "Phụ kiện phối cùng áo dài.",
      "Phụ kiện hoàn thiện tổng thể trang phục, phù hợp dùng cùng các bộ áo dài truyền thống và cách tân.",
      price,
      "VND",
      categorySlug,
      null,
      isFeatured,
      [CreateImage(imageSlug, name)],
      [CreateVariant(imageSlug, "STD", null, price, 15, true)]);
  }

  private static SeedProductImage CreateImage(string slug, string altText)
  {
    return new SeedProductImage($"{UploadPath}/{slug}.webp", altText, 1, true);
  }

  private static SeedProductVariant CreateVariant(
    string productSlug,
    string skuSuffix,
    string? size,
    decimal price,
    int stockQty,
    bool isDefault)
  {
    var sku = $"{productSlug}-{skuSuffix}".ToUpperInvariant().Replace('-', '_');
    var variantName = size is null ? "Mặc định" : $"Size {size}";
    return new SeedProductVariant(sku, variantName, size, null, price, null, stockQty, isDefault);
  }
}

public sealed record SeedProduct(
  string Name,
  string Slug,
  string ShortDescription,
  string LongDescription,
  decimal Price,
  string Currency,
  string CategorySlug,
  string? MaterialSlug,
  bool IsFeatured,
  IReadOnlyList<SeedProductImage> Images,
  IReadOnlyList<SeedProductVariant> Variants);

public sealed record SeedProductImage(string ImageUrl, string AltText, int SortOrder, bool IsPrimary);

public sealed record SeedProductVariant(
  string Sku,
  string VariantName,
  string? Size,
  string? Color,
  decimal Price,
  decimal? SalePrice,
  int StockQty,
  bool IsDefault);
