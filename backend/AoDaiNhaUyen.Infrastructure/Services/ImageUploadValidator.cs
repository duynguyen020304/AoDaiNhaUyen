using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ImageUploadValidator : IImageUploadValidator
{
  public ImageUploadValidationResult Validate(string? contentType, byte[] bytes, long declaredLength, long maxBytes)
  {
    if (declaredLength <= 0 || bytes.Length == 0)
    {
      return ImageUploadValidationResult.Failure("invalid_image", "Ảnh không được để trống.");
    }

    if (declaredLength > maxBytes || bytes.LongLength > maxBytes)
    {
      return ImageUploadValidationResult.Failure("invalid_image", "Ảnh vượt quá dung lượng cho phép.");
    }

    var detected = DetectContentType(bytes);
    if (detected is null)
    {
      return ImageUploadValidationResult.Failure("invalid_image_type", "Chỉ hỗ trợ ảnh PNG, JPG hoặc WEBP.");
    }

    if (!string.IsNullOrWhiteSpace(contentType) &&
        !IsAllowedDeclaredContentType(contentType))
    {
      return ImageUploadValidationResult.Failure("invalid_image_type", "Chỉ hỗ trợ ảnh PNG, JPG hoặc WEBP.");
    }

    return ImageUploadValidationResult.Success(detected);
  }

  private static bool IsAllowedDeclaredContentType(string contentType) =>
    contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
    contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) ||
    contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
    contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase);

  private static string? DetectContentType(ReadOnlySpan<byte> bytes)
  {
    if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
    {
      return "image/jpeg";
    }

    if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
        bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
    {
      return "image/png";
    }

    if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
        bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
    {
      return "image/webp";
    }

    return null;
  }
}
