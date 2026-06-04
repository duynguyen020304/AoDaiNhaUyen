using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Role : BaseEntity
{
  public required string Name { get; set; }
  public string? Description { get; set; }

  public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
