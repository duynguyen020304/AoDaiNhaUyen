using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class Comment : BaseEntity
{
  public Guid UserId { get; set; }
  public Guid ProductId { get; set; }
  public Guid? ParentCommentId { get; set; }
  public required string Content { get; set; }
  public int? Rating { get; set; }
  public bool IsVisible { get; set; } = true;

  public User User { get; set; } = null!;
  public Product Product { get; set; } = null!;
  public Comment? ParentComment { get; set; }
  public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
