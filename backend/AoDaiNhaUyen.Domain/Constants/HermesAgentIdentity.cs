namespace AoDaiNhaUyen.Domain.Constants;

/// <summary>
/// Fixed identity the Hermes growth agent acts as when it calls the admin API with the
/// <c>X-Hermes-Admin-Key</c> header (it has no human session). The authentication handler
/// synthesises a principal whose <c>NameIdentifier</c> is <see cref="UserId"/>, and any row
/// the agent writes that references a user (e.g. a review/comment reply sets
/// <c>Comment.UserId = this id</c>) FK-references <c>users.id</c> — so a matching user row
/// MUST be seeded or those writes fail with a 500.
///
/// This is the single source of truth for that id: the auth handler and the data seeder both
/// reference it, so they can never drift apart.
/// </summary>
public static class HermesAgentIdentity
{
  /// <summary>Canonical string form, for use in claims and configuration.</summary>
  public const string UserIdString = "470b9aa9-4bb7-46ae-845f-958003132d00";

  /// <summary>Typed form, for entity keys and EF queries.</summary>
  public static readonly Guid UserId = new(UserIdString);

  /// <summary>Display name shown for the agent's authored content.</summary>
  public const string DisplayName = "Nhã Uyên";
}
