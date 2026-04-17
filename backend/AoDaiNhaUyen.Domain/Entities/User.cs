namespace AoDaiNhaUyen.Domain.Entities;

public sealed class User
{
  public long Id { get; set; }
  public required string FullName { get; set; }
  public string? Email { get; set; }
  public string? Phone { get; set; }
  public string? Gender { get; set; }
  public DateOnly? DateOfBirth { get; set; }
  public string? AvatarUrl { get; set; }
  public string Status { get; set; } = "active";
  public DateTime? EmailVerifiedAt { get; set; }
  public DateTime? PhoneVerifiedAt { get; set; }
  public DateTime? LastLoginAt { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();
  public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
  public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
  public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
  public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
  public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
  public ICollection<MeasurementProfile> MeasurementProfiles { get; set; } = new List<MeasurementProfile>();
  public ICollection<Order> Orders { get; set; } = new List<Order>();
  public ICollection<Cart> Carts { get; set; } = new List<Cart>();
  public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
