using System.Text.RegularExpressions;

namespace AoDaiNhaUyen.Api.Hermes;

public sealed class HermesAdminApiDescriptionRegistry
{
  private static readonly HermesResponseDescription Envelope = new(
    "{ success, message, data, errors, timestamp }",
    "See dataShape for this endpoint.");

  private static readonly IReadOnlyList<string> DefaultNotes =
  [
    "Gọi lại cùng URL không có X-Hermes-Describe để thực thi.",
    "Dùng Content-Type: application/json khi requestBody.contentType là application/json.",
    "Chỉ gửi field cần đổi nếu endpoint hỗ trợ partial update."
  ];

  private static readonly IReadOnlyList<string> MultipartNotes =
  [
    "Gọi lại cùng URL không có X-Hermes-Describe để thực thi.",
    "Dùng Content-Type: multipart/form-data.",
    "Gửi file trong field form-data tên `file`."
  ];

  private readonly IReadOnlyList<HermesAdminApiDescription> _descriptions = BuildDescriptions();

  public HermesAdminApiDescription? Find(string method, PathString path)
  {
    var normalizedPath = path.Value?.TrimEnd('/') ?? string.Empty;
    foreach (var description in _descriptions)
    {
      if (!string.Equals(description.Method, method, StringComparison.OrdinalIgnoreCase)) continue;
      if (IsMatch(description.Route, normalizedPath)) return description;
    }

    return null;
  }

  public IReadOnlyList<string> KnownRoutes() => _descriptions.Select(d => $"{d.Method} {d.Route}").ToArray();

  private static bool IsMatch(string route, string path)
  {
    var pattern = "^" + Regex.Replace(route.TrimEnd('/'), @"\{[^/]+\}", "[^/]+") + "$";
    return Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
  }

