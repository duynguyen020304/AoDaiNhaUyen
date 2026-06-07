namespace AoDaiNhaUyen.Domain.Common;

public enum AdminAiActionType
{
  /// <summary>Read-only queries — dashboard, listing, stats.</summary>
  Query = 0,

  /// <summary>Create operations — new product, category, user.</summary>
  Create = 1,

  /// <summary>Update operations — edit product, category, user.</summary>
  Update = 2,

  /// <summary>Delete/soft-delete operations.</summary>
  Delete = 3,

  /// <summary>Restore soft-deleted entities.</summary>
  Restore = 4,

  /// <summary>Status toggles — product active/inactive, user enable/disable.</summary>
  Toggle = 5,

  /// <summary>Role assignment changes.</summary>
  RoleChange = 6,

  /// <summary>Image upload / media operations.</summary>
  ImageUpload = 7,

  /// <summary>Generative AI — descriptions, copy, reports.</summary>
  Generative = 8,

  /// <summary>System/prompt-only interaction with no data mutation.</summary>
  Chat = 9
}
