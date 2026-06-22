using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services.AdminAiTools;

public sealed class AdminToolInstructionRegistry : IAdminToolInstructionRegistry
{
  private static readonly string[] MutatingToolNames =
  [
    "confirm_order", "start_processing_order", "ship_order", "complete_order", "cancel_order",
    "update_order_address", "update_order_items", "delete_order", "restore_order",
    "create_user", "update_user_status", "update_user_role", "update_user_profile", "delete_user", "restore_user",
    "create_role", "update_role", "delete_role",
    "create_product", "update_product", "delete_product", "restore_product", "toggle_product_status",
    "create_variant", "update_variant", "update_variant_stock", "delete_variant",
    "create_category", "update_category", "delete_category",
    "reply_to_review", "reply_to_comment", "hide_review", "show_review", "delete_review",
    "save_blog_draft", "publish_blog_post", "update_blog_post", "delete_blog_post",
    "send_marketing_campaign", "unsubscribe_subscriber", "delete_subscriber",
    "retry_email_job", "cancel_email_job", "delete_email_job",
    "create_promo_code", "update_promo_code", "toggle_promo_code", "delete_promo_code",
    "create_purchase_note",
    "toggle_autonomy", "upload_media", "delete_media", "reply_facebook_comment"
  ];

  private readonly Dictionary<string, ToolInstruction> _instructions;

  public AdminToolInstructionRegistry()
  {
    _instructions = MutatingToolNames.ToDictionary(
      name => name,
      CreateInstruction,
      StringComparer.Ordinal);
  }

  public bool TryGetInstruction(string toolName, out ToolInstruction instruction) =>
    _instructions.TryGetValue(toolName, out instruction!);

  private static ToolInstruction CreateInstruction(string toolName) => new(
    toolName,
    $"Thực hiện công cụ quản trị {toolName} sau khi backend xác minh mục tiêu và tham số.",
    "Phải có yêu cầu rõ từ admin. Nếu admin dùng tên/mã hiển thị, backend phải resolve sang GUID nội bộ trước khi ghi.",
    "Tool ghi phải có risk metadata, instruction, tham số đúng kiểu, GUID thật cho trường id/orderId/productId/variantId/userId/roleId/promoId.",
    "Không chấp nhận display id như AD-... trong trường GUID. Không thêm cờ destructive không có schema. Enum phải thuộc tập cho phép.",
    "Không tự xác nhận thay admin. Medium/High/Critical risk qua confirmation gate hiện có. Không đoán resource khi lookup mơ hồ.",
    "Nếu thiếu hoặc mơ hồ target, hỏi admin chọn hoặc nhập thêm; nếu có lookup read-only an toàn, resolve trước.",
    "Kết quả cho admin dùng mã/tên hiển thị, không lộ GUID nội bộ trừ khi admin hỏi trực tiếp.");
}
