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

    // Reviews
    Get("/api/admin/reviews", "List đánh giá để Hermes kiểm tra nội dung, rating, productId và reviewId trước khi phản hồi.", "Paginated AdminReviewModerationItem[]", query: [Param("search", "string", false, "Từ khóa nội dung/khách/sản phẩm/email"), Param("rating", "int", false, "Số sao 1-5"), Param("isVisible", "bool", false, "Trạng thái hiển thị"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang, tối đa 100")]),
    Get("/api/admin/reviews/recovery-stats", "Thống kê chăm sóc đánh giá xấu: tỷ lệ đã phản hồi, thời gian phản hồi đầu tiên, số quá SLA. Đây là recovery action/response coverage, không phải true resolution.", "BadReviewRecoveryStats", query: [Param("days", "int", false, "Khoảng ngày, mặc định 30"), Param("slaHours", "double", false, "SLA phản hồi tính theo giờ, mặc định 4")]),
    Post("/api/admin/reviews/{id}/reply", "Trả lời một đánh giá/bình luận bằng tài khoản Hermes admin; tạo child comment công khai.", "AdminReplyResult", ReviewReplyBody(), path: [Id("id", "ID review/comment gốc")], notes: ["Risk: low/medium brand impact. Chỉ phản hồi khi có reviewId/commentId và productId thật.", "Nội dung phản hồi phải lịch sự, đúng giọng Áo Dài Nhã Uyên, không hứa hoàn tiền/khuyến mãi nếu chưa có policy rõ.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Patch("/api/admin/reviews/{id}/visibility", "Ẩn/hiển thị đánh giá cho moderation.", "AdminReviewActionResult", Body([Field("isVisible", "bool", true, "true để hiển thị, false để ẩn")], new { isVisible = true }), path: [Id("id", "ID review")]),
    Delete("/api/admin/reviews/{id}", "Xóa đánh giá/bình luận khỏi hệ thống.", "AdminReviewActionResult", path: [Id("id", "ID review")]),

    // Social
    Get("/api/admin/social/analytics", "Lấy thống kê social đã kết nối qua Zernio. Dùng cho event social_metrics_snapshot_created/social_engagement_anomaly; không chứa token, PII người bình luận hay nội dung tin nhắn.", "SocialAnalyticsDto", query: [Param("platform", "string", false, "facebook/instagram/tiktok, mặc định facebook"), Param("fromDate", "date", false, "Ngày bắt đầu yyyy-MM-dd"), Param("toDate", "date", false, "Ngày kết thúc yyyy-MM-dd")], notes: ["Risk: medium vì là KPI marketing nội bộ.", "Không công khai raw impressions/clicks/CTR/spend cho customer chat.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),

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
    Post("/api/admin/email-templates", "Tạo mẫu email (DEPRECATED).", "EmailTemplateAdminDto", EmailTemplateBody(create: true), notes: NonExecutableEmailTemplateNotes),
    Put("/api/admin/email-templates/{id}", "Cập nhật mẫu email (DEPRECATED).", "EmailTemplateAdminDto", EmailTemplateBody(create: false), path: [Id("id", "ID mẫu")], notes: NonExecutableEmailTemplateNotes),
    Delete("/api/admin/email-templates/{id}", "Xóa mềm mẫu email (DEPRECATED).", "NoContent", path: [Id("id", "ID mẫu")], notes: NonExecutableEmailTemplateNotes),
    Patch("/api/admin/email-templates/{id}/restore", "Khôi phục mẫu email (DEPRECATED).", "null", path: [Id("id", "ID mẫu")], notes: NonExecutableEmailTemplateNotes),

    // Marketing: subscribers
    Get("/api/admin/subscribers", "List subscribers.", "Paginated SubscriberAdminDto[]", query: [Param("search", "string", false, "Từ khóa"), Param("status", "string", false, "Trạng thái"), Param("includeDeleted", "bool", false, "Bao gồm đã xóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/subscribers/{id}", "Chi tiết subscriber.", "SubscriberAdminDto", path: [Id("id", "ID subscriber")]),
    Patch("/api/admin/subscribers/{id}/unsubscribe", "Hủy đăng ký subscriber.", "null", path: [Id("id", "ID subscriber")]),
    Post("/api/admin/subscribers/import", "Import subscribers.", "ImportSubscribersResult", Body([Field("emails", "string[]", true, "Danh sách email"), Field("source", "string", false, "Nguồn import")], new { emails = new[] { "khach@example.com" }, source = "manual" })),

    // Marketing: email jobs
    Get("/api/admin/email-jobs", "List email jobs.", "Paginated EmailJobAdminDto[]", query: [Param("status", "string", false, "Trạng thái"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/email-jobs/{id}", "Chi tiết email job.", "EmailJobAdminDto", path: [Id("id", "ID job")]),
    Post("/api/admin/email-jobs", "Tạo một email job cho một khách hàng; dùng để gửi cảm ơn ngay hoặc lên lịch khảo sát sau 14 ngày.", "QueueSingleEmailJobResponse", SingleEmailJobBody(), notes: ["Risk: medium vì gửi email ra ngoài hệ thống.", "Bắt buộc có customerId hoặc orderId để backend tự xác minh email; không gửi tới email tùy ý.", "toEmail nếu gửi phải khớp email của customer/order nguồn.", "Bắt buộc idempotencyKey ổn định để tránh gửi trùng khi Hermes retry.", "Dùng templateKey hermes.single_email nếu chưa có template riêng.", "Survey follow-up nên đặt scheduledAt = now + 14 days và purpose = survey; backend yêu cầu khách đã opt-in email.", "Không hứa mã giảm giá/hoàn tiền nếu không có policy rõ.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Patch("/api/admin/email-jobs/{id}/retry", "Retry email job.", "null", path: [Id("id", "ID job")]),
    Patch("/api/admin/email-jobs/{id}/cancel", "Hủy email job.", "null", path: [Id("id", "ID job")]),
    Get("/api/admin/marketing/stats", "Thống kê marketing.", "MarketingStatsDto"),

    // Blog (full lifecycle — content, SEO, image upload, moderation)
    Get("/api/v1/admin/blog", "List bài blog admin; dùng để tìm postId/slug trước khi update/SEO.", "Paginated BlogPostListItemDto[]", query: [Param("search", "string", false, "Từ khóa tiêu đề"), Param("status", "string", false, "draft/published/archived"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang, tối đa 100")]),
    Get("/api/v1/admin/blog/categories", "List danh mục blog; dùng để lấy BlogCategoryId thật cho create/update.", "BlogCategoryDto[]"),
    Get("/api/v1/admin/blog/{id}", "Chi tiết bài blog admin; đọc Content blocks + SEO trước khi sửa.", "BlogPostDto", path: [Id("id", "ID bài blog")]),
    Post("/api/v1/admin/blog", "Tạo bài blog mới (upload bài post).", "BlogPostDto", BlogCreateBody(), notes: ["Risk: medium vì tạo nội dung public.", "Title + Excerpt + Content là bắt buộc; Content là JSON content blocks (kiểu BlogBlock), không phải text thuần.", "BlogCategoryId phải là ID thật từ GET /api/v1/admin/blog/categories; không bịia GUID.", "Trước khi publish, upload featured image qua POST /api/v1/admin/blog/upload rồi dùng imageUrl trả về làm FeaturedImage.", "Status: draft (mặc định, an toàn) hoặc published (cần ReviewedBy/InformationGain cho E-E-A-T).", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Put("/api/v1/admin/blog/{id}", "Cập nhật nội dung bài blog (tiêu đề, excerpt, content blocks, category, status).", "BlogPostDto", BlogUpdateBody(), path: [Id("id", "ID bài blog")], notes: ["Risk: medium vì thay đổi nội dung public.", "Toàn bộ trường required (Title/Excerpt/Content) phải gửi đầy đủ; endpoint là full-update, không phải partial.", "Status published yêu cầu ReviewedBy + InformationGain cho E-E-A-T.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/v1/admin/blog/upload", "Upload ảnh blog (multipart); trả về imageUrl để dùng cho FeaturedImage hoặc trong Content blocks.", "BlogImageUploadResponse", MultipartBody("file", "File ảnh blog (jpg/png/gif/webp, tối đa 8MB)"), notes: ["Risk: low.", "Dùng Content-Type: multipart/form-data; gửi file trong field form-data tên `file`.", "Sau khi upload, dùng `imageUrl` từ response làm FeaturedImage trong POST/PUT /api/v1/admin/blog.", "Không bịia FeaturedImage URL; phải khớp kết quả upload thật.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Put("/api/v1/admin/blog/{id}/seo", "Cập nhật metadata SEO/E-E-A-T cho một bài blog đã có. Dùng cho event blog_seo_opportunity; chỉ gửi field cần đổi.", "BlogPostDto", BlogSeoBody(), path: [Id("id", "ID bài blog")], notes: ["Risk: medium vì thay đổi nội dung SEO public.", "Endpoint partial-update: field null/không gửi sẽ giữ nguyên giá trị hiện tại.", "Không bịa route khác; dùng đúng /api/v1/admin/blog/{id}/seo cho UPDATE_BLOG_SEO.", "Nếu thiếu postId/contentId thật từ event payload thì actions phải là [].", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Delete("/api/v1/admin/blog/{id}", "Xóa bài blog (soft delete).", "NoContent", path: [Id("id", "ID bài blog")], notes: ["Risk: high vì xóa nội dung public.", "Chỉ dùng khi admin yêu cầu rõ trong chat; cân nhắc Status=archived thay vì delete.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),

    // Product variants (create/update — stock PATCH đã có phía trên)
    Post("/api/admin/products/{productId}/variants", "Tạo biến thể sản phẩm (size/màu/sku/giá/tồn).", "AdminProductDetailResponse", VariantCreateBody(), path: [Id("productId", "ID sản phẩm")], notes: ["Risk: medium vì ảnh hưởng catalog + giá.", "Sku phải duy nhất; không bịia Sku đã tồn tại.", "Status: active/inactive/draft.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Put("/api/admin/products/{productId}/variants/{variantId}", "Cập nhật biến thể sản phẩm.", "AdminProductDetailResponse", VariantUpdateBody(), path: [Id("productId", "ID sản phẩm"), Id("variantId", "ID biến thể")], notes: ["Risk: medium vì đổi giá/tồn có thể ảnh hưởng đơn đang chờ.", "Full-update: gửi đủ Sku/Price/StockQty/Status.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),

    // Marketing campaigns (campaign send là bulk email, risk cao)
    Get("/api/admin/marketing/content-options", "Lấy content options (templates/posts/promos) khả dụng cho chiến dịch email.", "MarketingContentOption[]"),
    Post("/api/admin/marketing/campaigns/send", "Gửi chiến dịch email marketing tới subscriber hoặc danh sách manual.", "MarketingCampaignSendResult", CampaignSendBody(), notes: ["Risk: HIGH — bulk email ra ngoài hệ thống, có thể spam nếu sai.", "RecipientMode: subscribers (cần SubscriberIds thật) hoặc manual (cần ManualEmails opt-in).", "TemplateKey phải là template thật đang active; Subject/Body phải khớp template.", "Khi ScheduledAt null → gửi ngay; đặt ScheduledAt để lên lịch.", "Không bịia SubscriberIds/TemplateKey; backend sẽ skip nhưng vẫn tốn tài nguyên.", "Chỉ gửi khi admin yêu cầu rõ; cân nhắc test segment nhỏ trước.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),

    // Facebook Page management (token được mã hóa DataProtection; KHÔNG bao giờ in raw token)
    Get("/api/admin/facebook/connections", "List page Facebook đã kết nối; chỉ expose TokenLast4, không bao giờ lộ raw token.", "FacebookConnectionDto[]", notes: ["Risk: low (read).", "Page Access Token được mã hóa DataProtection trong DB; response chỉ có TokenLast4.", "Không bao giờ log raw token, secrets, PII."]),
    Post("/api/admin/facebook/connections", "Kết nối page Facebook bằng PageAccessToken thu thập từ admin.", "FacebookConnectionDto", FacebookConnectBody(), notes: ["Risk: high vì gắn Page Access Token vào hệ thống.", "PageAccessToken là SECRET — gửi đúng một lần qua X-Hermes-Admin-Key; KHÔNG bao giờ in giá trị trong response/report/log.", "Backend sẽ mã hóa và chỉ lưu TokenLast4; không lưu raw.", "PageId phải là ID số thật của Facebook Page; không bịia.", "Sau khi connect, validate qua GET /api/admin/facebook/{pageId}/info.", "Auto-execute: blocked — endpoint này phơi bày secret, yêu cầu admin tự gọi.", "Nếu được yêu cầu tạo action, dùng actions: []."]),
    Delete("/api/admin/facebook/connections/{pageId}", "Gỡ kết nối page Facebook (revoke token trong DB).", "FacebookDeleteResultDto", path: [StringId("pageId", "Facebook Page ID (chuỗi số)")], notes: ["Risk: high vì mất khả năng đăng/quản lý page.", "Xóa connection không xóa bài đã đăng trên Facebook.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Get("/api/admin/facebook/oauth-url", "Lấy URL OAuth Facebook để admin cấp quyền page (2-bước; không tự execute).", "FacebookOAuthUrlDto", query: [Param("redirectUri", "string", true, "URL callback sau OAuth"), Param("state", "string", true, "State anti-CSRF")], notes: ["Setup 2-bước.", "Admin mở URL trong browser, không tự gọi qua API agent."]),
    Post("/api/admin/facebook/oauth/pages", "List page Facebook có thể kết nối sau khi admin OAuth (setup 2-bước).", "FacebookOAuthPageDto[]", FacebookOAuthPagesBody(), notes: ["Setup 2-bước.", "Code là Facebook OAuth code từ bước trước; không tự execute, admin chủ động.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/admin/facebook/connections/oauth", "Kết nối page qua OAuth code (setup 2-bước).", "FacebookConnectionDto", FacebookOAuthConnectBody(), notes: ["Risk: high vì gắn token.", "Setup 2-bước sau OAuth; không tự execute, admin chủ động.", "PageId + ConnectToken phải là giá trị thật từ bước OAuth.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Get("/api/admin/facebook/{pageId}/info", "Thông tin page Facebook (tên, category, link).", "FacebookPageInfoDto", path: [StringId("pageId", "Facebook Page ID (chuỗi số)")]),
    Get("/api/admin/facebook/{pageId}/posts", "List bài đã đăng trên page Facebook.", "FacebookPostListDto", path: [StringId("pageId", "Facebook Page ID (chuỗi số)")], query: [Param("limit", "int", false, "Số bài, mặc định 25"), Param("cursor", "string", false, "Cursor trang kế")]),
    Post("/api/admin/facebook/{pageId}/posts", "Đăng bài text/link lên page Facebook.", "FacebookPublishResultDto", FacebookPostBody(), path: [StringId("pageId", "Facebook Page ID (chuỗi số)")], notes: ["Risk: high vì publish công khai lên page.", "Message phải lịch sự, đúng giọng Áo Dài Nhà Uyên; không spam/nội dung nhạy cảm.", "Published=true để đăng ngay; ScheduledPublishTime để lên lịch.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/admin/facebook/{pageId}/photos", "Đăng ảnh lên page Facebook (multipart).", "FacebookPublishResultDto", MultipartBody("file", "File ảnh (jpg/png/gif/webp, tối đa 10MB)"), path: [StringId("pageId", "Facebook Page ID (chuỗi số)")], notes: FacebookPhotoNotes),
    Post("/api/admin/facebook/{pageId}/videos", "Đăng video lên page Facebook (multipart).", "FacebookPublishResultDto", MultipartBody("file", "File video (mp4/mov/webm, tối đa 200MB)"), path: [StringId("pageId", "Facebook Page ID (chuỗi số)")], notes: FacebookVideoNotes),
    Put("/api/admin/facebook/posts/{postId}", "Cập nhật nội dung text bài đăng Facebook.", "FacebookPostDto", FacebookUpdatePostBody(), path: [StringId("postId", "Facebook Post ID (chuỗi số)")], notes: ["Risk: medium.", "Chỉ cập nhật được Message; không đổi ảnh/video đã đăng.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Delete("/api/admin/facebook/posts/{postId}", "Xóa bài đăng Facebook.", "FacebookDeleteResultDto", path: [StringId("postId", "Facebook Post ID (chuỗi số)")], notes: ["Risk: high vì xóa public, không hoàn tác được trên Facebook.", "Chỉ khi admin yêu cầu rõ; cân nhắc ẩn thay vì xóa.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Get("/api/admin/facebook/{pageId}/posts/{postId}/comments", "List bình luận trên bài Facebook.", "FacebookPostCommentListDto", path: [StringId("pageId", "Facebook Page ID"), StringId("postId", "Facebook Post ID")], query: [Param("after", "string", false, "Cursor"), Param("limit", "int", false, "Số bình luận, mặc định 25")]),
    Post("/api/admin/facebook/{pageId}/posts/{postId}/comments", "Bình luận vào bài Facebook bằng page.", "FacebookCommentActionResultDto", FacebookCommentBody(), path: [StringId("pageId", "Facebook Page ID"), StringId("postId", "Facebook Post ID")], notes: ["Risk: medium vì bình luận công khai đại diện page.", "Message lịch sự, đúng giọng thương hiệu; không tranh cãi với khách.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/admin/facebook/{pageId}/comments/{commentId}/replies", "Trả lời bình luận Facebook bằng page.", "FacebookCommentActionResultDto", FacebookCommentReplyBody(), path: [StringId("pageId", "Facebook Page ID"), StringId("commentId", "Facebook Comment ID")], notes: ["Risk: medium.", "Reply ngắn, ấm, lịch sự; cảm ơn khách khi tích cực, xin lỗi + giải quyết khi tiêu cực.", "postId là bắt buộc: dùng ThreadId/postId từ event social_comment_received hoặc từ GET comments.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Patch("/api/admin/facebook/{pageId}/comments/{commentId}/visibility", "Ẩn/hiện bình luận Facebook.", "FacebookCommentActionResultDto", FacebookToggleHiddenBody(), path: [StringId("pageId", "Facebook Page ID"), StringId("commentId", "Facebook Comment ID")], notes: ["Risk: medium.", "IsHidden=true để ẩn bình luận tiêu cực/spam khỏi public; không xóa để giữ audit.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Delete("/api/admin/facebook/{pageId}/comments/{commentId}", "Xóa bình luận Facebook.", "FacebookCommentActionResultDto", path: [StringId("pageId", "Facebook Page ID"), StringId("commentId", "Facebook Comment ID")], notes: ["Risk: high vì xóa public, không hoàn tác.", "Ưu tiên ẩn (PATCH visibility) thay vì xóa khi bình luận tiêu cực/spam.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Get("/api/admin/facebook/{pageId}/conversations", "List hội thoại inbox Facebook.", "FacebookConversationListDto", path: [StringId("pageId", "Facebook Page ID")], query: [Param("limit", "int", false, "Số hội thoại, mặc định 25")]),
    Get("/api/admin/facebook/{pageId}/conversations/{conversationId}/messages", "List tin nhắn trong hội thoại Facebook.", "FacebookMessageListDto", path: [StringId("pageId", "Facebook Page ID"), StringId("conversationId", "Facebook Conversation ID")], query: [Param("limit", "int", false, "Số tin nhắn")]),
    Post("/api/admin/facebook/{pageId}/conversations/{conversationId}/messages", "Gửi tin nhắn vào hội thoại Facebook.", "FacebookMessageSendResultDto", FacebookMessageBody(), path: [StringId("pageId", "Facebook Page ID"), StringId("conversationId", "Facebook Conversation ID")], notes: ["Risk: medium vì gửi tin nhắn ra ngoài.", "Text lịch sự, đúng giọng thương hiệu; không gửi link lạ/mã giảm giá nếu chưa có policy.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/admin/facebook/{pageId}/conversations/{conversationId}/read", "Đánh dấu hội thoại Facebook đã đọc.", "MarkConversationReadResultDto", path: [StringId("pageId", "Facebook Page ID"), StringId("conversationId", "Facebook Conversation ID")], notes: ["Risk: low.", "Sau khi trả lời khách, mark-read để đồng bộ trạng thái inbox.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),

    // Social / Zernio unified inbox + cross-platform posting
    Get("/api/admin/social/accounts", "List tài khoản social đã kết nối (Facebook qua Zernio).", "SocialAccountConnectionDto[]", query: [Param("platform", "string", false, "facebook/instagram/tiktok"), Param("sync", "bool", false, "true để force sync từ Zernio"), Param("profileId", "string", false, "Zernio profile ID")]),
    Get("/api/admin/social/connect-url", "Lấy URL kết nối Zernio để admin cấp quyền (setup 2-bước; không tự execute).", "SocialConnectUrlDto", query: [Param("platform", "string", true, "facebook/instagram/tiktok"), Param("profileId", "string", true, "Zernio profile ID"), Param("redirectUrl", "string", true, "URL callback"), Param("headless", "bool", false, "true cho headless flow")], notes: ["Setup 2-bước.", "Admin mở URL trong browser, không tự gọi qua API agent."]),
    Post("/api/admin/social/facebook/pages/select", "Chọn page Facebook để kết nối qua Zernio (setup 2-bước; không tự execute).", "SocialAccountConnectionDto[]", SocialSelectPageBody(), notes: ["Risk: high vì gắn token.", "Setup 2-bước sau khi admin mở connect-url trong browser; không tự execute.", "ProfileId/PageId/TempToken phải là giá trị thật từ bước connect.", "Nếu được yêu cầu tạo action, dùng actions: []."]),
    Delete("/api/admin/social/accounts/{id}", "Gỡ kết nối tài khoản social.", "null", path: [Id("id", "ID kết nối social (Guid)")], notes: ["Risk: high vì mất khả năng đăng/quản lý kênh.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Get("/api/admin/social/posts", "List bài social đã lên lịch/đã đăng (đa nền tảng).", "SocialPostListDto", query: [Param("platform", "string", false, "Lọc theo nền tảng"), Param("accountId", "guid", false, "Lọc theo tài khoản"), Param("profileId", "string", false, "Zernio profile ID"), Param("page", "int", false, "Trang, mặc định 1"), Param("limit", "int", false, "Số bài, mặc định 25")]),
    Get("/api/admin/social/posts/{postId}", "Chi tiết bài social.", "SocialPostDto", path: [StringId("postId", "Social Post ID")]),
    Post("/api/admin/social/posts", "Tạo bài social đa nền tảng (lên lịch hoặc đăng ngay).", "SocialPostDto", SocialPostBody(), notes: ["Risk: high vì publish công khai lên mạng xã hội.", "AccountIds phải là GUID thật từ GET /api/admin/social/accounts; không bịia.", "PublishNow=true để đăng ngay; ScheduledFor để lên lịch.", "Content lịch sự, đúng giọng thương hiệu; không spam.", "MediaUrls phải là URL thật (từ POST /api/admin/social/media/upload).", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Put("/api/admin/social/posts/{postId}", "Cập nhật bài social (content/lịch/account).", "SocialPostDto", SocialUpdatePostBody(), path: [StringId("postId", "Social Post ID")], notes: ["Risk: medium.", "Chỉ cập nhật được trước khi publish; sau khi publish chỉ sửa content.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/admin/social/posts/{postId}/unpublish", "Hủy xuất bản bài social trên một nền tảng.", "SocialPostActionResultDto", SocialUnpublishBody(), path: [StringId("postId", "Social Post ID")], notes: ["Risk: high vì gỡ bài public.", "Cần Platform thật (facebook/instagram/tiktok).", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Delete("/api/admin/social/posts/{postId}", "Xóa bài social.", "SocialPostActionResultDto", path: [StringId("postId", "Social Post ID")], notes: ["Risk: high.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Get("/api/admin/social/comments", "List bài social có bình luận cần chăm sóc (đa nền tảng).", "SocialCommentedPostListDto", query: [Param("platform", "string", false, "facebook (mặc định)"), Param("accountId", "string", false, "ID tài khoản Zernio"), Param("profileId", "string", false, "Zernio profile ID"), Param("cursor", "string", false, "Cursor trang kế"), Param("limit", "int", false, "Số bản ghi, mặc định 25")]),
    Get("/api/admin/social/comments/{postId}", "List bình luận trên một bài social.", "SocialCommentListDto", path: [StringId("postId", "Social Post ID")], query: [Param("accountId", "string", true, "ID tài khoản Zernio (bắt buộc)"), Param("cursor", "string", false, "Cursor"), Param("limit", "int", false, "Số bản ghi, mặc định 50")]),
    Post("/api/admin/social/comments/{postId}", "Trả lời bình luận social (đa nền tảng).", "SocialActionResultDto", SocialCommentReplyBody(), path: [StringId("postId", "Social Post ID")], notes: ["Risk: medium vì phản hồi công khai đại diện thương hiệu.", "AccountId phải là ID thật từ GET /api/admin/social/accounts.", "Reply ngắn, ấm; cảm ơn khi tích cực, xin lỗi + hướng dẫn khi tiêu cực.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Patch("/api/admin/social/comments/{postId}/{commentId}/visibility", "Ẩn/hiện bình luận social.", "SocialActionResultDto", SocialToggleHiddenBody(), path: [StringId("postId", "Social Post ID"), StringId("commentId", "Social Comment ID")], query: [Param("accountId", "string", true, "ID tài khoản Zernio (bắt buộc)")], notes: ["Risk: medium.", "accountId truyền qua query, không qua body.", "Ưu tiên ẩn (IsHidden=true) thay vì xóa khi bình luận tiêu cực/spam.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Delete("/api/admin/social/comments/{postId}/{commentId}", "Xóa bình luận social.", "SocialActionResultDto", path: [StringId("postId", "Social Post ID"), StringId("commentId", "Social Comment ID")], query: [Param("accountId", "string", true, "ID tài khoản Zernio (bắt buộc)")], notes: ["Risk: high vì xóa public, không hoàn tác.", "accountId truyền qua query, không qua body.", "Chỉ khi spam/vi phạm nghiêm trọng; cân nhắc ẩn trước.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Get("/api/admin/social/conversations", "List hội thoại inbox social (đa nền tảng).", "SocialConversationListDto", query: [Param("platform", "string", false, "facebook (mặc định)"), Param("accountId", "string", false, "ID tài khoản Zernio"), Param("profileId", "string", false, "Zernio profile ID"), Param("cursor", "string", false, "Cursor"), Param("limit", "int", false, "Số bản ghi, mặc định 25")]),
    Get("/api/admin/social/conversations/{conversationId}/messages", "List tin nhắn trong hội thoại social.", "SocialMessageListDto", path: [StringId("conversationId", "Social Conversation ID")], query: [Param("accountId", "string", true, "ID tài khoản Zernio (bắt buộc)"), Param("cursor", "string", false, "Cursor"), Param("limit", "int", false, "Số bản ghi, mặc định 50")]),
    Post("/api/admin/social/conversations/{conversationId}/messages", "Gửi tin nhắn social.", "SocialActionResultDto", SocialMessageBody(), path: [StringId("conversationId", "Social Conversation ID")], notes: ["Risk: medium vì gửi tin nhắn ra ngoài.", "AccountId phải là ID thật; Message lịch sự, đúng giọng thương hiệu.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/admin/social/conversations/{conversationId}/read", "Đánh dấu hội thoại social đã đọc.", "SocialActionResultDto", SocialMarkReadBody(), path: [StringId("conversationId", "Social Conversation ID")], notes: ["Risk: low.", "AccountId phải là ID thật.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),
    Post("/api/admin/social/media/upload", "Upload media cho bài social (multipart); trả về PublicUrl để dùng trong CreateSocialPostRequest.MediaUrls.", "SocialMediaUploadDto", MultipartBody("file", "File ảnh/video social"), notes: ["Risk: low.", "Dùng Content-Type: multipart/form-data; gửi file trong field `file`.", "PublicUrl trả về dùng cho MediaUrls trong POST /api/admin/social/posts.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),

    // AI try-on feedback moderation
    Get("/api/admin/ai-tryon-feedback", "List feedback khách về AI try-on (rating/comment) để admin xử lý.", "Paginated AdminAiTryOnFeedbackDto[]", query: [Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang"), Param("status", "string", false, "Lọc theo trạng thái resolved"), Param("rating", "int", false, "Lọc theo số sao 1-5")]),
    Patch("/api/admin/ai-tryon-feedback/{id}/status", "Cập nhật trạng thái feedback AI try-on (resolved + ghi chú admin).", "AdminAiTryOnFeedbackDto", AiTryOnFeedbackStatusBody(), path: [Id("id", "ID feedback")], notes: ["Risk: low.", "IsResolved=true khi đã xử lý; AdminNote ghi hành động đã làm.", "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."]),

    // Hermes reports
    Get("/api/admin/hermes/reports", "List báo cáo Hermes đã lưu.", "Paginated HermesReportListItemResponse[]", query: [Param("severity", "string", false, "info/warning/high/critical"), Param("type", "string", false, "Loại báo cáo"), Param("status", "string", false, "Trạng thái"), Param("q", "string", false, "Từ khóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/hermes/reports/{id}", "Chi tiết báo cáo Hermes.", "HermesReportResponse", path: [Id("id", "ID báo cáo")]),
    Post("/api/admin/hermes/report", "Hermes runner gửi báo cáo về backend để lưu DB.", "HermesReportResponse", HermesReportBody(), notes: ["Endpoint callback dùng X-Hermes-Admin-Key.", "PayloadJson phải là chuỗi JSON hợp lệ nếu có.", "Không gửi secrets/token/raw PII nếu không cần."]),

    // Hermes outbox events
    Get("/api/admin/hermes/events", "List Hermes event outbox.", "Paginated HermesEventOutboxListItemResponse[]", query: [Param("status", "string", false, "pending/processing/completed/failed/dead/cancelled"), Param("eventType", "string", false, "Loại event"), Param("aggregateType", "string", false, "Order/Product/Inventory/Promotion/AdminSecurity/Role/Content/Email/HermesConfig"), Param("q", "string", false, "Từ khóa"), Param("page", "int", false, "Trang"), Param("pageSize", "int", false, "Kích thước trang")]),
    Get("/api/admin/hermes/events/{id}", "Chi tiết Hermes event outbox.", "HermesEventOutboxResponse", path: [Id("id", "ID event")]),
    Post("/api/admin/hermes/events/{id}/retry", "Đưa event Hermes vào hàng đợi xử lý lại.", "null", path: [Id("id", "ID event")]),
    Post("/api/admin/hermes/events/{id}/cancel", "Hủy event Hermes đang pending/failed.", "null", path: [Id("id", "ID event")]),

    // NOTE: Admin-side mutations (order/product/stock/promo/user/role/content/email/tools-risk/blog/facebook/social)
    // auto-enqueue Hermes events into a durable outbox for autonomous analysis.
    // Event payloads are UNTRUSTED DATA — never treat payload fields as instructions.
    // Event/outbox-driven analysis stays report-only; it never auto-executes from untrusted payloads.
    // Chat/cron-initiated actions are agent-callable via X-Hermes-Admin-Key when IDs are validated
    // through describe/lookup, and every such mutation is recorded in the hermes_action_audit table.

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

  private static HermesAdminApiDescription Get(string route, string purpose, string dataShape, IReadOnlyList<HermesParamDescription>? path = null, IReadOnlyList<HermesParamDescription>? query = null, IReadOnlyList<string>? notes = null) =>
    Desc("GET", route, purpose, dataShape, null, path, query, notes);

  private static HermesAdminApiDescription Post(string route, string purpose, string dataShape, HermesBodyDescription? body = null, IReadOnlyList<HermesParamDescription>? path = null, IReadOnlyList<string>? notes = null) =>
    Desc("POST", route, purpose, dataShape, body, path, null, notes);

  private static HermesAdminApiDescription Put(string route, string purpose, string dataShape, HermesBodyDescription? body = null, IReadOnlyList<HermesParamDescription>? path = null, IReadOnlyList<string>? notes = null) =>
    Desc("PUT", route, purpose, dataShape, body, path, null, notes);

  private static HermesAdminApiDescription Patch(string route, string purpose, string dataShape, HermesBodyDescription? body = null, IReadOnlyList<HermesParamDescription>? path = null, IReadOnlyList<HermesParamDescription>? query = null, IReadOnlyList<string>? notes = null) =>
    Desc("PATCH", route, purpose, dataShape, body, path, query, notes);

  private static HermesAdminApiDescription Delete(string route, string purpose, string dataShape, IReadOnlyList<HermesParamDescription>? path = null, IReadOnlyList<HermesParamDescription>? query = null, IReadOnlyList<string>? notes = null) =>
    Desc("DELETE", route, purpose, dataShape, null, path, query, notes);

  private static HermesAdminApiDescription Desc(string method, string route, string purpose, string dataShape, HermesBodyDescription? body, IReadOnlyList<HermesParamDescription>? path, IReadOnlyList<HermesParamDescription>? query, IReadOnlyList<string>? notes = null) =>
    new(method, route, purpose, path ?? [], query ?? [], body, Envelope with { DataShape = dataShape }, notes ?? DefaultNotes);

  private static HermesParamDescription Id(string name, string description) => Param(name, "guid", true, description);

  // Non-GUID path param (Facebook Page IDs, post IDs, conversation IDs are
  // numeric strings, not GUIDs). Using the wrong type misleads the agent.
  private static HermesParamDescription StringId(string name, string description) => Param(name, "string", true, description);

  private static HermesParamDescription Param(string name, string type, bool required, string description) => new(name, type, required, description);

  private static KeyValuePair<string, HermesFieldDescription> Field(string name, string type, bool required, string description) =>
    new(name, new HermesFieldDescription(type, required, description));

  private static HermesBodyDescription Body(IEnumerable<KeyValuePair<string, HermesFieldDescription>> fields, object? example) =>
    new("application/json", true, fields.ToDictionary(), example);

  private static HermesBodyDescription MultipartBody(string fieldName, string description) =>
    new("multipart/form-data", true, new Dictionary<string, HermesFieldDescription> { [fieldName] = new("file", true, description) }, null);

  private static HermesBodyDescription SingleEmailJobBody() =>
    Body([
      Field("toEmail", "string", false, "Email khách hàng; nếu gửi phải khớp customerId/orderId"),
      Field("customerId", "guid", false, "ID khách hàng nguồn; bắt buộc nếu không có orderId"),
      Field("orderId", "guid", false, "ID đơn hàng nguồn; bắt buộc nếu không có customerId"),
      Field("templateKey", "string", true, "Dùng hermes.single_email nếu không có template riêng"),
      Field("subject", "string", true, "Tiêu đề email"),
      Field("preheader", "string", false, "Preview text"),
      Field("intro", "string", false, "Lời mở đầu"),
      Field("body", "string", false, "Nội dung text, tối đa 4000 ký tự"),
      Field("ctaLabel", "string", false, "Nhãn CTA"),
      Field("ctaUrl", "string", false, "URL CTA"),
      Field("purpose", "string", true, "transactional/survey/thank_you"),
      Field("scheduledAt", "datetime", false, "UTC ISO time; bỏ trống để gửi ngay, +14 ngày cho survey"),
      Field("idempotencyKey", "string", true, "Khóa ổn định, ví dụ hermes:thank-you:{orderId}")
    ], new { toEmail = "khach@example.com", customerId = "00000000-0000-0000-0000-000000000000", orderId = (string?)null, templateKey = "hermes.single_email", subject = "Cảm ơn chị đã tin yêu Áo Dài Nhã Uyên", preheader = "Nhã Uyên rất trân trọng trải nghiệm của chị", intro = "Chào chị,", body = "Cảm ơn chị đã lựa chọn Áo Dài Nhã Uyên. Nhã Uyên hy vọng sản phẩm mang lại trải nghiệm thật đẹp và thoải mái cho chị.", ctaLabel = "Chia sẻ cảm nhận", ctaUrl = "https://aodainhauyen.io.vn", purpose = "thank_you", scheduledAt = (string?)null, idempotencyKey = "hermes:thank-you:00000000000000000000000000000000" });

  private static HermesBodyDescription ReviewReplyBody() =>
    Body([
      Field("productId", "guid", true, "ID sản phẩm chứa review/comment gốc"),
      Field("content", "string", true, "Nội dung phản hồi công khai, tiếng Việt, lịch sự/ấm/chuyên nghiệp")
    ], new { productId = "00000000-0000-0000-0000-000000000000", content = "Cảm ơn chị đã chia sẻ trải nghiệm với Áo Dài Nhã Uyên. Nhã Uyên rất vui khi sản phẩm hợp ý chị ạ!" });

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

  private static HermesBodyDescription BlogSeoBody() =>
    Body([
      Field("metaTitle", "string", false, "Meta title SEO, nên <= 60 ký tự"),
      Field("metaDescription", "string", false, "Meta description SEO, nên khoảng 150-160 ký tự"),
      Field("canonicalUrl", "string", false, "Canonical URL tuyệt đối thuộc aodainhauyen.io.vn"),
      Field("reviewedBy", "string", false, "Tên người/role reviewer E-E-A-T"),
      Field("informationGain", "string", false, "Điểm độc đáo/thông tin mới của bài viết"),
      Field("tags", "string[]", false, "Keyword/topic cluster liên quan")
    ], new { metaTitle = "Áo dài Việt Nam - Biểu tượng văn hóa", metaDescription = "Khám phá ý nghĩa áo dài Việt Nam, cách mặc và giá trị văn hóa dành cho du khách quốc tế.", canonicalUrl = "https://aodainhauyen.io.vn/blog/ao-dai-viet-nam", reviewedBy = "Biên tập Áo Dài Nhã Uyên", informationGain = "Góc nhìn từ nghệ nhân và ngữ cảnh văn hóa Việt.", tags = new[] { "áo dài", "văn hóa Việt", "áo dài du khách" } });

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

  private static readonly IReadOnlyList<string> NonExecutableEmailTemplateNotes =
  [
    "Endpoint trả HTTP 405 (templates_code_managed).",
    "Mẫu email được React Email quản lý (code), không thể tạo/sửa/xóa qua API.",
    "Chỉ dùng GET /api/admin/email-templates để đọc. Không propose action cho POST/PUT/DELETE/restore — actions phải là [].",
    "Nếu cần mẫu mới, báo admin thêm qua code repository."
  ];

  // ---- Blog bodies ----
  private static HermesBodyDescription BlogCreateBody() =>
    Body([
      Field("title", "string", true, "Tiêu đề bài blog (max 500)"),
      Field("slug", "string", false, "Slug URL; nếu bỏ trống backend tự sinh từ title"),
      Field("excerpt", "string", true, "Tóm tắt ngắn hiển thị ở list/preview"),
      Field("featuredImage", "string", false, "URL ảnh封面; phải là URL từ POST /api/v1/admin/blog/upload"),
      Field("featuredImageWidth", "int", false, "Chiều rộng ảnhFeaturedImage"),
      Field("featuredImageHeight", "int", false, "Chiều cao ảnhFeaturedImage"),
      Field("template", "string", false, "Template layout (StandardArticle, ...)"),
      Field("content", "object", true, "JSON content blocks (kiểu BlogBlock), không phải text thuần — ví dụ [{\"type\":\"paragraph\",\"text\":\"...\"}]"),
      Field("tags", "string[]", false, "Keyword/topic cluster"),
      Field("blogCategoryId", "guid", false, "ID danh mục blog thật từ GET /api/v1/admin/blog/categories"),
      Field("authorId", "guid", false, "ID tác giả (user) nếu có"),
      Field("authorNameOverride", "string", false, "Tên hiển thị tác giả (ghi đè)"),
      Field("authorBio", "string", false, "Tiểu sử tác giả"),
      Field("reviewedBy", "string", false, "Người/role review E-E-A-T (cần khi published)"),
      Field("informationGain", "string", false, "Điểm độc đáo/thông tin mới (cần khi published)"),
      Field("status", "string", false, "draft (mặc định, an toàn) / published / archived"),
      Field("publishedAt", "datetime", false, "Thời gian publish (UTC ISO); bỏ trống để dùng now"),
      Field("metaTitle", "string", false, "Meta title SEO (<= 60 ký tự)"),
      Field("metaDescription", "string", false, "Meta description SEO (~150-160 ký tự)"),
      Field("canonicalUrl", "string", false, "Canonical URL tuyệt đối thuộc aodainhauyen.io.vn")
    ], new { title = "Áo dài cưới 2026 — Xu hướng và gợi ý từ Nhà Uyên", slug = "ao-dai-cuoi-2026", excerpt = "Tổng hợp xu hướng áo dài cưới 2026 và gợi ý chọn váy cho cô dâu Việt.", featuredImage = (string?)null, featuredImageWidth = (int?)null, featuredImageHeight = (int?)null, template = "StandardArticle", content = new[] { new { type = "paragraph", text = "Nội dung mở đầu..." } }, tags = new[] { "áo dài cưới", "2026", "cô dâu" }, blogCategoryId = "00000000-0000-0000-0000-000000000000", authorId = (Guid?)null, authorNameOverride = "Biên tập Áo Dài Nhã Uyên", authorBio = "Đội nội dung Áo Dài Nhã Uyên", reviewedBy = "Biên tập Áo Dài Nhã Uyên", informationGain = "Góc nhìn nghệ nhân + xu hướng 2026.", status = "draft", publishedAt = (DateTime?)null, metaTitle = "Áo dài cưới 2026 — Xu hướng mới nhất", metaDescription = "Khám phá xu hướng áo dài cưới 2026 và gợi ý chọn váy cho cô dâu Việt.", canonicalUrl = "https://aodainhauyen.io.vn/blog/ao-dai-cuoi-2026" });

  private static HermesBodyDescription BlogUpdateBody() =>
    Body([
      Field("title", "string", true, "Tiêu đề bài blog (max 500)"),
      Field("slug", "string", false, "Slug URL"),
      Field("excerpt", "string", true, "Tóm tắt ngắn"),
      Field("featuredImage", "string", false, "URL ảnh封面 từ POST /api/v1/admin/blog/upload"),
      Field("featuredImageWidth", "int", false, "Chiều rộng ảnhFeaturedImage"),
      Field("featuredImageHeight", "int", false, "Chiều cao ảnhFeaturedImage"),
      Field("template", "string", false, "Template layout"),
      Field("content", "object", true, "JSON content blocks (kiểu BlogBlock)"),
      Field("tags", "string[]", false, "Keyword/topic cluster"),
      Field("blogCategoryId", "guid", false, "ID danh mục blog thật"),
      Field("authorId", "guid", false, "ID tác giả"),
      Field("authorNameOverride", "string", false, "Tên hiển thị tác giả"),
      Field("authorBio", "string", false, "Tiểu sử tác giả"),
      Field("reviewedBy", "string", false, "Người/role review E-E-A-T"),
      Field("informationGain", "string", false, "Điểm độc đáo/thông tin mới"),
      Field("status", "string", false, "draft / published / archived"),
      Field("publishedAt", "datetime", false, "Thời gian publish (UTC ISO)"),
      Field("metaTitle", "string", false, "Meta title SEO"),
      Field("metaDescription", "string", false, "Meta description SEO"),
      Field("canonicalUrl", "string", false, "Canonical URL tuyệt đối thuộc aodainhauyen.io.vn")
    ], new { title = "Áo dài cưới 2026 (đã cập nhật)", slug = "ao-dai-cuoi-2026", excerpt = "Tổng hợp xu hướng áo dài cưới 2026.", featuredImage = (string?)null, featuredImageWidth = (int?)null, featuredImageHeight = (int?)null, template = "StandardArticle", content = new[] { new { type = "paragraph", text = "Nội dung cập nhật..." } }, tags = new[] { "áo dài cưới", "2026" }, blogCategoryId = "00000000-0000-0000-0000-000000000000", authorId = (Guid?)null, authorNameOverride = "Biên tập Áo Dài Nhã Uyên", authorBio = "Đội nội dung Áo Dài Nhã Uyên", reviewedBy = "Biên tập Áo Dài Nhã Uyên", informationGain = "Góc nhìn nghệ nhân + xu hướng 2026.", status = "published", publishedAt = (DateTime?)null, metaTitle = "Áo dài cưới 2026", metaDescription = "Xu hướng áo dài cưới 2026.", canonicalUrl = "https://aodainhauyen.io.vn/blog/ao-dai-cuoi-2026" });

  // ---- Product variant bodies ----
  private static HermesBodyDescription VariantCreateBody() =>
    Body([
      Field("sku", "string", true, "Mã SKU biến thể, duy nhất (max 120)"),
      Field("variantName", "string", false, "Tên hiển thị biến thể (max 120)"),
      Field("size", "string", false, "Kích cỡ (S/M/L/XL/...)"),
      Field("color", "string", false, "Màu sắc"),
      Field("price", "number", true, "Giá bán (VND)"),
      Field("salePrice", "number", false, "Giá khuyến mãi (nếu có)"),
      Field("stockQty", "int", true, "Số lượng tồn kho"),
      Field("isDefault", "bool", false, "Có phải biến thể mặc định không"),
      Field("status", "string", true, "active/inactive/draft")
    ], new { sku = "AD-001-RED-M", variantName = "Áo dài đỏ size M", size = "M", color = "Đỏ", price = 1500000, salePrice = (decimal?)null, stockQty = 10, isDefault = false, status = "active" });

  private static HermesBodyDescription VariantUpdateBody() =>
    Body([
      Field("sku", "string", true, "Mã SKU biến thể (max 120)"),
      Field("variantName", "string", false, "Tên hiển thị biến thể"),
      Field("size", "string", false, "Kích cỡ"),
      Field("color", "string", false, "Màu sắc"),
      Field("price", "number", true, "Giá bán (VND)"),
      Field("salePrice", "number", false, "Giá khuyến mãi"),
      Field("stockQty", "int", true, "Số lượng tồn kho"),
      Field("isDefault", "bool", false, "Biến thể mặc định"),
      Field("status", "string", true, "active/inactive/draft")
    ], new { sku = "AD-001-RED-M", variantName = "Áo dài đỏ size M", size = "M", color = "Đỏ", price = 1500000, salePrice = (decimal?)null, stockQty = 8, isDefault = false, status = "active" });

  // ---- Marketing campaign body ----
  private static HermesBodyDescription CampaignSendBody() =>
    Body([
      Field("recipientMode", "string", true, "subscribers (cần SubscriberIds) hoặc manual (cần ManualEmails opt-in)"),
      Field("subscriberIds", "guid[]", false, "Danh sách ID subscriber thật (bắt buộc khi recipientMode=subscribers)"),
      Field("manualEmails", "string[]", false, "Danh sách email manual opt-in (bắt buộc khi recipientMode=manual)"),
      Field("templateKey", "string", true, "Key của email template thật đang active"),
      Field("subject", "string", true, "Tiêu đề email"),
      Field("preheader", "string", false, "Preview text"),
      Field("intro", "string", false, "Lời mở đầu"),
      Field("bodyHtml", "string", false, "Nội dung HTML email"),
      Field("ctaLabel", "string", false, "Nhãn CTA"),
      Field("ctaUrl", "string", false, "URL CTA"),
      Field("attachments", "object[]", false, "Đính kèm: { type, id?, title, url?, description?, code? }"),
      Field("scheduledAt", "datetime", false, "UTC ISO; bỏ trống để gửi ngay")
    ], new { recipientMode = "subscribers", subscriberIds = new[] { "00000000-0000-0000-0000-000000000000" }, manualEmails = (string[]?)null, templateKey = "weekly_promo", subject = "Ưu đãi tuần này từ Áo Dài Nhã Uyên", preheader = "Giảm 15% bộ sưu tập áo dài cưới", intro = "Chào chị,", bodyHtml = "<p>Tuần này Nhà Uyên có ưu đãi...</p>", ctaLabel = "Mua ngay", ctaUrl = "https://aodainhauyen.io.vn", attachments = (object[]?)null, scheduledAt = (DateTime?)null });

  // ---- Facebook bodies ----
  private static HermesBodyDescription FacebookConnectBody() =>
    Body([
      Field("pageId", "string", true, "Facebook Page ID (chuỗi số thật)"),
      Field("pageAccessToken", "string", true, "SECRET — Facebook Page Access Token. Gửi đúng một lần qua X-Hermes-Admin-Key; KHÔNG bao giờ in giá trị trong response/report/log/chat."),
      Field("pageName", "string", false, "Tên page hiển thị")
    ], new { pageId = "1234567890", pageAccessToken = "<SENT_ONCE_NEVER_LOGGED>", pageName = "Áo Dài Nhã Uyên" });

  private static HermesBodyDescription FacebookOAuthPagesBody() =>
    Body([
      Field("code", "string", true, "Facebook OAuth code từ bước OAuth (setup 2-bước)"),
      Field("redirectUri", "string", true, "URL callback khớp với OAuth URL ban đầu")
    ], new { code = "<FROM_OAUTH_FLOW>", redirectUri = "https://aodainhauyen.io.vn/admin/facebook/callback" });

  private static HermesBodyDescription FacebookOAuthConnectBody() =>
    Body([
      Field("pageId", "string", true, "Facebook Page ID thật từ bước OAuth"),
      Field("connectToken", "string", true, "Connect token từ response của /oauth/pages (setup 2-bước)")
    ], new { pageId = "1234567890", connectToken = "<FROM_OAUTH_PAGES_STEP>" });

  private static HermesBodyDescription FacebookPostBody() =>
    Body([
      Field("message", "string", true, "Nội dung bài đăng text"),
      Field("link", "string", false, "URL link đính kèm"),
      Field("scheduledPublishTime", "datetime", false, "UTC ISO; bỏ trống để đăng ngay"),
      Field("published", "bool", false, "true (mặc định) đăng ngay; false để lên lịch")
    ], new { message = "Bộ sưu tập áo dài cưới 2026 đã ra mắt 💝", link = "https://aodainhauyen.io.vn", scheduledPublishTime = (DateTimeOffset?)null, published = true });

  private static HermesBodyDescription FacebookUpdatePostBody() =>
    Body([Field("message", "string", true, "Nội dung text mới (chỉ cập nhật text)")], new { message = "Bộ sưu tập áo dài cưới 2026 — cập nhật" });

  private static HermesBodyDescription FacebookCommentBody() =>
    Body([Field("message", "string", true, "Nội dung bình luận/reply (lịch sự, đúng giọng thương hiệu)")], new { message = "Cảm ơn chị đã quan tâm ạ 💝" });

  private static HermesBodyDescription FacebookCommentReplyBody() =>
    Body([
      Field("message", "string", true, "Nội dung reply lịch sự, đúng giọng thương hiệu"),
      Field("postId", "string", true, "Facebook Post ID chứa bình luận; bắt buộc để Zernio/Facebook reply đúng thread")
    ], new { message = "Cảm ơn chị đã quan tâm ạ 💝", postId = "123456789_10111213" });

  private static HermesBodyDescription FacebookToggleHiddenBody() =>
    Body([Field("isHidden", "bool", true, "true để ẩn, false để hiện")], new { isHidden = true });

  private static HermesBodyDescription FacebookMessageBody() =>
    Body([
      Field("text", "string", false, "Nội dung tin nhắn"),
      Field("attachmentUrl", "string", false, "URL attachment nếu có"),
      Field("attachmentType", "string", false, "Loại attachment (image/video/file)")
    ], new { text = "Chào chị, Nhà Uyên có thể hỗ trợ gì cho chị ạ?", attachmentUrl = (string?)null, attachmentType = (string?)null });

  // Facebook photo/video dùng multipart form-data, không phải JSON body.
  // Fields bổ sung (caption/description, scheduledPublishTime, published) gửi
  // cùng file qua form-data; schema không có body JSON.
  private static readonly IReadOnlyList<string> FacebookPhotoNotes =
  [
    "Risk: high vì publish ảnh công khai lên page Facebook.",
    "Dùng Content-Type: multipart/form-data; gửi file trong field form-data tên `file` (jpg/png/gif/webp, tối đa 10MB).",
    "Fields form-data bổ sung: caption (string), scheduledPublishTime (datetime ISO, optional), published (bool, mặc định true).",
    "Ảnh phải chất lượng tốt, đúng thương hiệu; không đăng ảnh khách không xin phép.",
    "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."
  ];

  private static readonly IReadOnlyList<string> FacebookVideoNotes =
  [
    "Risk: high vì publish video công khai và tốn quota upload.",
    "Dùng Content-Type: multipart/form-data; gửi file trong field form-data tên `file` (mp4/mov/webm, tối đa 200MB).",
    "Fields form-data bổ sung: description (string), scheduledPublishTime (datetime ISO, optional), published (bool, mặc định true).",
    "Gọi lại cùng URL không có X-Hermes-Describe để thực thi."
  ];

  // ---- Social bodies ----
  private static HermesBodyDescription SocialSelectPageBody() =>
    Body([
      Field("profileId", "string", true, "Zernio profile ID thật"),
      Field("pageId", "string", true, "Facebook Page ID thật"),
      Field("tempToken", "string", true, "Temp token từ bước connect-url (setup 2-bước)"),
      Field("userProfile", "object", true, "{ id, name, profilePicture } từ bước connect"),
      Field("redirectUrl", "string", true, "URL callback khớp connect-url")
    ], new { profileId = "zernio-profile-1", pageId = "1234567890", tempToken = "<FROM_CONNECT_STEP>", userProfile = new { id = "zernio-user-1", name = "Admin Áo Dài Nhã Uyên", profilePicture = "https://..." }, redirectUrl = "https://aodainhauyen.io.vn/admin/social/callback" });
  private static HermesBodyDescription SocialPostBody() =>
    Body([
      Field("content", "string", true, "Nội dung bài đăng"),
      Field("accountIds", "guid[]", true, "Danh sách ID tài khoản social thật từ GET /api/admin/social/accounts"),
      Field("publishNow", "bool", false, "true để đăng ngay; false cần ScheduledFor"),
      Field("scheduledFor", "datetime", false, "UTC ISO; bắt buộc khi publishNow=false"),
      Field("mediaUrls", "string[]", false, "URL media thật từ POST /api/admin/social/media/upload")
    ], new { content = "Áo dài cưới 2026 — Sự kết hợp giữa truyền thống và hiện đại 💝", accountIds = new[] { "00000000-0000-0000-0000-000000000000" }, publishNow = false, scheduledFor = DateTimeOffset.UtcNow.AddHours(2), mediaUrls = (string[]?)null });

  private static HermesBodyDescription SocialUpdatePostBody() =>
    Body([
      Field("content", "string", false, "Nội dung mới"),
      Field("publishNow", "bool", false, "Đăng ngay"),
      Field("scheduledFor", "datetime", false, "UTC ISO lên lịch"),
      Field("accountIds", "guid[]", false, "Danh sách tài khoản mới"),
      Field("mediaUrls", "string[]", false, "URL media mới")
    ], new { content = "Nội dung cập nhật", publishNow = false, scheduledFor = (DateTimeOffset?)null, accountIds = (Guid[]?)null, mediaUrls = (string[]?)null });

  private static HermesBodyDescription SocialUnpublishBody() =>
    Body([Field("platform", "string", true, "facebook/instagram/tiktok — nền tảng cần gỡ")], new { platform = "facebook" });

  private static HermesBodyDescription SocialCommentReplyBody() =>
    Body([
      Field("accountId", "string", true, "ID tài khoản social thật (string, từ GET /api/admin/social/accounts)"),
      Field("message", "string", true, "Nội dung reply lịch sự, đúng giọng thương hiệu"),
      Field("commentId", "string", false, "ID bình luận cha nếu là reply (bỏ trống cho comment mới)")
    ], new { accountId = "00000000", message = "Cảm ơn chị đã chia sẻ ạ 💝", commentId = (string?)null });

  private static HermesBodyDescription SocialToggleHiddenBody() =>
    Body([Field("isHidden", "bool", true, "true để ẩn, false để hiện")], new { isHidden = true });

  private static HermesBodyDescription SocialMessageBody() =>
    Body([
      Field("accountId", "string", true, "ID tài khoản social thật"),
      Field("message", "string", false, "Nội dung tin nhắn"),
      Field("attachmentUrl", "string", false, "URL attachment"),
      Field("attachmentType", "string", false, "Loại attachment (image/video/file)")
    ], new { accountId = "00000000", message = "Chào chị, Nhà Uyên có thể hỗ trợ gì ạ?", attachmentUrl = (string?)null, attachmentType = (string?)null });

  private static HermesBodyDescription SocialMarkReadBody() =>
    Body([Field("accountId", "string", true, "ID tài khoản social thật")], new { accountId = "00000000" });

  // ---- AI try-on feedback body ----
  private static HermesBodyDescription AiTryOnFeedbackStatusBody() =>
    Body([
      Field("isResolved", "bool", true, "true khi đã xử lý feedback"),
      Field("adminNote", "string", false, "Ghi chú hành động đã làm (vd: 'đã liên hệ khách', 'đã cải thiện prompt')")
    ], new { isResolved = true, adminNote = "Đã ghi nhận và cải thiện chất lượng render áo dài." });
}
