using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminToolRiskService(AppDbContext dbContext, ISafetyGate safetyGate) : IAdminToolRiskService
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

    await dbContext.SaveChangesAsync(ct);

    // Invalidate SafetyGate cache so new config takes effect immediately
    await safetyGate.InvalidateCacheAsync(ct);

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

  private static List<ToolRiskConfig> GetDefaultToolConfigs()
  {
    return
    [
      // Dashboard
      new() { ToolName = "get_dashboard_summary", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tổng quan dashboard", Category = "Dashboard" },
      new() { ToolName = "get_revenue", RiskLevel = "Read", RequiresConfirmation = false, Description = "Dữ liệu doanh thu", Category = "Dashboard" },
      new() { ToolName = "get_orders_by_status", RiskLevel = "Read", RequiresConfirmation = false, Description = "Phân phối đơn theo trạng thái", Category = "Dashboard" },
      new() { ToolName = "get_recent_orders", RiskLevel = "Read", RequiresConfirmation = false, Description = "Đơn hàng gần đây", Category = "Dashboard" },
      new() { ToolName = "get_top_products", RiskLevel = "Read", RequiresConfirmation = false, Description = "Sản phẩm bán chạy", Category = "Dashboard" },

      // Products
      new() { ToolName = "list_products", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê sản phẩm", Category = "Products" },
      new() { ToolName = "get_product", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết sản phẩm", Category = "Products" },
      new() { ToolName = "create_product", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo sản phẩm mới (nháp)", Category = "Products" },
      new() { ToolName = "update_product", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Cập nhật sản phẩm", Category = "Products" },
      new() { ToolName = "delete_product", RiskLevel = "High", RequiresConfirmation = true, Description = "Xóa mềm sản phẩm", Category = "Products" },
      new() { ToolName = "toggle_product_status", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Bật/tắt trạng thái sản phẩm", Category = "Products" },

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

      // Orders
      new() { ToolName = "list_orders", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê đơn hàng", Category = "Orders" },
      new() { ToolName = "get_order", RiskLevel = "Read", RequiresConfirmation = false, Description = "Chi tiết đơn hàng", Category = "Orders" },
      new() { ToolName = "confirm_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Xác nhận đơn hàng", Category = "Orders" },
      new() { ToolName = "start_processing_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Bắt đầu xử lý đơn", Category = "Orders" },
      new() { ToolName = "ship_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Tạo shipment", Category = "Orders" },
      new() { ToolName = "cancel_order", RiskLevel = "High", RequiresConfirmation = true, Description = "Hủy đơn hàng", Category = "Orders" },

      // Inventory & Store Health
      new() { ToolName = "get_inventory_summary", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tồn kho tổng quan", Category = "Inventory" },
      new() { ToolName = "get_store_health_score", RiskLevel = "Read", RequiresConfirmation = false, Description = "Điểm sức khỏe cửa hàng", Category = "Inventory" },

      // Reviews & Comments
      new() { ToolName = "list_recent_reviews", RiskLevel = "Read", RequiresConfirmation = false, Description = "Đánh giá gần đây", Category = "Reviews" },
      new() { ToolName = "list_recent_comments", RiskLevel = "Read", RequiresConfirmation = false, Description = "Bình luận gần đây", Category = "Reviews" },
      new() { ToolName = "reply_to_review", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Phản hồi đánh giá khách hàng", Category = "Reviews" },
      new() { ToolName = "reply_to_comment", RiskLevel = "Medium", RequiresConfirmation = true, Description = "Phản hồi bình luận khách hàng", Category = "Reviews" },

      // Promotions
      new() { ToolName = "list_promo_codes", RiskLevel = "Read", RequiresConfirmation = false, Description = "Liệt kê mã khuyến mãi", Category = "Promotions" },
      new() { ToolName = "create_promo_code", RiskLevel = "High", RequiresConfirmation = true, Description = "Tạo mã khuyến mãi mới", Category = "Promotions" },

      // Blog content
      new() { ToolName = "generate_blog_draft", RiskLevel = "Read", RequiresConfirmation = false, Description = "Tạo bản nháp blog AI", Category = "Content" },

      // Purchase Note + Daily Report
      new() { ToolName = "create_purchase_note", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo ghi chú nhập hàng", Category = "Inventory" },
      new() { ToolName = "generate_daily_report", RiskLevel = "Read", RequiresConfirmation = false, Description = "Báo cáo doanh thu hôm nay", Category = "Intelligence" },

      // Autonomy Mode
      new() { ToolName = "toggle_autonomy", RiskLevel = "High", RequiresConfirmation = true, Description = "Bật/tắt chế độ tự động", Category = "System" },
      new() { ToolName = "get_autonomy_status", RiskLevel = "Read", RequiresConfirmation = false, Description = "Trạng thái chế độ tự động", Category = "System" },

      // Intelligence
      new() { ToolName = "generate_product_description", RiskLevel = "Low", RequiresConfirmation = false, Description = "Tạo mô tả sản phẩm bằng AI", Category = "Intelligence" },
      new() { ToolName = "generate_weekly_report", RiskLevel = "Read", RequiresConfirmation = false, Description = "Báo cáo tuần", Category = "Intelligence" },
      new() { ToolName = "check_inventory_alerts", RiskLevel = "Read", RequiresConfirmation = false, Description = "Cảnh báo tồn kho thấp", Category = "Intelligence" },
    ];
  }
}