  private static IReadOnlyList<HermesAdminApiDescription> BuildDescriptions() =>
  [
    // Dashboard
    Get("/api/admin/dashboard/summary", "Lấy tổng quan dashboard.", "AdminDashboardSummaryDto"),
    Get("/api/admin/dashboard/revenue", "Lấy dữ liệu doanh thu theo thời gian.", "AdminRevenuePointDto[]", query: [Param("period", "int", false, "Số ngày, mặc định 30")]),
    Get("/api/admin/dashboard/orders-by-status", "Lấy thống kê đơn hàng theo trạng thái.", "AdminOrderStatusDistributionDto[]"),
    Get("/api/admin/dashboard/recent-orders", "Lấy đơn hàng gần đây.", "AdminRecentOrderDto[]", query: [Param("limit", "int", false, "Số bản ghi, mặc định 10")]),
    Get("/api/admin/dashboard/top-products", "Lấy sản phẩm bán chạy.", "AdminTopProductDto[]", query: [Param("limit", "int", false, "Số bản ghi, mặc định 5")]),
    Get("/api/admin/dashboard/user-growth", "Lấy tăng trưởng người dùng.", "AdminUserGrowthDto[]", query: [Param("period", "int", false, "Số ngày, mặc định 30")]),

    // Products
    Get("/api/admin/products", "Tìm/list sản phẩm admin.", "Paginated AdminProductListItemResponse[]", query: [Param("search", "string", false, "Từ khóa"), Param("status", "string", false, "Trạng thái"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang, tối đa 100"), Param("includeDeleted", "bool", false, "Bao gồm đã xóa")]),
    Get("/api/admin/products/{id}", "Lấy chi tiết sản phẩm.", "AdminProductDetailResponse", path: [Id("id", "ID sản phẩm")]),
    Post("/api/admin/products", "Tạo sản phẩm.", "AdminProductDetailResponse", ProductBody(create: true)),
    Put("/api/admin/products/{id}", "Cập nhật sản phẩm.", "AdminProductDetailResponse", ProductBody(create: false), path: [Id("id", "ID sản phẩm")]),
    Delete("/api/admin/products/{id}", "Xóa mềm sản phẩm.", "NoContent", path: [Id("id", "ID sản phẩm")]),
    Patch("/api/admin/products/{id}/restore", "Khôi phục sản phẩm đã xóa mềm.", "null", path: [Id("id", "ID sản phẩm")]),
    Patch("/api/admin/products/{id}/status", "Cập nhật trạng thái sản phẩm.", "null", Body([Field("status", "string", true, "active/inactive/draft")], new { status = "active" }), path: [Id("id", "ID sản phẩm")]),
    Patch("/api/admin/products/{productId}/variants/{variantId}/stock", "Cập nhật tồn kho biến thể.", "AdminProductDetailResponse", Body([Field("stockQty", "int", true, "Số lượng tồn kho")], new { stockQty = 10 }), path: [Id("productId", "ID sản phẩm"), Id("variantId", "ID biến thể")]),
    Post("/api/admin/products/{productId}/images/{imageId}/make-public", "Chuyển ảnh sản phẩm sang công khai.", "ProductImageVisibilityDto", path: [Id("productId", "ID sản phẩm"), Id("imageId", "ID ảnh")]),
    Post("/api/admin/products/{productId}/images/{imageId}/make-private", "Chuyển ảnh sản phẩm sang riêng tư.", "ProductImageVisibilityDto", path: [Id("productId", "ID sản phẩm"), Id("imageId", "ID ảnh")]),
    Post("/api/admin/products/{productId}/images", "Upload ảnh sản phẩm.", "AdminImageResponse", MultipartBody("file", "File ảnh"), path: [Id("productId", "ID sản phẩm")], notes: MultipartNotes),
    Delete("/api/admin/products/{productId}/images/{imageId}", "Xóa ảnh khỏi sản phẩm.", "null", path: [Id("productId", "ID sản phẩm"), Id("imageId", "ID ảnh")]),
    Put("/api/admin/products/{productId}/images/{imageId}/primary", "Đặt ảnh sản phẩm làm ảnh chính.", "null", path: [Id("productId", "ID sản phẩm"), Id("imageId", "ID ảnh")]),

    // Categories
    Get("/api/admin/categories", "List danh mục.", "AdminCategoryListItemResponse[]", query: [Param("includeDeleted", "bool", false, "Bao gồm đã xóa")]),
    Get("/api/admin/categories/{id}", "Lấy chi tiết danh mục.", "AdminCategoryDetailResponse", path: [Id("id", "ID danh mục")]),
    Post("/api/admin/categories", "Tạo danh mục.", "AdminCategoryDetailResponse", CategoryBody(create: true)),
    Put("/api/admin/categories/{id}", "Cập nhật danh mục.", "AdminCategoryDetailResponse", CategoryBody(create: false), path: [Id("id", "ID danh mục")]),
    Delete("/api/admin/categories/{id}", "Xóa mềm danh mục.", "NoContent", path: [Id("id", "ID danh mục")]),
    Patch("/api/admin/categories/{id}/restore", "Khôi phục danh mục đã xóa mềm.", "null", path: [Id("id", "ID danh mục")]),

    // Users
    Get("/api/admin/users", "List người dùng.", "Paginated AdminUserListItemDto[]", query: [Param("search", "string", false, "Từ khóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang"), Param("includeDeleted", "bool", false, "Bao gồm đã xóa")]),
    Get("/api/admin/users/{id}", "Lấy chi tiết người dùng.", "AdminUserListItemDto", path: [Id("id", "ID người dùng")]),
    Post("/api/admin/users", "Tạo người dùng.", "AdminUserListItemDto", UserCreateBody()),
    Put("/api/admin/users/{id}", "Cập nhật người dùng.", "AdminUserListItemDto", UserUpdateBody(), path: [Id("id", "ID người dùng")]),
    Delete("/api/admin/users/{id}", "Xóa mềm người dùng.", "NoContent", path: [Id("id", "ID người dùng")]),
    Patch("/api/admin/users/{id}/restore", "Khôi phục người dùng đã xóa mềm.", "null", path: [Id("id", "ID người dùng")]),
    Patch("/api/admin/users/{id}/role", "Cập nhật vai trò người dùng.", "null", Body([Field("roleId", "guid", true, "ID vai trò")], new { roleId = "00000000-0000-0000-0000-000000000000" }), path: [Id("id", "ID người dùng")]),
    Patch("/api/admin/users/{id}/status", "Cập nhật trạng thái người dùng.", "null", Body([Field("status", "string", true, "active/inactive/banned")], new { status = "active" }), path: [Id("id", "ID người dùng")]),

    // Orders
    Patch("/api/admin/orders/{orderId}/status", "Cập nhật trạng thái đơn hàng.", "OrderMutationResult", Body([Field("status", "string", true, "Trạng thái đơn hàng mới")], new { status = "confirmed" }), path: [Id("orderId", "ID đơn hàng")]),
    Post("/api/admin/orders/{orderId}/ship", "Tạo shipment cho đơn hàng.", "OrderMutationResult", Body([Field("carrier", "string", true, "Đơn vị vận chuyển"), Field("trackingNumber", "string", false, "Mã vận đơn")], new { carrier = "GHN", trackingNumber = "GHN123456" }), path: [Id("orderId", "ID đơn hàng")]),
    Patch("/api/admin/orders/shipments/{shipmentId}/status", "Cập nhật trạng thái shipment.", "OrderMutationResult", Body([Field("status", "string", true, "Trạng thái shipment")], new { status = "delivered" }), path: [Id("shipmentId", "ID shipment")]),

    // Inventory
    Get("/api/admin/inventory/low-stock", "Lấy danh sách sản phẩm sắp hết hàng.", "LowStockAlertDto[]", query: [Param("threshold", "int", false, "Ngưỡng tồn kho, mặc định 5")]),

    // LLM logs
    Get("/api/admin/llm-logs", "Tìm kiếm nhật ký LLM.", "Paginated LlmAuditLogListItemDto[]", query: [Param("search", "string", false, "Từ khóa"), Param("provider", "string", false, "Nhà cung cấp"), Param("model", "string", false, "Model"), Param("from", "datetime", false, "Từ ngày"), Param("to", "datetime", false, "Đến ngày"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/llm-logs/stats", "Thống kê nhật ký LLM.", "LlmAuditLogStatsDto", query: [Param("from", "datetime", false, "Từ ngày"), Param("to", "datetime", false, "Đến ngày")]),
    Get("/api/admin/llm-logs/{id}", "Chi tiết nhật ký LLM.", "LlmAuditLogDetailDto", path: [Id("id", "ID log")]),

    // Media
    Get("/api/admin/media", "List ảnh admin.", "Paginated AdminImageResponse[]", query: [Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang"), Param("sourceType", "string", false, "Nguồn ảnh"), Param("search", "string", false, "Từ khóa")]),
    Get("/api/admin/media/{id}", "Chi tiết ảnh.", "AdminImageResponse", path: [Id("id", "ID ảnh")]),
    Delete("/api/admin/media/{id}", "Xóa ảnh.", "bool", path: [Id("id", "ID ảnh")]),
    Get("/api/admin/media/stats", "Thống kê ảnh.", "AdminMediaStatsResponse"),

    // Promos
    Get("/api/admin/promos", "List mã khuyến mãi.", "Paginated PromoAdminListItemDto[]", query: [Param("includeDeleted", "bool", false, "Bao gồm đã xóa"), Param("search", "string", false, "Từ khóa"), Param("isActive", "bool", false, "Trạng thái"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/promos/{id}", "Chi tiết mã khuyến mãi.", "PromoAdminDetailDto", path: [Id("id", "ID mã")]),
    Get("/api/admin/promos/{id}/performance", "Hiệu suất mã khuyến mãi.", "PromoPerformanceDto", path: [Id("id", "ID mã")], query: [Param("from", "datetime", false, "Từ ngày"), Param("to", "datetime", false, "Đến ngày")]),
    Post("/api/admin/promos", "Tạo mã khuyến mãi.", "PromoAdminDetailDto", PromoCreateBody()),
    Put("/api/admin/promos/{id}", "Cập nhật mã khuyến mãi.", "PromoAdminDetailDto", PromoUpdateBody(), path: [Id("id", "ID mã")]),
    Patch("/api/admin/promos/{id}/status", "Bật/tắt mã khuyến mãi.", "null", Body([Field("isActive", "bool", true, "true để bật, false để tắt")], new { isActive = true }), path: [Id("id", "ID mã")]),
    Delete("/api/admin/promos/{id}", "Xóa mềm mã khuyến mãi.", "NoContent", path: [Id("id", "ID mã")]),
    Patch("/api/admin/promos/{id}/restore", "Khôi phục mã khuyến mãi.", "null", path: [Id("id", "ID mã")]),

    // Roles
    Get("/api/admin/roles", "List vai trò.", "AdminRoleDto[]"),
    Post("/api/admin/roles", "Tạo vai trò.", "AdminRoleDto", RoleBody(create: true)),
    Put("/api/admin/roles/{id}", "Cập nhật vai trò.", "AdminRoleDto", RoleBody(create: false), path: [Id("id", "ID vai trò")]),
    Delete("/api/admin/roles/{id}", "Xóa vai trò.", "NoContent", path: [Id("id", "ID vai trò")]),

    // Tool risk
    Get("/api/admin/tools-risk", "List cấu hình rủi ro công cụ.", "ToolRiskConfigDto[]"),
    Put("/api/admin/tools-risk/{id}", "Cập nhật mức rủi ro công cụ.", "ToolRiskConfigDto", ToolRiskBody(), path: [Id("id", "ID cấu hình")]),

    // Marketing: email templates
    Get("/api/admin/email-templates", "List mẫu email.", "Paginated EmailTemplateAdminDto[]", query: [Param("search", "string", false, "Từ khóa"), Param("includeDeleted", "bool", false, "Bao gồm đã xóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/email-templates/{id}", "Chi tiết mẫu email.", "EmailTemplateAdminDto", path: [Id("id", "ID mẫu")]),
    Post("/api/admin/email-templates", "Tạo mẫu email.", "EmailTemplateAdminDto", EmailTemplateBody(create: true)),
    Put("/api/admin/email-templates/{id}", "Cập nhật mẫu email.", "EmailTemplateAdminDto", EmailTemplateBody(create: false), path: [Id("id", "ID mẫu")]),
    Delete("/api/admin/email-templates/{id}", "Xóa mềm mẫu email.", "NoContent", path: [Id("id", "ID mẫu")]),
    Patch("/api/admin/email-templates/{id}/restore", "Khôi phục mẫu email.", "null", path: [Id("id", "ID mẫu")]),

    // Marketing: subscribers
    Get("/api/admin/subscribers", "List subscribers.", "Paginated SubscriberAdminDto[]", query: [Param("search", "string", false, "Từ khóa"), Param("status", "string", false, "Trạng thái"), Param("includeDeleted", "bool", false, "Bao gồm đã xóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/subscribers/{id}", "Chi tiết subscriber.", "SubscriberAdminDto", path: [Id("id", "ID subscriber")]),
    Patch("/api/admin/subscribers/{id}/unsubscribe", "Hủy đăng ký subscriber.", "null", path: [Id("id", "ID subscriber")]),
    Post("/api/admin/subscribers/import", "Import subscribers.", "ImportSubscribersResult", Body([Field("emails", "string[]", true, "Danh sách email"), Field("source", "string", false, "Nguồn import")], new { emails = new[] { "khach@example.com" }, source = "manual" })),

    // Marketing: email jobs
    Get("/api/admin/email-jobs", "List email jobs.", "Paginated EmailJobAdminDto[]", query: [Param("status", "string", false, "Trạng thái"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/email-jobs/{id}", "Chi tiết email job.", "EmailJobAdminDto", path: [Id("id", "ID job")]),
    Patch("/api/admin/email-jobs/{id}/retry", "Retry email job.", "null", path: [Id("id", "ID job")]),
    Patch("/api/admin/email-jobs/{id}/cancel", "Hủy email job.", "null", path: [Id("id", "ID job")]),
    Get("/api/admin/marketing/stats", "Thống kê marketing.", "MarketingStatsDto"),

    // Hermes reports
    Get("/api/admin/hermes/reports", "List báo cáo Hermes đã lưu.", "Paginated HermesReportListItemResponse[]", query: [Param("severity", "string", false, "info/warning/high/critical"), Param("type", "string", false, "Loại báo cáo"), Param("status", "string", false, "Trạng thái"), Param("q", "string", false, "Từ khóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/hermes/reports/{id}", "Chi tiết báo cáo Hermes.", "HermesReportResponse", path: [Id("id", "ID báo cáo")]),
    Post("/api/admin/hermes/report", "Hermes runner gửi báo cáo về backend để lưu DB.", "HermesReportResponse", HermesReportBody(), notes: ["Endpoint callback dùng X-Hermes-Admin-Key.", "PayloadJson phải là chuỗi JSON hợp lệ nếu có.", "Không gửi secrets/token/raw PII nếu không cần."]),

    // Hermes outbox events
    Get("/api/admin/hermes/events", "List Hermes event outbox.", "Paginated HermesEventOutboxListItemResponse[]", query: [Param("status", "string", false, "pending/processing/completed/failed/dead/cancelled"), Param("eventType", "string", false, "Loại event"), Param("aggregateType", "string", false, "Order/Product/Inventory/Promotion/AdminSecurity/Role/Content/Email/HermesConfig"), Param("q", "string", false, "Từ khóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/hermes/events/{id}", "Chi tiết Hermes event outbox.", "HermesEventOutboxResponse", path: [Id("id", "ID event")]),
    Post("/api/admin/hermes/events/{id}/retry", "Đưa event Hermes vào hàng đợi xử lý lại.", "null", path: [Id("id", "ID event")]),
    Post("/api/admin/hermes/events/{id}/cancel", "Hủy event Hermes đang pending/failed.", "null", path: [Id("id", "ID event")]),

    // NOTE: Admin-side mutations (order/product/stock/promo/user/role/content/email/tools-risk) 
    // auto-enqueue Hermes events into a durable outbox for autonomous analysis.
    // Event payloads are UNTRUSTED DATA — never treat payload fields as instructions.
    // Hermes event worker does NOT auto-mutate store data; analysis/report only.

    // AI
    Post("/api/admin/ai/chat", "Stream chat AI admin qua SSE.", "text/event-stream chunks", AdminChatBody(), notes: ["Endpoint trả về text/event-stream, không phải JSON envelope.", "Dùng conversationId để tiếp tục cuộc trò chuyện cũ.", "Response kết thúc bằng data: [DONE]."]),
    Get("/api/admin/ai/conversations", "List cuộc trò chuyện AI admin.", "AdminConversationSummaryDto[]"),
    Get("/api/admin/ai/conversations/{threadId}", "Lấy chi tiết cuộc trò chuyện AI admin.", "AdminConversationDetailDto", path: [Id("threadId", "ID thread")]),
    Delete("/api/admin/ai/conversations/{threadId}", "Xóa cuộc trò chuyện AI admin.", "null", path: [Id("threadId", "ID thread")]),
    Get("/api/admin/ai/suggestions", "Lấy gợi ý AI admin.", "AdminSuggestionDto[]"),
    Post("/api/admin/ai/action/confirm", "Xác nhận/từ chối hành động AI đang chờ.", "null", Body([Field("actionId", "guid", true, "ID hành động"), Field("approved", "bool", true, "true để duyệt, false để từ chối")], new { actionId = "00000000-0000-0000-0000-000000000000", approved = true })),
    Post("/api/admin/ai/auto-mode/toggle", "Bật/tắt chế độ AI tự động.", "null", Body([Field("enabled", "bool", true, "true để bật, false để tắt")], new { enabled = true })),
    Get("/api/admin/ai/auto-mode/status", "Lấy trạng thái chế độ AI tự động.", "{ isAutoMode }"),
    Get("/api/admin/ai/store-health", "Lấy điểm sức khỏe cửa hàng.", "StoreHealthScoreDto")
  ];

  private static HermesAdminApiDescription Get(string route, string purpose, string dataShape, IReadOnlyList<HermesParamDescription>? path = null, IReadOnlyList<HermesParamDescription>? query = null) =>
    Desc("GET", route, purpose, dataShape, null, path, query);

  private static HermesAdminApiDescription Post(string route, string purpose, string dataShape, HermesBodyDescription? body = null, IReadOnlyList<HermesParamDescription>? path = null, IReadOnlyList<string>? notes = null) =>
    Desc("POST", route, purpose, dataShape, body, path, null, notes);

  private static HermesAdminApiDescription Put(string route, string purpose, string dataShape, HermesBodyDescription? body = null, IReadOnlyList<HermesParamDescription>? path = null) =>
    Desc("PUT", route, purpose, dataShape, body, path, null);

  private static HermesAdminApiDescription Patch(string route, string purpose, string dataShape, HermesBodyDescription? body = null, IReadOnlyList<HermesParamDescription>? path = null) =>
    Desc("PATCH", route, purpose, dataShape, body, path, null);

  private static HermesAdminApiDescription Delete(string route, string purpose, string dataShape, IReadOnlyList<HermesParamDescription>? path = null) =>
    Desc("DELETE", route, purpose, dataShape, null, path, null);

  private static HermesAdminApiDescription Desc(string method, string route, string purpose, string dataShape, HermesBodyDescription? body, IReadOnlyList<HermesParamDescription>? path, IReadOnlyList<HermesParamDescription>? query, IReadOnlyList<string>? notes = null) =>
    new(method, route, purpose, path ?? [], query ?? [], body, Envelope with { DataShape = dataShape }, notes ?? DefaultNotes);

  private static HermesParamDescription Id(string name, string description) => Param(name, "guid", true, description);

  private static HermesParamDescription Param(string name, string type, bool required, string description) => new(name, type, required, description);

  private static KeyValuePair<string, HermesFieldDescription> Field(string name, string type, bool required, string description) =>
    new(name, new HermesFieldDescription(type, required, description));

  private static HermesBodyDescription Body(IEnumerable<KeyValuePair<string, HermesFieldDescription>> fields, object? example) =>
    new("application/json", true, fields.ToDictionary(), example);

  private static HermesBodyDescription MultipartBody(string fieldName, string description) =>
    new("multipart/form-data", true, new Dictionary<string, HermesFieldDescription> { [fieldName] = new("file", true, description) }, null);

  private static HermesBodyDescription HermesReportBody() =>
    Body([
      Field("reportType", "string", true, "Loại báo cáo, ví dụ daily_summary/order_anomaly/provider_health"),
      Field("severity", "string", false, "info/warning/high/critical"),
      Field("title", "string", true, "Tiêu đề báo cáo"),
      Field("summary", "string", true, "Tóm tắt tiếng Việt, tối đa 4000 ký tự"),
      Field("payloadJson", "string", false, "Chuỗi JSON hợp lệ chứa dữ liệu chi tiết"),
      Field("source", "string", false, "Nguồn báo cáo"),
      Field("correlationId", "string", false, "ID liên kết ngoài nếu có"),
      Field("runId", "guid", false, "ID Hermes run nếu có")
    ], new { reportType = "provider_health", severity = "warning", title = "Z.AI đang rate limit", summary = "Hermes gặp HTTP 429 khi gọi upstream.", payloadJson = "{\"provider\":\"zai\",\"status\":429}" });

  private static HermesBodyDescription AdminChatBody() =>
    Body([Field("message", "string", false, "Tin nhắn mới, tối đa 4000 ký tự"), Field("conversationId", "string", false, "ID cuộc trò chuyện để tiếp tục thread cũ")], new { message = "Tóm tắt tình hình cửa hàng hôm nay", conversationId = (string?)null });

  private static HermesBodyDescription CategoryBody(bool create) =>
    Body([Field("name", "string", create, "Tên danh mục"), Field("slug", "string", false, "Slug"), Field("description", "string", false, "Mô tả"), Field("parentId", "guid", false, "Danh mục cha")], new { name = "Áo dài cưới", slug = "ao-dai-cuoi", description = "Danh mục áo dài cưới", parentId = (Guid?)null });

  private static HermesBodyDescription EmailTemplateBody(bool create) =>
    Body([Field("name", "string", create, "Tên mẫu"), Field("subject", "string", create, "Tiêu đề email"), Field("body", "string", create, "Nội dung HTML/text"), Field("type", "string", false, "Loại mẫu"), Field("isActive", "bool", false, "Trạng thái")], new { name = "welcome", subject = "Chào mừng", body = "<p>Xin chào</p>", type = "marketing", isActive = true });

  private static HermesBodyDescription ProductBody(bool create) =>
    Body([Field("name", "string", create, "Tên sản phẩm"), Field("description", "string", false, "Mô tả"), Field("price", "number", create, "Giá bán"), Field("categoryId", "guid", create, "ID danh mục"), Field("status", "string", false, "draft/active/inactive")], new { name = "Áo dài lụa đỏ", description = "Áo dài cao cấp", price = 1200000, categoryId = "00000000-0000-0000-0000-000000000000", status = "draft" });

  private static HermesBodyDescription PromoCreateBody() =>
    Body([Field("code", "string", true, "Mã khuyến mãi"), Field("description", "string", false, "Mô tả"), Field("discountType", "string", true, "percent/fixed"), Field("discountValue", "number", true, "Giá trị giảm"), Field("startAt", "datetime", false, "Ngày bắt đầu"), Field("endAt", "datetime", false, "Ngày kết thúc"), Field("isActive", "bool", false, "Trạng thái")], new { code = "SALE10", description = "Giảm 10%", discountType = "percent", discountValue = 10, isActive = true });

  private static HermesBodyDescription PromoUpdateBody() =>
    Body([Field("description", "string", false, "Mô tả"), Field("discountType", "string", false, "percent/fixed"), Field("discountValue", "number", false, "Giá trị giảm"), Field("startAt", "datetime", false, "Ngày bắt đầu"), Field("endAt", "datetime", false, "Ngày kết thúc"), Field("isActive", "bool", false, "Trạng thái")], new { description = "Giảm 15%", discountType = "percent", discountValue = 15, isActive = true });

  private static HermesBodyDescription RoleBody(bool create) =>
    Body([Field("name", "string", create, "Tên vai trò"), Field("description", "string", false, "Mô tả")], new { name = "manager", description = "Quản lý cửa hàng" });

  private static HermesBodyDescription ToolRiskBody() =>
    Body([Field("riskLevel", "string", true, "Read/Low/Medium/High/Critical"), Field("requiresConfirmation", "bool", true, "Có yêu cầu xác nhận không"), Field("description", "string", false, "Mô tả")], new { riskLevel = "High", requiresConfirmation = true, description = "Cần xác nhận" });

  private static HermesBodyDescription UserCreateBody() =>
    Body([Field("email", "string", true, "Email"), Field("fullName", "string", true, "Họ tên"), Field("phone", "string", false, "Số điện thoại"), Field("password", "string", true, "Mật khẩu"), Field("roleId", "guid", true, "ID vai trò")], new { email = "admin@example.com", fullName = "Admin Hermes", phone = "0900000000", password = "ChangeMe123!", roleId = "00000000-0000-0000-0000-000000000000" });

  private static HermesBodyDescription UserUpdateBody() =>
    Body([Field("email", "string", false, "Email"), Field("fullName", "string", false, "Họ tên"), Field("phone", "string", false, "Số điện thoại")], new { email = "user@example.com", fullName = "Nguyễn Văn A", phone = "0900000000" });
}
