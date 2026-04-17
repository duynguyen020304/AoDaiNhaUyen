namespace AoDaiNhaUyen.Domain.SeedData;

public static class DefaultProducts
{
  public static readonly IReadOnlyList<SeedProduct> Items =
  [
    new(
      "Ao Dai Lua Nha Uyen 01",
      "ao-dai-lua-nha-uyen-01",
      "Thiet ke lua to tam dang suong.",
      "Mau ao dai lua to tam toi gian, phu hop cho su kien va di lam.",
      2850000m,
      "VND",
      "ao-dai-truyen-thong",
      "lua-to-tam",
      true,
      new List<SeedProductImage>
      {
        new("/assets/products/aodai-lua-01-main.jpg", "Ao dai lua 01", 1, true),
        new("/assets/products/aodai-lua-01-detail.jpg", "Chi tiet hoa van ao dai lua 01", 2, false)
      }),
    new(
      "Ao Dai Gam Cach Tan 02",
      "ao-dai-gam-cach-tan-02",
      "Dang ao cach tan sang trong.",
      "Mau gam cach tan voi form gon, duong cat hien dai.",
      3250000m,
      "VND",
      "ao-dai-cach-tan",
      "gam-theu",
      true,
      new List<SeedProductImage>
      {
        new("/assets/products/aodai-gam-02-main.jpg", "Ao dai gam 02", 1, true),
        new("/assets/products/aodai-gam-02-detail.jpg", "Chi tiet theu ao dai gam 02", 2, false)
      }),
    new(
      "Ao Dai Cuoi Voan 03",
      "ao-dai-cuoi-voan-03",
      "Mau ao dai cuoi voan nhe.",
      "Ao dai cuoi voan cao cap, gam mau trang ngoc trai.",
      4600000m,
      "VND",
      "ao-dai-cuoi",
      "voan-cao-cap",
      false,
      new List<SeedProductImage>
      {
        new("/assets/products/aodai-cuoi-03-main.jpg", "Ao dai cuoi 03", 1, true)
      })
  ];
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
  IReadOnlyList<SeedProductImage> Images);

public sealed record SeedProductImage(string ImageUrl, string AltText, int SortOrder, bool IsPrimary);
