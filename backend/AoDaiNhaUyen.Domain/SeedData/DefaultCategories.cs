namespace AoDaiNhaUyen.Domain.SeedData;

public static class DefaultCategories
{
  public static readonly IReadOnlyList<SeedCategory> Items =
  [
    new("Áo dài", "ao-dai", 1, null),
    new("Phụ kiện", "phu-kien", 2, null),
    new("Áo dài truyền thống", "ao-dai-truyen-thong", 1, "ao-dai"),
    new("Áo dài cách tân", "ao-dai-cach-tan", 2, "ao-dai"),
    new("Áo dài lụa trơn", "ao-dai-lua-tron", 3, "ao-dai"),
    new("Áo dài thêu hoa", "ao-dai-theu-hoa", 4, "ao-dai"),
    new("Áo dài cưới", "ao-dai-cuoi", 5, "ao-dai"),
    new("Quạt cầm tay", "quat-cam-tay", 1, "phu-kien"),
    new("Túi xách", "tui-xach", 2, "phu-kien"),
    new("Trâm cài tóc", "tram-cai-toc", 3, "phu-kien"),
    new("Guốc", "guoc", 4, "phu-kien")
  ];
}

public sealed record SeedCategory(string Name, string Slug, int SortOrder, string? ParentSlug);
