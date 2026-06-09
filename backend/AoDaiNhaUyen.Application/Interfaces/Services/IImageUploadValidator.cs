namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IImageUploadValidator
{
  ImageUploadValidationResult Validate(string? contentType, byte[] bytes, long declaredLength, long maxBytes);
}

public sealed record ImageUploadValidationResult(bool IsValid, string? NormalizedContentType, string? ErrorCode, string? ErrorMessage)
{
  public static ImageUploadValidationResult Success(string contentType) => new(true, contentType, null, null);
  public static ImageUploadValidationResult Failure(string code, string message) => new(false, null, code, message);
}
