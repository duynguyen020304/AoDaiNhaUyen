namespace AoDaiNhaUyen.Domain.SeedData;

public static class DefaultMaterials
{
  public static readonly IReadOnlyList<SeedMaterial> Items =
  [
    new("Lua To Tam", "lua-to-tam", "Chat vai mem, do rap tu nhien", "/assets/materials/lua-to-tam.jpg"),
    new("Gam Theu", "gam-theu", "Bam dang tot, hoa tiet theu noi", "/assets/materials/gam-theu.jpg"),
    new("Voan Cao Cap", "voan-cao-cap", "Nhe, thoang va ton dang", "/assets/materials/voan-cao-cap.jpg")
  ];
}

public sealed record SeedMaterial(string Name, string Slug, string Description, string SwatchImageUrl);
