namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>
/// Request body for the admin AI try-on generation endpoint exposed to the
/// Hermes agent. The agent passes a presigned person-image URL (typically from
/// <c>GET /api/admin/social/messages/{messageId}/image</c>) plus a garment
/// product id chosen by the customer.
/// </summary>
/// <param name="GarmentProductId">ID của sản phẩm áo dài đã chọn (phải có AI asset active; lấy từ GET /api/admin/ai-tryon/catalog).</param>
/// <param name="GarmentVariantId">Optional variant id (nếu sản phẩm có nhiều màu/size).</param>
/// <param name="AccessoryProductIds">Optional danh sách phụ kiện đi kèm (đã có AI asset).</param>
/// <param name="PersonImageUrl">Presigned HTTPS URL ảnh người mặc (từ GET /api/admin/social/messages/{id}/image).</param>
public sealed record AdminGenerateTryOnRequestDto(
  Guid GarmentProductId,
  Guid? GarmentVariantId,
  IReadOnlyList<Guid>? AccessoryProductIds,
  string PersonImageUrl);

/// <summary>
/// Result of admin AI try-on generation.
/// </summary>
/// <param name="ResultImageUrl">Presigned HTTPS URL (1 giờ) của ảnh thử đồ đã sinh — gửi ngay qua POST /api/admin/social/conversations/{conversationId}/messages với attachmentType=image.</param>
/// <param name="MimeType">Mime type của ảnh kết quả.</param>
/// <param name="GeneratedImageId">ID của UserGeneratedImage nếu được lưu, null khi chạy không gắn user.</param>
public sealed record AdminGenerateTryOnResultDto(
  string ResultImageUrl,
  string MimeType,
  Guid? GeneratedImageId);

/// <summary>
/// Response cho GET /api/admin/social/messages/{messageId}/image — presigned
/// URL của ảnh inbound đã được lưu trong private storage.
/// </summary>
public sealed record SocialMessageImageDto(
  Guid MessageId,
  string? PresignedUrl,
  string? MimeType,
  bool HasStoredImage);
