using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminToolRiskService(
  AppDbContext dbContext,
  ISafetyGate safetyGate,
  IHermesEventOutboxPublisher hermesEvents) : IAdminToolRiskService
{
  public async Task<IReadOnlyList<ToolRiskConfigDto>> GetAllAsync(CancellationToken ct = default)
  {
    var configs = await dbContext.ToolRiskConfigs
      .AsNoTracking()
      .OrderBy(c => c.Category)
      .ThenBy(c => c.ToolName)
      .Select(c => new ToolRiskConfigDto(
        c.Id, c.ToolName, c.RiskLevel, c.RequiresConfirmation, c.Description, c.Category))
      .ToListAsync(ct);

    return configs;
  }

  public async Task<bool> UpdateAsync(Guid id, UpdateToolRiskRequest request, CancellationToken ct = default)
  {
    var config = await dbContext.ToolRiskConfigs.FindAsync([id], ct);
    if (config is null) return false;

    config.RiskLevel = request.RiskLevel;
    config.RequiresConfirmation = request.RequiresConfirmation;
    config.UpdatedAt = DateTimeOffset.UtcNow;

    // Invalidate SafetyGate cache so new config takes effect immediately
    await safetyGate.InvalidateCacheAsync(ct);

    await dbContext.SaveChangesAsync(ct);

    await hermesEvents.EnqueueAdminAiConfigEventAsync(
      "hermes_config_changed",
      config.Id.ToString("N"),
      new { configId = config.Id, config.ToolName, config.RiskLevel, config.RequiresConfirmation, config.Category },
      $"hermes_config_changed:HermesConfig:{config.Id:N}:{config.RiskLevel}:{config.UpdatedAt.Ticks}",
      ct);

    return true;
  }

  public async Task SeedDefaultsAsync(CancellationToken ct = default)
  {
    var existing = await dbContext.ToolRiskConfigs.Select(c => c.ToolName).ToListAsync(ct);
    var existingSet = new HashSet<string>(existing);

    var defaults = GetDefaultToolConfigs();
    var toAdd = defaults.Where(d => !existingSet.Contains(d.ToolName)).ToList();

    if (toAdd.Count > 0)
    {
      dbContext.ToolRiskConfigs.AddRange(toAdd);
      await dbContext.SaveChangesAsync(ct);
    }
  }

  /// <summary>
  /// Single source of truth for default tool risk configurations.
  /// Consumed by <see cref="SeedDefaultsAsync"/> and by <c>SeedDataService</c>
  /// (via the shared <c>ToolRiskConfigDefaults</c> static below) to avoid the
  /// previous drift between three divergent copies. New tools MUST be added here.
  /// </summary>
  public static List<ToolRiskConfig> GetDefaultToolConfigs()
  {
    return
    [
      // Dashboard
      new() { ToolName = "get_dashboard_summary", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tổng quan dashboard", Category = "Dashboard" },
      new() { ToolName = "get_revenue", RiskLevel = "Read", RequiresConfirmation = false, Description = "Dữ liệu doanh thu", Category = "Dashboard" },
      new() { ToolName = "get_orders_by_status", RiskLevel = "Read", RequiresConfirmation = false, Description = "Phân phối đơn theo trạng thái", Category = "Dashboard" },
      new() { ToolName = "get_recent_orders", RiskLevel = "Read", RequiresConfirmation = false, Description = "Đơn hàng gần đây", Category = "Dashboard" },
      new() { ToolName = "get_top_products", RiskLevel = "Read", RequiresConfirmation = false, Description = "Sản phẩm bán chạy", Category = "Dashboard" },

      // Date-range / specific-day queries (all read-only aggregations)
      new() { ToolName = "get_revenue_by_range", RiskLevel = "Read", RequiresConfirmation = false, Description = "Doanh thu theo ngày/khoảng ngày", Category = "Reports" },
      new() { ToolName = "get_orders_by_status_by_range", RiskLevel = "Read", RequiresConfirmation = false, Description = "Phân phối đơn theo khoảng ngày", Category = "Reports" },
      new() { ToolName = "get_top_products_by_range", RiskLevel = "Read", RequiresConfirmation = false, Description = "Top sản phẩm theo khoảng ngày", Category = "Reports" },
      new() { ToolName = "get_range_metrics", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tổng hợp số liệu theo khoảng ngày", Category = "Reports" },
      new() { ToolName = "list_orders_by_range", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê đơn theo khoảng ngày", Category = "Reports" },
      new() { ToolName = "count_by_created_range", RiskLevel = "Read", RequiresConfirmation = false, Description = "Đếm mọi loại entity theo khoảng ngày (timeline universal)", Category = "Reports" },

      // Live shop activity & Hermes reports
      new() { ToolName = "list_recent_activity", RiskLevel = "Read", RequiresConfirmation = false, Description = "Dòng hoạt động gần đây từ Hermes", Category = "Activity" },
      new() { ToolName = "list_hermes_reports", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê báo cáo Hermes", Category = "Activity" },
      new() { ToolName = "get_hermes_report", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết báo cáo Hermes", Category = "Activity" },
      new() { ToolName = "list_hermes_events", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê sự kiện Hermes outbox", Category = "Activity" },

      // Products
      new() { ToolName = "list_products", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê sản phẩm", Category = "Products" },
      new() { ToolName = "get_product", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết sản phẩm", Category = "Products" },
      new() { ToolName = "create_product", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo sản phẩm mới (nháp)", Category = "Products" },
      new() { ToolName = "update_product", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật sản phẩm", Category = "Products" },
      new() { ToolName = "delete_product", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm sản phẩm", Category = "Products" },
      new() { ToolName = "toggle_product_status", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Bật/tắt trạng thái sản phẩm", Category = "Products" },
      new() { ToolName = "list_variants", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê biến thể sản phẩm", Category = "Products" },
      new() { ToolName = "create_variant", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo biến thể sản phẩm", Category = "Products" },
      new() { ToolName = "update_variant", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật biến thể sản phẩm", Category = "Products" },
      new() { ToolName = "update_variant_stock", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật tồn kho biến thể", Category = "Products" },
      new() { ToolName = "delete_variant", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm biến thể sản phẩm", Category = "Products" },

      // Categories
      new() { ToolName = "list_categories", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê danh mục", Category = "Categories" },
      new() { ToolName = "create_category", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo danh mục mới", Category = "Categories" },
      new() { ToolName = "update_category", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật danh mục", Category = "Categories" },
      new() { ToolName = "delete_category", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm danh mục", Category = "Categories" },

      // Users
      new() { ToolName = "list_users", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê người dùng", Category = "Users" },
      new() { ToolName = "get_user", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết người dùng", Category = "Users" },
      new() { ToolName = "update_user_status", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Bật/tắt trạng thái người dùng", Category = "Users" },
      new() { ToolName = "update_user_role", RiskLevel = "High", RequiresConfirmation = true, Description = "Thay đổi vai trò người dùng", Category = "Users" },
      new() { ToolName = "update_user_profile", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật hồ sơ người dùng", Category = "Users" },
      new() { ToolName = "create_role", RiskLevel = "High", RequiresConfirmation = true, Description = "Tạo vai trò mới", Category = "Users" },
      new() { ToolName = "list_roles", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê vai trò", Category = "Users" },
      new() { ToolName = "update_role", RiskLevel = "High", RequiresConfirmation = true, Description = "Cập nhật vai trò", Category = "Users" },
      new() { ToolName = "delete_role", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa vai trò", Category = "Users" },
      new() { ToolName = "create_user", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo người dùng mới", Category = "Users" },
      new() { ToolName = "delete_user", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm người dùng", Category = "Users" },
      new() { ToolName = "restore_user", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Khôi phục người dùng đã xóa", Category = "Users" },

      // Orders
      new() { ToolName = "list_orders", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê đơn hàng", Category = "Orders" },
      new() { ToolName = "get_order", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết đơn hàng", Category = "Orders" },
      new() { ToolName = "confirm_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Xác nhận đơn hàng", Category = "Orders" },
      new() { ToolName = "start_processing_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Bắt đầu xử lý đơn", Category = "Orders" },
      new() { ToolName = "ship_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Tạo shipment", Category = "Orders" },
      new() { ToolName = "complete_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Hoàn tất đơn hàng", Category = "Orders" },
      new() { ToolName = "cancel_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Hủy đơn hàng", Category = "Orders" },
      new() { ToolName = "update_order_address", RiskLevel = "High", RequiresConfirmation = true, Description = "Cập nhật địa chỉ nhận hàng", Category = "Orders" },
      new() { ToolName = "update_order_items", RiskLevel = "High", RequiresConfirmation = true, Description = "Cập nhật dòng hàng đơn", Category = "Orders" },
      new() { ToolName = "delete_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm đơn hàng", Category = "Orders" },
      new() { ToolName = "restore_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Khôi phục đơn hàng", Category = "Orders" },

      // Inventory, Media & Store Health
      new() { ToolName = "get_inventory_summary", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tồn kho tổng quan", Category = "Inventory" },
      new() { ToolName = "get_store_health_score", RiskLevel = "Read", RequiresConfirmation = false, Description = "Điểm sức khỏe cửa hàng", Category = "Inventory" },
      new() { ToolName = "list_media", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê media", Category = "Media" },
      new() { ToolName = "get_media", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết media", Category = "Media" },
      new() { ToolName = "upload_media", RiskLevel = "Low", RequiresConfirmation = false, Description = "Upload media từ base64", Category = "Media" },
      new() { ToolName = "delete_media", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm media", Category = "Media" },

      // Reviews & Comments
      new() { ToolName = "list_recent_reviews", RiskLevel = "Read", RequiresConfirmation = false, Description = "Đánh giá gần đây", Category = "Reviews" },
      new() { ToolName = "list_recent_comments", RiskLevel = "Read", RequiresConfirmation = false, Description = "Bình luận gần đây", Category = "Reviews" },
      new() { ToolName = "reply_to_review", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Phản hồi đánh giá khách hàng", Category = "Reviews" },
      new() { ToolName = "reply_to_comment", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Phản hồi bình luận khách hàng", Category = "Reviews" },
      new() { ToolName = "list_reviews", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê đánh giá sản phẩm", Category = "Reviews" },
      new() { ToolName = "hide_review", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Ẩn đánh giá", Category = "Reviews" },
      new() { ToolName = "show_review", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Hiện lại đánh giá", Category = "Reviews" },
      new() { ToolName = "delete_review", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa vĩnh viễn đánh giá", Category = "Reviews" },

      // Promotions
      new() { ToolName = "list_promo_codes", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê mã khuyến mãi", Category = "Promotions" },
      new() { ToolName = "create_promo_code", RiskLevel = "High", RequiresConfirmation = true, Description = "Tạo mã khuyến mãi mới", Category = "Promotions" },
      new() { ToolName = "get_promo_code", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết mã khuyến mãi", Category = "Promotions" },
      new() { ToolName = "update_promo_code", RiskLevel = "High", RequiresConfirmation = true, Description = "Cập nhật mã khuyến mãi", Category = "Promotions" },
      new() { ToolName = "toggle_promo_code", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Bật/tắt mã khuyến mãi", Category = "Promotions" },
      new() { ToolName = "delete_promo_code", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm mã khuyến mãi", Category = "Promotions" },

      // Marketing & Subscribers
      new() { ToolName = "list_marketing_options", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê nội dung marketing có thể gắn vào email", Category = "Marketing" },
      new() { ToolName = "send_marketing_campaign", RiskLevel = "High", RequiresConfirmation = true, Description = "Gửi chiến dịch email marketing", Category = "Marketing" },
      new() { ToolName = "list_subscribers", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê người đăng ký email", Category = "Marketing" },
      new() { ToolName = "get_subscriber", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết người đăng ký email", Category = "Marketing" },
      new() { ToolName = "unsubscribe_subscriber", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Hủy đăng ký email", Category = "Marketing" },
      new() { ToolName = "delete_subscriber", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm người đăng ký email", Category = "Marketing" },
      new() { ToolName = "list_email_jobs", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê email jobs", Category = "Marketing" },
      new() { ToolName = "get_email_job", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết email job", Category = "Marketing" },
      new() { ToolName = "retry_email_job", RiskLevel = "Low", RequiresConfirmation = false, Description = "Retry email job", Category = "Marketing" },
      new() { ToolName = "cancel_email_job", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Hủy email job", Category = "Marketing" },
      new() { ToolName = "delete_email_job", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm email job", Category = "Marketing" },

      // Blog content
      new() { ToolName = "generate_blog_draft", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tạo bản nháp blog AI", Category = "Content" },
      new() { ToolName = "generate_blog_images", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo và upload ảnh blog bằng AI", Category = "Content" },
      new() { ToolName = "save_blog_draft", RiskLevel = "Low", RequiresConfirmation = false, Description = "Lưu bài viết nháp", Category = "Content" },
      new() { ToolName = "publish_blog_post", RiskLevel = "High", RequiresConfirmation = true, Description = "Xuất bản bài viết", Category = "Content" },
      new() { ToolName = "list_blog_posts", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê bài viết blog", Category = "Content" },
      new() { ToolName = "get_blog_post", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết bài viết blog", Category = "Content" },
      new() { ToolName = "update_blog_post", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật bài viết blog", Category = "Content" },
      new() { ToolName = "delete_blog_post", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa bài viết blog", Category = "Content" },

      // Purchase Note + Daily Report
      new() { ToolName = "create_purchase_note", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo ghi chú nhập hàng", Category = "Inventory" },
      new() { ToolName = "generate_daily_report", RiskLevel = "Read", RequiresConfirmation = false, Description = "Báo cáo doanh thu hôm nay", Category = "Intelligence" },

      // Facebook Page (social)
      new() { ToolName = "generate_facebook_post_plan", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tạo kế hoạch bài Facebook AI", Category = "Social" },
      new() { ToolName = "publish_facebook_post", RiskLevel = "High", RequiresConfirmation = true, Description = "Đăng bài lên Facebook Page", Category = "Social" },
      new() { ToolName = "delete_facebook_post", RiskLevel = "High", RequiresConfirmation = true, Description = "Gỡ/xóa bài viết Facebook", Category = "Social" },
      new() { ToolName = "list_facebook_pages", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê trang Facebook đã kết nối", Category = "Social" },
      new() { ToolName = "list_facebook_posts", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê bài đăng Facebook", Category = "Social" },
      new() { ToolName = "list_facebook_post_comments", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê bình luận bài Facebook", Category = "Social" },
      new() { ToolName = "reply_facebook_comment", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Trả lời bình luận Facebook bằng trang", Category = "Social" },
      new() { ToolName = "list_facebook_conversations", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê hội thoại Messenger fanpage", Category = "Social" },
      new() { ToolName = "list_facebook_conversation_messages", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê tin nhắn trong hội thoại Messenger", Category = "Social" },
      new() { ToolName = "send_facebook_message", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Gửi tin nhắn Messenger bằng fanpage", Category = "Social" },

      // Autonomy Mode
      new() { ToolName = "toggle_autonomy", RiskLevel = "High", RequiresConfirmation = true, Description = "Bật/tắt chế độ tự động", Category = "System" },
      new() { ToolName = "get_autonomy_status", RiskLevel = "Read", RequiresConfirmation = false, Description = "Trạng thái chế độ tự động", Category = "System" },

      // Intelligence
      new() { ToolName = "generate_product_description", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tạo mô tả sản phẩm bằng AI từ dữ liệu sản phẩm hiện có", Category = "Intelligence" },
      new() { ToolName = "generate_weekly_report", RiskLevel = "Read", RequiresConfirmation = false, Description = "Báo cáo tuần", Category = "Intelligence" },
      new() { ToolName = "check_inventory_alerts", RiskLevel = "Read", RequiresConfirmation = false, Description = "Cảnh báo tồn kho thấp", Category = "Intelligence" },
    ];
  }
}
