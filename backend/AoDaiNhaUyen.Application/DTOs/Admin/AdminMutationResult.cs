namespace AoDaiNhaUyen.Application.DTOs.Admin;

public sealed record AdminMutationResult(bool Succeeded, string? ErrorCode = null, string? ErrorMessage = null)
{
  public static AdminMutationResult Success() => new(true);
  public static AdminMutationResult Failure(string code, string message) => new(false, code, message);
}
