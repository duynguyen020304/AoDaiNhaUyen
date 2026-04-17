namespace AoDaiNhaUyen.Domain.SeedData;

public static class DefaultStoreLocations
{
  public static readonly IReadOnlyList<SeedStoreLocation> Items =
  [
    new(
      "Ao Dai Nha Uyen Flagship",
      "36 Nguyen Hue, Ben Nghe",
      "Ho Chi Minh",
      "0909 123 456",
      "0312345678",
      "08:30 - 21:00",
      10.77689m,
      106.70081m)
  ];
}

public sealed record SeedStoreLocation(
  string Name,
  string AddressLine,
  string City,
  string Phone,
  string TaxCode,
  string OpeningHours,
  decimal? Latitude,
  decimal? Longitude);
