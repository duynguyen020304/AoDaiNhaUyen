namespace AoDaiNhaUyen.Domain.SeedData;

public static class DefaultCustomers
{
  public static readonly IReadOnlyList<SeedCustomer> Items =
  [
    new(
      "Nguyen Ha An",
      "ha.an@example.com",
      "0901000001",
      "female",
      "seed-customer-password"),
    new(
      "Tran Minh Chau",
      "minh.chau@example.com",
      "0901000002",
      "female",
      "seed-customer-password"),
    new(
      "Le Quynh Nhu",
      "quynh.nhu@example.com",
      "0901000003",
      "female",
      "seed-customer-password")
  ];
}

public sealed record SeedCustomer(
  string FullName,
  string Email,
  string Phone,
  string Gender,
  string PasswordHash);
