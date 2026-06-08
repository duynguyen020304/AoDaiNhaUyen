using System.Runtime.CompilerServices;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminAgentService : IAdminAgentService
{
  private readonly IAdminLlmProvider _llm;
  private readonly ISafetyGate _safety;
  private readonly IAdminProductService _products;
  private readonly IAdminCategoryService _categories;
  private readonly IAdminUserService _users;
  private readonly IAdminRoleService _roles;
  private readonly IAdminDashboardService _dashboard;
  private readonly IAdminOrderService _orders;
  private readonly IAdminInventoryService _inventory;
  private readonly IAdminReviewService _reviews;
  private readonly IAdminPromoService _promos;
  private readonly IAutoModeStore _autoMode;
  private readonly ILogger<AdminAgentService> _logger;

  private readonly IPendingActionStore _pendingStore;
  private readonly IConversationStore _conversationStore;

  public AdminAgentService(
    IAdminLlmProvider llm,
    ISafetyGate safety,
    IAdminProductService products,
    IAdminCategoryService categories,
    IAdminUserService users,
    IAdminRoleService roles,
    IAdminDashboardService dashboard,
    IAdminOrderService orders,
    IAdminInventoryService inventory,
    IAdminReviewService reviews,
    IAdminPromoService promos,
    IAutoModeStore autoMode,
    ILogger<AdminAgentService> logger,
    IPendingActionStore pendingStore,
    IConversationStore conversationStore)
  {
    _llm = llm;
    _safety = safety;
    _products = products;
    _categories = categories;
    _users = users;
    _roles = roles;
    _dashboard = dashboard;
    _orders = orders;
    _inventory = inventory;
    _reviews = reviews;
    _promos = promos;
    _autoMode = autoMode;
    _logger = logger;
    _pendingStore = pendingStore;
    _conversationStore = conversationStore;
  }

  private static readonly IReadOnlyList<ToolDefinition> Tools =
  [
    // Dashboard
    T("get_dashboard_summary", "Lấy tổng quan dashboard (tổng doanh thu, đơn hàng, người dùng, sản phẩm).",
      P(("period", O("string", "Khoảng thời gian: today, week, month. Mặc định: week")))),

    T("get_revenue", "Lấy dữ liệu doanh thu theo khoảng thời gian.",
      P(("period", O("integer", "Số ngày: 7, 30, hoặc 90. Mặc định: 7")))),

    T("get_orders_by_status", "Lấy phân phối đơn hàng theo trạng thái.", P()),

    T("get_recent_orders", "Lấy danh sách đơn hàng gần đây.",
      P(("limit", O("integer", "Số lượng đơn hàng. Mặc định: 10")))),

    T("get_top_products", "Lấy top sản phẩm bán chạy.",
      P(("limit", O("integer", "Số lượng sản phẩm. Mặc định: 5")))),

    // Products
    T("list_products", "Liệt kê danh sách sản phẩm với phân trang.",
      P(
        ("page", O("integer", "Trang hiện tại, mặc định 1")),
        ("pageSize", O("integer", "Số sản phẩm mỗi trang, mặc định 20")),
        ("search", O("string", "Từ khóa tìm kiếm (tùy chọn)")),
        ("status", O("string", "Lọc theo trạng thái (tùy chọn)")))),

    T("get_product", "Lấy chi tiết một sản phẩm.",
      P(("id", O("string", "ID của sản phẩm (GUID)")))),

    T("create_product", "Tạo sản phẩm mới (bản nháp).",
      P(
        ("name", O("string", "Tên sản phẩm")),
        ("description", O("string", "Mô tả sản phẩm (tùy chọn)")),
        ("categoryId", O("string", "ID danh mục (GUID) (tùy chọn)")),
        ("productType", O("string", "Loại: ao_dai hoặc phu_kien. Mặc định: ao_dai")))),

    T("update_product", "Cập nhật sản phẩm hiện có.",
      P(
        ("id", O("string", "ID sản phẩm (GUID)")),
        ("name", O("string", "Tên mới (tùy chọn)")),
        ("description", O("string", "Mô tả mới (tùy chọn)")))),

    T("delete_product", "Xóa mềm một sản phẩm.",
      P(("id", O("string", "ID sản phẩm (GUID)")))),

    T("toggle_product_status", "Bật/tắt trạng thái sản phẩm (active/inactive).",
      P(
        ("id", O("string", "ID sản phẩm (GUID)")),
        ("status", O("string", "Trạng thái mới: active hoặc inactive")))),

    // Categories
    T("list_categories", "Liệt kê tất cả danh mục.", P()),

    T("create_category", "Tạo danh mục mới.",
      P(
        ("name", O("string", "Tên danh mục")),
        ("description", O("string", "Mô tả (tùy chọn)")))),

    T("update_category", "Cập nhật danh mục.",
      P(
        ("id", O("string", "ID danh mục (GUID)")),
        ("name", O("string", "Tên mới (tùy chọn)")),
        ("description", O("string", "Mô tả mới (tùy chọn)")))),

    T("delete_category", "Xóa mềm một danh mục.",
      P(("id", O("string", "ID danh mục (GUID)")))),

    // Users
    T("list_users", "Liệt kê danh sách người dùng với phân trang.",
      P(
        ("page", O("integer", "Trang hiện tại, mặc định 1")),
        ("pageSize", O("integer", "Số người dùng mỗi trang, mặc định 20")),
        ("search", O("string", "Từ khóa tìm kiếm (tùy chọn)")))),

    T("get_user", "Lấy chi tiết một người dùng.",
      P(("id", O("string", "ID người dùng (GUID)")))),

    T("update_user_status", "Bật/tắt trạng thái người dùng.",
      P(
        ("id", O("string", "ID người dùng (GUID)")),
        ("status", O("string", "Trạng thái mới: active hoặc inactive")))),

    T("update_user_role", "Thay đổi vai trò người dùng (admin hoặc customer).",
      P(
        ("id", O("string", "ID người dùng (GUID)")),
        ("role", O("string", "Vai trò mới: admin hoặc customer")))),

    // Orders
    T("list_orders", "Liệt kê đơn hàng theo trạng thái.",
      P(
        ("status", O("string", "Lọc theo trạng thái: pending, confirmed, processing, shipping, completed, cancelled. Mặc định: tất cả")),
        ("limit", O("integer", "Số lượng đơn hàng. Mặc định: 10")))),

    T("get_order", "Xem chi tiết một đơn hàng.",
      P(("orderId", O("string", "ID đơn hàng (GUID)")))),

    T("confirm_order", "Xác nhận đơn hàng (pending → confirmed).",
      P(("orderId", O("string", "ID đơn hàng (GUID)")))),

    T("start_processing_order", "Bắt đầu xử lý đơn hàng (confirmed → processing).",
      P(("orderId", O("string", "ID đơn hàng (GUID)")))),

    T("ship_order", "Tạo shipment và chuyển đơn sang trạng thái shipping.",
      P(
        ("orderId", O("string", "ID đơn hàng (GUID)")),
        ("carrier", O("string", "Tên đơn vị vận chuyển (tùy chọn)")),
        ("trackingNumber", O("string", "Mã vận đơn (tùy chọn)")))),

    T("cancel_order", "Hủy đơn hàng và hoàn stock.",
      P(("orderId", O("string", "ID đơn hàng (GUID)")))),

    // Inventory & Store Health
    T("get_inventory_summary", "Kiểm tra tồn kho tổng quan và cảnh báo sản phẩm sắp hết.",
      P(("threshold", O("integer", "Ngưỡng tồn kho thấp. Mặc định: 10")))),

    T("get_store_health_score", "Điểm sức khỏe cửa hàng (0-100) dựa trên nhiều chỉ số.",
      P()),

    // Reviews & Comments
    T("list_recent_reviews", "Xem đánh giá gần đây từ khách hàng.",
      P(("limit", O("integer", "Số lượng đánh giá. Mặc định: 10")))),

    T("list_recent_comments", "Xem bình luận gần đây từ khách hàng.",
      P(("limit", O("integer", "Số lượng bình luận. Mặc định: 10")))),

    T("reply_to_review", "Phản hồi đánh giá (review) của khách hàng. Dùng cho review có rating.",
      P(
        ("commentId", O("string", "ID đánh giá gốc (GUID)")),
        ("productId", O("string", "ID sản phẩm (GUID)")),
        ("content", O("string", "Nội dung phản hồi")))),

    T("reply_to_comment", "Phản hồi một bình luận/đánh giá của khách hàng.",
      P(
        ("commentId", O("string", "ID bình luận gốc (GUID)")),
        ("productId", O("string", "ID sản phẩm (GUID)")),
        ("content", O("string", "Nội dung phản hồi")))),

    // Promotions
    T("create_purchase_note", "Tạo ghi chú nhập hàng (draft) cho sản phẩm.",
      P(
        ("productName", O("string", "Tên sản phẩm cần nhập")),
        ("quantity", O("integer", "Số lượng cần nhập")),
        ("note", O("string", "Ghi chú thêm (tùy chọn)")))),

    T("generate_daily_report", "Tạo báo cáo doanh thu và đơn hàng hôm nay.", P()),

    T("list_promo_codes", "Liệt kê tất cả mã khuyến mãi.", P()),

    // Autonomy Mode
    T("toggle_autonomy", "Bật/tắt chế độ tự động cho AI. Khi bật, các hành động Medium risk được tự động thực hiện.",
      P(("enabled", O("boolean", "true để bật, false để tắt")))),

    T("get_autonomy_status", "Kiểm tra trạng thái chế độ tự động hiện tại.", P()),

    T("create_promo_code", "Tạo mã khuyến mãi mới.",
      P(
        ("code", O("string", "Mã giảm giá (viết hoa, không dấu)")),
        ("discountType", O("string", "Loại: percentage hoặc fixed")),
        ("discountValue", O("number", "Giá trị giảm (% hoặc VND)")),
        ("minOrderAmount", O("number", "Đơn hàng tối thiểu. Mặc định: 0")),
        ("maxUses", O("integer", "Số lần sử dụng tối đa. Mặc định: 0 (không giới hạn)")),
        ("endDate", O("string", "Ngày hết hạn (ISO). Mặc định: 30 ngày nữa")))),

    // Phase 3: Intelligence
    T("generate_product_description", "Tạo mô tả sản phẩm bằng AI (tiếng Việt). Dùng khi tạo hoặc cải thiện mô tả sản phẩm.",
      P(
        ("productId", O("string", "ID sản phẩm (GUID) — đọc dữ liệu hiện có để làm gốc")),
        ("focus", O("string", "Trọng tâm: chất liệu, kiểu dáng, dịp mặc, hoặc all. Mặc định: all")))),

    T("generate_weekly_report", "Tạo báo cáo tuần tổng hợp từ dữ liệu dashboard. Trả về bản tóm tắt dạng văn bản tiếng Việt.",
      P(("periodDays", O("integer", "Số ngày phân tích. Mặc định: 7")))),

    T("check_inventory_alerts", "Kiểm tra sản phẩm sắp hết hàng (tồn kho thấp).",
      P(("threshold", O("integer", "Ngưỡng tồn kho thấp. Mặc định: 10")))),
  ];

  public async IAsyncEnumerable<LlmChunk> StreamChatAsync(
    AdminAiChatRequest request,
    Guid adminUserId,
    [EnumeratorCancellation] CancellationToken ct)
  {
    var conversationId = request.ConversationId ?? Guid.NewGuid().ToString("N");
    var history = _conversationStore.GetOrAdd(conversationId, () => (new List<AdminLlmMessage>(), adminUserId)).History;

    // Tell frontend the conversation ID so it can continue after confirmations
    yield return new LlmChunk("conversation", conversationId);

    if (!string.IsNullOrWhiteSpace(request.Message))
      history.Add(new AdminLlmMessage(AdminLlmRole.User, request.Message));

    var maxIterations = 5;
    for (var iteration = 0; iteration < maxIterations; iteration++)
    {
      var hadToolCall = false;
      var assistantText = "";

      await foreach (var chunk in _llm.StreamChatAsync(history, Tools, ct))
      {
        if (chunk.Type == "text") assistantText += chunk.Content;
        yield return chunk;

        if (chunk.Type == "tool_call" && chunk.ToolCallId is not null)
        {
          hadToolCall = true;
          var toolResult = await ExecuteToolAsync(
            chunk.ToolCallId, chunk.Content, adminUserId, ct, false);

          // If tool needs confirmation, hold it
          if (toolResult.NeedsConfirmation)
          {
            // Save assistant text in history so LLM context continues correctly after confirm
            if (!string.IsNullOrWhiteSpace(assistantText))
              history.Add(new AdminLlmMessage(AdminLlmRole.Assistant, assistantText));

            var actionId = Guid.NewGuid().ToString("N");
            _pendingStore.Add(actionId, new AdminPendingAction(
              actionId, chunk.ToolCallId,
              toolResult.Description,
              toolResult.RiskLevel.ToString(),
              DateTime.UtcNow,
              conversationId,
              chunk.Content,
              assistantText));

            yield return new LlmChunk("confirmation", toolResult.Description, chunk.ToolCallId, actionId);
            yield return new LlmChunk("done", "", null, null);
            yield break; // Stop until user confirms
          }

          // Add tool result to history (non-confirmation path)
          history.Add(new AdminLlmMessage(AdminLlmRole.User,
            $"[Kết quả từ công cụ '{chunk.ToolCallId}']: {toolResult.Content}"));
          assistantText = ""; // reset for next iteration
        }
      }

      if (!hadToolCall) break;
    }
  }

  public async Task<bool> ConfirmActionAsync(string actionId, bool approved, Guid adminUserId, CancellationToken ct)
  {
    if (_pendingStore.Remove(actionId) is not { } pending)
    {
      _logger.LogWarning("[AdminAgent] Pending action {ActionId} not found", actionId);
      return false;
    }

    _logger.LogInformation("[AdminAgent] Action {ActionId} {Result} by admin {AdminId}",
      actionId, approved ? "approved" : "rejected", adminUserId);

    // Find the conversation and add the tool result
    if (pending.ConversationId is not null
      && _conversationStore.TryGetValue(pending.ConversationId, out var conv))
    {
      if (approved)
      {
        // Execute the tool and add result to history
        var toolResult = await ExecuteToolAsync(
          pending.ToolName, pending.ToolArgsJson ?? "{}", adminUserId, ct, skipConfirmation: true);
        conv.History.Add(new AdminLlmMessage(AdminLlmRole.User,
          $"[Kết quả từ công cụ '{pending.ToolName}']: {toolResult.Content}"));
      }
      else
      {
        conv.History.Add(new AdminLlmMessage(AdminLlmRole.User,
          $"[Người dùng đã từ chối thực hiện hành động '{pending.ToolName}']"));
      }
    }

    return true;
  }

  public async Task<IReadOnlyList<AdminAiSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct)
  {
    var suggestions = new List<AdminAiSuggestionResponse>();

    try
    {
      // Pending orders
      var pendingOrders = await _orders.GetOrdersAsync("pending", 5, ct);
      if (pendingOrders.Count > 0)
      {
        suggestions.Add(new("s1",
          "🔔 Đơn hàng chờ xác nhận",
          $"Có {pendingOrders.Count} đơn hàng mới cần xác nhận. Tổng: {pendingOrders.Sum(o => o.TotalAmount):N0}đ",
          "/admin/orders"));
      }

      // Low inventory
      var inv = await _inventory.GetInventorySummaryAsync(10, ct);
      if (inv.OutOfStockCount > 0)
      {
        suggestions.Add(new("s2",
          "🔴 Sản phẩm hết hàng",
          $"{inv.OutOfStockCount} sản phẩm đã hết hàng. Cần nhập thêm ngay.",
          "/admin/products"));
      }
      else if (inv.LowStockCount > 0)
      {
        suggestions.Add(new("s3",
          "⚠️ Tồn kho thấp",
          $"{inv.LowStockCount} sản phẩm sắp hết (dưới 10 cái).",
          "/admin/products"));
      }

      // Store health
      var health = await _inventory.GetStoreHealthScoreAsync(ct);
      if (health.Overall < 70)
      {
        suggestions.Add(new("s4",
          $"🟠 Sức khỏe cửa hàng: {health.Overall}/100",
          health.Summary,
          "/admin/dashboard"));
      }

      // Recent reviews needing response
      var reviews = await _reviews.GetRecentReviewsAsync(5, ct);
      var lowReviews = reviews.Where(r => r.Rating <= 2).ToList();
      if (lowReviews.Count > 0)
      {
        suggestions.Add(new("s5",
          "⭐ Đánh giá tiêu cực",
          $"Có {lowReviews.Count} đánh giá 1-2 sao cần phản hồi.",
          "/admin/products"));
      }

      // Revenue trend
      var summary = await _dashboard.GetSummaryAsync(ct);
      if (summary.TotalOrders > 0)
      {
        suggestions.Add(new("s6",
          $"📊 Doanh thu: {summary.TotalRevenue:N0}đ",
          $"{summary.TotalOrders} đơn hàng. {(summary.RevenueGrowth >= 0 ? "Tăng" : "Giảm")} {Math.Abs(summary.RevenueGrowth):P0} so với kỳ trước.",
          "/admin/dashboard"));
      }

      // Auto mode hint
      if (!_autoMode.IsAutoMode)
      {
        suggestions.Add(new("s7",
          "🤖 Bật chế độ tự động",
          "Cho phép AI tự xử lý hành động Medium risk. Hỏi AI: \"Bật chế độ tự động\"",
          "/admin/ai-chat"));
      }
    }
    catch
    {
      // Graceful degradation
      suggestions.Add(new("s_fallback",
        "📊 Xem dashboard",
        "Xem tổng quan cửa hàng",
        "/admin/dashboard"));
    }

    return suggestions;
  }

  // --- Tool execution ---

  private async Task<ToolResult> ExecuteToolAsync(
    string toolName,
    string argsJson,
    Guid adminUserId,
    CancellationToken ct,
    bool skipConfirmation = false)
  {
    var riskLevel = await _safety.ClassifyAsync(toolName, ct);
    _logger.LogInformation("[AdminAgent] Executing tool {ToolName} (risk={RiskLevel}) by {AdminId}",
      toolName, riskLevel, adminUserId);

    try
    {
      using var doc = JsonDocument.Parse(argsJson);
      var args = doc.RootElement;

      var result = toolName switch
      {
        // Dashboard
        "get_dashboard_summary" => await DashboardSummary(ct),
        "get_revenue" => await GetRevenue(GetIntArg(args, "period", 7), ct),
        "get_orders_by_status" => await OrdersByStatus(ct),
        "get_recent_orders" => await RecentOrders(GetIntArg(args, "limit", 10), ct),
        "get_top_products" => await TopProducts(GetIntArg(args, "limit", 5), ct),

        // Products
        "list_products" => await ListProducts(
          GetIntArg(args, "page", 1), GetIntArg(args, "pageSize", 20),
          GetStrArg(args, "search"), GetStrArg(args, "status"), ct),
        "get_product" => await GetProduct(Guid.Parse(GetStrArg(args, "id")!), ct),
        "create_product" => await CreateProduct(args, ct),
        "update_product" => await UpdateProduct(args, ct),
        "delete_product" => await DeleteProduct(Guid.Parse(GetStrArg(args, "id")!), ct),
        "toggle_product_status" => await ToggleProductStatus(
          Guid.Parse(GetStrArg(args, "id")!), GetStrArg(args, "status") ?? "active", ct),

        // Categories
        "list_categories" => await ListCategories(ct),
        "create_category" => await CreateCategory(args, ct),
        "update_category" => await UpdateCategory(args, ct),
        "delete_category" => await DeleteCategory(Guid.Parse(GetStrArg(args, "id")!), ct),

        // Users
        "list_users" => await ListUsers(
          GetIntArg(args, "page", 1), GetIntArg(args, "pageSize", 20),
          GetStrArg(args, "search"), ct),
        "get_user" => await GetUser(Guid.Parse(GetStrArg(args, "id")!), ct),
        "update_user_status" => await UpdateUserStatus(
          Guid.Parse(GetStrArg(args, "id")!), GetStrArg(args, "status") ?? "active", ct),
        "update_user_role" => await UpdateUserRole(
          Guid.Parse(GetStrArg(args, "id")!), GetStrArg(args, "role") ?? "customer", ct),

        // Orders
        "list_orders" => await ListOrders(GetStrArg(args, "status"), GetIntArg(args, "limit", 10), ct),
        "get_order" => await GetOrder(Guid.Parse(GetStrArg(args, "orderId")!), ct),
        "confirm_order" => await UpdateOrderStatus(Guid.Parse(GetStrArg(args, "orderId")!), "confirmed", ct),
        "start_processing_order" => await UpdateOrderStatus(Guid.Parse(GetStrArg(args, "orderId")!), "processing", ct),
        "ship_order" => await ShipOrder(args, ct),
        "cancel_order" => await CancelOrder(Guid.Parse(GetStrArg(args, "orderId")!), ct),

        // Inventory & Store Health
        "get_inventory_summary" => await GetInventorySummary(GetIntArg(args, "threshold", 10), ct),
        "get_store_health_score" => await GetStoreHealthScore(ct),

        // Reviews & Comments
        "list_recent_reviews" => await ListRecentReviews(GetIntArg(args, "limit", 10), ct),
        "list_recent_comments" => await ListRecentComments(GetIntArg(args, "limit", 10), ct),
        "reply_to_review" => await ReplyToComment(adminUserId, args, ct),
        "reply_to_comment" => await ReplyToComment(adminUserId, args, ct),

        // Promotions
        "list_promo_codes" => await ListPromoCodes(ct),
        "create_promo_code" => await CreatePromoCode(args, ct),

        // Purchase Note + Daily Report
        "create_purchase_note" => CreatePurchaseNote(args),
        "generate_daily_report" => await GenerateDailyReport(ct),

        // Autonomy Mode
        "toggle_autonomy" => ToggleAutonomy(args),
        "get_autonomy_status" => GetAutonomyStatus(),

        // Phase 3: Intelligence
        "generate_product_description" => await GenerateProductDescription(args, ct),
        "generate_weekly_report" => await GenerateWeeklyReport(GetIntArg(args, "periodDays", 7), ct),
        "check_inventory_alerts" => await CheckInventoryAlerts(GetIntArg(args, "threshold", 10), ct),

        _ => "❌ Không tìm thấy công cụ này."
      };

      // Check if confirmation is needed (auto mode can bypass Medium risk)
      if (!skipConfirmation && _safety.RequiresConfirmation(riskLevel) && !_autoMode.IsAutoApproved(riskLevel.ToString()))
      {
        return new ToolResult(result, true,
          _safety.GetConfirmationPrompt(toolName, result), riskLevel.ToString());
      }

      return new ToolResult(result, false, result, riskLevel.ToString());
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "[AdminAgent] Tool {ToolName} failed", toolName);
      return new ToolResult($"❌ Lỗi: {ex.Message}", false, ex.Message, riskLevel.ToString());
    }
  }

  // --- Tool implementations ---

  private async Task<string> DashboardSummary(CancellationToken ct)
  {
    var s = await _dashboard.GetSummaryAsync(ct);
    return $"📊 Tổng quan:\n- Tổng doanh thu: {s.TotalRevenue:N0} VND\n- Đơn hàng: {s.TotalOrders}\n- Sản phẩm: {s.TotalProducts}\n- Người dùng: {s.TotalUsers}";
  }

  private async Task<string> GetRevenue(int periodDays, CancellationToken ct)
  {
    var r = await _dashboard.GetRevenueAsync(periodDays, ct);
    return JsonSerializer.Serialize(r);
  }

  private async Task<string> OrdersByStatus(CancellationToken ct)
  {
    var o = await _dashboard.GetOrdersByStatusAsync(ct);
    return JsonSerializer.Serialize(o);
  }

  private async Task<string> RecentOrders(int limit, CancellationToken ct)
  {
    var o = await _dashboard.GetRecentOrdersAsync(limit, ct);
    return JsonSerializer.Serialize(o);
  }

  private async Task<string> TopProducts(int limit, CancellationToken ct)
  {
    var p = await _dashboard.GetTopProductsAsync(limit, ct);
    return JsonSerializer.Serialize(p);
  }

  private async Task<string> ListProducts(int page, int pageSize, string? search, string? status, CancellationToken ct)
  {
    var (items, total) = await _products.GetPagedAsync(search, status, page, pageSize, false, ct);
    return $"📦 Tìm thấy {total} sản phẩm (trang {page}).\n" +
           string.Join("\n", items.Take(10).Select(p => $"- {p.Name} ({p.Status})"));
  }

  private async Task<string> GetProduct(Guid id, CancellationToken ct)
  {
    var p = await _products.GetByIdAsync(id, ct);
    if (p is null) return "❌ Không tìm thấy sản phẩm.";
    return JsonSerializer.Serialize(p);
  }

  private async Task<string> CreateProduct(JsonElement args, CancellationToken ct)
  {
    var name = GetStrArg(args, "name") ?? "Sản phẩm mới";
    var description = GetStrArg(args, "description");
    var productType = GetStrArg(args, "productType") ?? "ao_dai";

    var slug = Slugify(name);
    var categoryIdStr = GetStrArg(args, "categoryId");
    var categoryId = categoryIdStr is not null && Guid.TryParse(categoryIdStr, out var cid) ? cid : Guid.Empty;

    if (categoryId == Guid.Empty)
    {
      // Pick first category as fallback
      var cats = await _categories.GetAllAsync(false, ct);
      var first = cats.FirstOrDefault();
      if (first is null) return "❌ Cần ít nhất một danh mục để tạo sản phẩm.";
      categoryId = first.Id;
    }

    var dto = new CreateProductRequest
    {
      Name = name,
      Slug = slug,
      ProductType = productType,
      CategoryId = categoryId,
      Description = description,
      Status = "draft"
    };

    var result = await _products.CreateAsync(dto, ct);
    return $"✅ Đã tạo sản phẩm '{result.Name}' (ID: {result.Id}, slug: {result.Slug}) ở trạng thái nháp.";
  }

  private async Task<string> UpdateProduct(JsonElement args, CancellationToken ct)
  {
    var idStr = GetStrArg(args, "id");
    if (idStr is null || !Guid.TryParse(idStr, out var id)) return "❌ Cần ID sản phẩm hợp lệ.";

    var existing = await _products.GetByIdAsync(id, ct);
    if (existing is null) return "❌ Không tìm thấy sản phẩm.";

    var name = GetStrArg(args, "name") ?? existing.Name;
    var description = GetStrArg(args, "description") ?? existing.Description;
    var productType = GetStrArg(args, "productType") ?? existing.ProductType;

    var dto = new UpdateProductRequest
    {
      Name = name,
      Slug = name != existing.Name ? Slugify(name) : existing.Slug,
      ProductType = productType,
      CategoryId = existing.CategoryId,
      Description = description,
      Status = existing.Status
    };

    var result = await _products.UpdateAsync(id, dto, ct);
    return result is null ? "❌ Không tìm thấy sản phẩm." : $"✅ Đã cập nhật sản phẩm '{result.Name}'.";
  }

  private async Task<string> DeleteProduct(Guid id, CancellationToken ct)
  {
    var ok = await _products.DeleteAsync(id, ct);
    return ok ? "✅ Đã xóa sản phẩm." : "❌ Không tìm thấy sản phẩm.";
  }

  private async Task<string> ToggleProductStatus(Guid id, string status, CancellationToken ct)
  {
    var ok = await _products.ToggleStatusAsync(id, status, ct);
    return ok ? $"✅ Đã chuyển trạng thái sản phẩm thành '{status}'." : "❌ Không tìm thấy sản phẩm.";
  }

  private async Task<string> ListCategories(CancellationToken ct)
  {
    var cats = await _categories.GetAllAsync(false, ct);
    return string.Join("\n", cats.Select(c => $"- {c.Name} (ID: {c.Id})"));
  }

  private async Task<string> CreateCategory(JsonElement args, CancellationToken ct)
  {
    var name = GetStrArg(args, "name") ?? "Danh mục mới";
    var description = GetStrArg(args, "description");
    var slug = Slugify(name);
    var dto = new CreateCategoryRequest { Name = name, Slug = slug, Description = description };
    var result = await _categories.CreateAsync(dto, ct);
    return $"✅ Đã tạo danh mục '{result.Name}' (ID: {result.Id}).";
  }

  private async Task<string> UpdateCategory(JsonElement args, CancellationToken ct)
  {
    var idStr = GetStrArg(args, "id");
    if (idStr is null || !Guid.TryParse(idStr, out var id)) return "❌ Cần ID danh mục hợp lệ.";
    var name = GetStrArg(args, "name");
    var description = GetStrArg(args, "description");
    var dto = new UpdateCategoryRequest
    {
      Name = name ?? "Danh mục",
      Slug = Slugify(name ?? "Danh mục"),
      Description = description
    };
    var result = await _categories.UpdateAsync(id, dto, ct);
    return result is null ? "❌ Không tìm thấy danh mục." : $"✅ Đã cập nhật danh mục '{result.Name}'.";
  }

  private async Task<string> DeleteCategory(Guid id, CancellationToken ct)
  {
    var ok = await _categories.DeleteAsync(id, ct);
    return ok ? "✅ Đã xóa danh mục." : "❌ Không tìm thấy danh mục.";
  }

  private async Task<string> ListUsers(int page, int pageSize, string? search, CancellationToken ct)
  {
    var r = await _users.GetUsersAsync(search, page, pageSize, false, ct);
    return $"👥 Tìm thấy {r.TotalCount} người dùng (trang {page}).\n" +
           string.Join("\n", r.Items.Take(10).Select(u => $"- {u.FullName ?? u.Email} ({u.Status})"));
  }

  private async Task<string> GetUser(Guid id, CancellationToken ct)
  {
    var u = await _users.GetUserByIdAsync(id, ct);
    return u is null ? "❌ Không tìm thấy người dùng." : JsonSerializer.Serialize(u);
  }

  private async Task<string> UpdateUserStatus(Guid id, string status, CancellationToken ct)
  {
    var ok = await _users.UpdateUserStatusAsync(id, new UpdateUserStatusRequest { Status = status }, ct);
    return ok ? $"✅ Đã chuyển trạng thái người dùng thành '{status}'." : "❌ Không tìm thấy người dùng.";
  }

  private async Task<string> UpdateUserRole(Guid id, string role, CancellationToken ct)
  {
    // Map role name to role ID via existing role list
    var roles = await _roles.GetRolesAsync(ct);
    var targetRole = roles.FirstOrDefault(r => r.Name?.Equals(role, StringComparison.OrdinalIgnoreCase) == true);
    if (targetRole is null)
      return $"❌ Không tìm thấy vai trò '{role}'. Các vai trò hiện có: {string.Join(", ", roles.Select(r => r.Name))}";

    var ok = await _users.UpdateUserRoleAsync(id, new UpdateUserRoleRequest { RoleId = targetRole.Id }, ct);
    return ok ? $"✅ Đã đổi vai trò người dùng thành '{role}'." : "❌ Không tìm thấy người dùng.";
  }

  // --- Order tools ---

  private async Task<string> ListOrders(string? status, int limit, CancellationToken ct)
  {
    var orders = await _orders.GetOrdersAsync(status, limit, ct);
    if (orders.Count == 0)
      return status is not null
        ? $"📦 Không có đơn hàng nào ở trạng thái '{status}'."
        : "📦 Chưa có đơn hàng nào.";

    var statusLabel = status is not null ? $" ({status})" : "";
    return $"📦 {orders.Count} đơn hàng{statusLabel}:\n" +
           string.Join("\n", orders.Select(o =>
             $"- [{o.OrderCode}] {o.CustomerName ?? "Khách"} — {o.TotalAmount:N0}đ ({o.OrderStatus}) — {o.ItemCount} sản phẩm"));
  }

  private async Task<string> GetOrder(Guid orderId, CancellationToken ct)
  {
    var order = await _orders.GetOrderByIdAsync(orderId, ct);
    if (order is null) return "❌ Không tìm thấy đơn hàng.";

    var items = string.Join("\n", order.Items.Select(i =>
      $"  - {i.ProductName} ({i.Size}/{i.Color}) x{i.Quantity} = {i.LineTotal:N0}đ"));

    return $@"📋 Đơn hàng {order.OrderCode}
Khách: {order.CustomerName} ({order.CustomerEmail})
Địa chỉ: {order.AddressLine}, {order.Ward}, {order.District}, {order.Province}
Trạng thái: {order.OrderStatus}
Tạm tính: {order.Subtotal:N0}đ
Giảm giá: {order.DiscountAmount:N0}đ
Phí ship: {order.ShippingFee:N0}đ
Tổng: {order.TotalAmount:N0}đ
Ghi chú: {order.Note ?? "Không có"}
Sản phẩm:
{items}";
  }

  private async Task<string> UpdateOrderStatus(Guid orderId, string newStatus, CancellationToken ct)
  {
    var result = await _orders.UpdateStatusAsync(orderId, newStatus, ct);
    return result.Success
      ? $"✅ Đã chuyển đơn hàng sang trạng thái '{result.NewStatus}'."
      : $"❌ {result.ErrorMessage}";
  }

  private async Task<string> ShipOrder(JsonElement args, CancellationToken ct)
  {
    var orderId = Guid.Parse(GetStrArg(args, "orderId")!);
    var carrier = GetStrArg(args, "carrier");
    var trackingNumber = GetStrArg(args, "trackingNumber");

    var result = await _orders.CreateShipmentAsync(orderId, carrier, trackingNumber, ct);
    return result.Success
      ? $"✅ Đã tạo shipment cho đơn hàng. Trạng thái: {result.NewStatus}."
      : $"❌ {result.ErrorMessage}";
  }

  private async Task<string> CancelOrder(Guid orderId, CancellationToken ct)
  {
    var result = await _orders.CancelOrderAsync(orderId, ct);
    return result.Success
      ? $"✅ Đã hủy đơn hàng. Stock đã được hoàn lại."
      : $"❌ {result.ErrorMessage}";
  }

  // --- Inventory & Store Health tools ---

  private async Task<string> GetInventorySummary(int threshold, CancellationToken ct)
  {
    var inv = await _inventory.GetInventorySummaryAsync(threshold, ct);
    if (inv.LowStockCount == 0)
      return $"✅ Tất cả sản phẩm đều có tồn kho trên {threshold} đơn vị.\n" +
             $"Tổng: {inv.TotalProducts} sản phẩm, {inv.TotalVariants} biến thể.";

    var items = string.Join("\n", inv.LowStockItems.Take(15).Select(i =>
      i.StockQty <= 0
        ? $"  🔴 {i.ProductName} ({i.Size}/{i.Color}): HẾT HÀNG"
        : $"  ⚠️ {i.ProductName} ({i.Size}/{i.Color}): còn {i.StockQty}"));

    return $"📦 TỒN KHO ({inv.TotalProducts} sản phẩm, {inv.TotalVariants} biến thể):\n" +
           $"- Hết hàng: {inv.OutOfStockCount}\n" +
           $"- Tồn kho thấp (<={threshold}): {inv.LowStockCount}\n\n" +
           $"Sản phẩm cần chú ý:\n{items}";
  }

  private async Task<string> GetStoreHealthScore(CancellationToken ct)
  {
    var h = await _inventory.GetStoreHealthScoreAsync(ct);

    var emoji = h.Overall switch
    {
      >= 85 => "🟢",
      >= 70 => "🟡",
      >= 50 => "🟠",
      _ => "🔴"
    };

    return $@"{emoji} STORE HEALTH SCORE: {h.Overall}/100

{h.Summary}

Chi tiết:
- Tỷ lệ hoàn thành đơn: {h.FulfillmentRate}%
- Sức khỏe tồn kho: {h.StockHealth}%
- Xu hướng doanh thu: {h.RevenueTrend}%
- Hài lòng khách hàng: {h.CustomerSatisfaction}%";
  }

  // --- Review & Comment tools ---

  private async Task<string> ListRecentReviews(int limit, CancellationToken ct)
  {
    var reviews = await _reviews.GetRecentReviewsAsync(limit, ct);
    if (reviews.Count == 0)
      return "⭐ Chưa có đánh giá nào.";

    return $"⭐ {reviews.Count} đánh giá gần đây:\n" +
           string.Join("\n", reviews.Select(r =>
           {
             var stars = new string('⭐', r.Rating);
             return $"- {stars} [{r.ProductName}] {r.UserName}: {(r.Content.Length > 80 ? r.Content[..80] + "…" : r.Content)}";
           }));
  }

  private async Task<string> ListRecentComments(int limit, CancellationToken ct)
  {
    var comments = await _reviews.GetRecentCommentsAsync(limit, ct);
    if (comments.Count == 0)
      return "💬 Chưa có bình luận nào.";

    return $"💬 {comments.Count} bình luận gần đây:\n" +
           string.Join("\n", comments.Select(c =>
           {
             var isReply = c.ParentCommentId.HasValue ? " (trả lời)" : "";
             return $"- [{c.ProductName}]{isReply} {c.UserName}: {(c.Content.Length > 80 ? c.Content[..80] + "…" : c.Content)}";
           }));
  }

  private async Task<string> ReplyToComment(Guid adminUserId, JsonElement args, CancellationToken ct)
  {
    var commentIdStr = GetStrArg(args, "commentId");
    var productIdStr = GetStrArg(args, "productId");
    var content = GetStrArg(args, "content");

    if (commentIdStr is null || !Guid.TryParse(commentIdStr, out var commentId))
      return "❌ Cần ID bình luận hợp lệ.";
    if (productIdStr is null || !Guid.TryParse(productIdStr, out var productId))
      return "❌ Cần ID sản phẩm hợp lệ.";
    if (string.IsNullOrWhiteSpace(content))
      return "❌ Nội dung phản hồi không được để trống.";

    var result = await _reviews.ReplyToCommentAsync(adminUserId, commentId, productId, content, ct);
    return result.Success
      ? $"✅ {result.Message}"
      : $"❌ {result.Message}";
  }

  // --- Autonomy Mode tools ---

  private string ToggleAutonomy(JsonElement args)
  {
    var enabled = args.TryGetProperty("enabled", out var el) && el.GetBoolean();
    if (enabled) _autoMode.Enable();
    else _autoMode.Disable();

    _logger.LogInformation("[AdminAgent] Autonomy mode {State} by admin", enabled ? "ENABLED" : "DISABLED");

    return enabled
      ? "🤖 Chế độ tự động đã BẬT. Các hành động Medium risk sẽ được tự động thực hiện. High/Critical vẫn cần xác nhận."
      : "🔒 Chế độ tự động đã TẮT. Tất cả hành động Medium+ cần xác nhận thủ công.";
  }

  private string GetAutonomyStatus()
  {
    var isOn = _autoMode.IsAutoMode;
    return isOn
      ? "🤖 Chế độ tự động: ĐANG BẬT\n- Read/Low/Medium: tự động duyệt\n- High/Critical: cần xác nhận"
      : "🔒 Chế độ tự động: ĐANG TẮT\n- Read/Low: tự động duyệt\n- Medium/High/Critical: cần xác nhận";
  }

  // --- Purchase Note + Daily Report tools ---

  private string CreatePurchaseNote(JsonElement args)
  {
    var productName = GetStrArg(args, "productName") ?? "Sản phẩm";
    var quantity = GetIntArg(args, "quantity", 0);
    var note = GetStrArg(args, "note");

    if (quantity <= 0)
      return "❌ Số lượng nhập phải lớn hơn 0.";

    var noteText = string.IsNullOrWhiteSpace(note) ? "" : $"\nGhi chú: {note}";
    return $"📝 PHIẾU NHẬP HÀNG (Draft)\n" +
           $"- Sản phẩm: {productName}\n" +
           $"- Số lượng: {quantity}\n" +
           $"- Trạng thái: Chờ xác nhận{noteText}\n\n" +
           $"💡 Lưu ý: Đây là ghi chú nháp. Cần liên hệ nhà cung cấp để đặt hàng.";
  }

  private async Task<string> GenerateDailyReport(CancellationToken ct)
  {
    var summary = await _dashboard.GetSummaryAsync(ct);
    var ordersByStatus = await _dashboard.GetOrdersByStatusAsync(ct);
    var topProducts = await _dashboard.GetTopProductsAsync(3, ct);
    var revenue = await _dashboard.GetRevenueAsync(1, ct);

    var todayRevenue = revenue.Points.FirstOrDefault()?.Revenue ?? 0m;
    var todayOrders = revenue.Points.FirstOrDefault()?.Orders ?? 0;

    var report = $@"📊 BÁO CÁO HÔM NAY

TỔNG QUAN:
- Doanh thu hôm nay: {todayRevenue:N0} VND
- Đơn hàng hôm nay: {todayOrders}
- Tổng doanh thu kỳ: {summary.TotalRevenue:N0} VND
- Tổng đơn hàng: {summary.TotalOrders}

ĐƠN HÀNG THEO TRẠNG THÁI:
- Chờ xử lý: {ordersByStatus.Pending}
- Đã xác nhận: {ordersByStatus.Confirmed}
- Đang xử lý: {ordersByStatus.Processing}
- Đang giao: {ordersByStatus.Shipping}
- Hoàn thành: {ordersByStatus.Completed}
- Hủy: {ordersByStatus.Cancelled}";

    if (topProducts.Count > 0)
    {
      report += "\n\nTOP SẢN PHẨM BÁN CHẠY:";
      foreach (var p in topProducts.Take(3))
        report += $"\n- {p.ProductName}: {p.SoldCount} đã bán, {p.Revenue:N0} VND";
    }

    return report;
  }

  // --- Promo tools ---

  private async Task<string> ListPromoCodes(CancellationToken ct)
  {
    var promos = await _promos.GetAllAsync(ct);
    if (promos.Count == 0)
      return "🎫 Chưa có mã khuyến mãi nào.";

    return $"🎫 {promos.Count} mã khuyến mãi:\n" +
           string.Join("\n", promos.Select(p =>
           {
             var status = p.IsActive ? "✅" : "❌";
             var discount = p.DiscountType == "percentage"
               ? $"{p.DiscountValue}%"
               : $"{p.DiscountValue:N0}đ";
             return $"- {status} {p.Code}: giảm {discount} (đã dùng {p.CurrentUses}/{(p.MaxUses > 0 ? p.MaxUses.ToString() : "∞")})";
           }));
  }

  private async Task<string> CreatePromoCode(JsonElement args, CancellationToken ct)
  {
    var code = GetStrArg(args, "code");
    var discountType = GetStrArg(args, "discountType") ?? "percentage";
    var discountValue = args.TryGetProperty("discountValue", out var dv) ? dv.GetDecimal() : 0m;
    var minOrderAmount = args.TryGetProperty("minOrderAmount", out var mo) ? mo.GetDecimal() : 0m;
    var maxUses = GetIntArg(args, "maxUses", 0);
    var endDateStr = GetStrArg(args, "endDate");

    if (string.IsNullOrWhiteSpace(code))
      return "❌ Cần nhập mã giảm giá.";

    DateTime? endDate = null;
    if (endDateStr is not null && DateTime.TryParse(endDateStr, out var parsed))
      endDate = parsed;

    var request = new CreateAdminPromoRequest(
      code, discountType, discountValue, minOrderAmount, maxUses, null, endDate);

    var result = await _promos.CreateAsync(request, ct);
    return result.Success
      ? $"✅ {result.Message}"
      : $"❌ {result.Message}";
  }

  // --- Phase 3: Intelligence tools ---

  private async Task<string> GenerateProductDescription(JsonElement args, CancellationToken ct)
  {
    var idStr = GetStrArg(args, "productId");
    if (idStr is null || !Guid.TryParse(idStr, out var id)) return "❌ Cần ID sản phẩm hợp lệ.";

    var product = await _products.GetByIdAsync(id, ct);
    if (product is null) return "❌ Không tìm thấy sản phẩm.";

    var focus = GetStrArg(args, "focus") ?? "all";

    // Build a structured description from existing data + LLM prompt
    var description = $@"SẢN PHẨM: {product.Name}
Loại: {product.ProductType}
Danh mục: {product.CategoryName}
Chất liệu: {product.Material ?? "Chưa có"}
Thương hiệu: {product.Brand ?? "Nhã Uyên"}
Xuất xứ: {product.Origin ?? "Việt Nam"}
Hướng dẫn bảo quản: {product.CareInstruction ?? "Chưa có"}
Mô tả hiện tại: {product.ShortDescription ?? "Chưa có"}

Yêu cầu: Viết mô tả sản phẩm bằng tiếng Việt, giọng trang trọng, tập trung vào: {focus}.
Mô tả nên dài khoảng 3-5 câu, nêu bật chất liệu cao cấp, thiết kế tinh tế, và dịp phù hợp để mặc.";

    // Use LLM to generate
    var history = new List<AdminLlmMessage>
    {
      new(AdminLlmRole.System, "Bạn là copywriter cho thương hiệu áo dài cao cấp Nhã Uyên. Viết mô tả sản phẩm bằng tiếng Việt, giọng trang nhã, tinh tế. Chỉ trả về phần mô tả, không thêm lời dẫn."),
      new(AdminLlmRole.User, description)
    };

    var sb = new System.Text.StringBuilder();
    await foreach (var chunk in _llm.StreamChatAsync(history, [], ct))
    {
      if (chunk.Type == "text") sb.Append(chunk.Content);
    }

    var generated = sb.ToString().Trim();
    if (string.IsNullOrWhiteSpace(generated))
      return "❌ Không thể tạo mô tả. Kiểm tra cấu hình Google AI.";

    return $"📝 Mô tả cho '{product.Name}':\n\n{generated}\n\n💡 Để áp dụng mô tả này, dùng tool update_product với id={product.Id}.";
  }

  private async Task<string> GenerateWeeklyReport(int periodDays, CancellationToken ct)
  {
    var summary = await _dashboard.GetSummaryAsync(ct);
    var revenue = await _dashboard.GetRevenueAsync(periodDays, ct);
    var ordersByStatus = await _dashboard.GetOrdersByStatusAsync(ct);
    var topProducts = await _dashboard.GetTopProductsAsync(5, ct);

    var report = $@"📊 BÁO CÁO {periodDays} NGÀY

TỔNG QUAN:
- Doanh thu: {summary.TotalRevenue:N0} VND
- Đơn hàng: {summary.TotalOrders} (tăng {summary.OrdersGrowth:P1})
- Người dùng: {summary.TotalUsers} (tăng {summary.UsersGrowth:P1})
- Sản phẩm: {summary.TotalProducts}

ĐƠN HÀNG THEO TRẠNG THÁI:
- Chờ xử lý: {ordersByStatus.Pending}
- Đã xác nhận: {ordersByStatus.Confirmed}
- Đang xử lý: {ordersByStatus.Processing}
- Đang giao: {ordersByStatus.Shipping}
- Hoàn thành: {ordersByStatus.Completed}
- Hủy: {ordersByStatus.Cancelled}

TOP 5 SẢN PHẨM:";

    foreach (var p in topProducts.Take(5))
    {
      report += $"\n- {p.ProductName}: {p.SoldCount} đã bán, {p.Revenue:N0} VND";
    }

    return report;
  }

  private async Task<string> CheckInventoryAlerts(int threshold, CancellationToken ct)
  {
    var (items, totalCount) = await _products.GetPagedAsync(null, "active", 1, 200, false, ct);

    // Check each product's variant stock — but list_products doesn't return variants.
    // For MVP, check total from dashboard context.
    var summary = await _dashboard.GetSummaryAsync(ct);

    // Get top products to check for low sellers
    var topProducts = await _dashboard.GetTopProductsAsync(10, ct);

    var lowAlerts = new List<string>();

    // Build alert list from products that might be underperforming
    foreach (var p in items.Take(50))
    {
      var detail = await _products.GetByIdAsync(p.Id, ct);
      if (detail is null) continue;

      var totalStock = detail.Variants?.Sum(v => v.StockQty) ?? 0;
      if (totalStock <= threshold)
      {
        lowAlerts.Add($"- {detail.Name}: chỉ còn {totalStock} sản phẩm (tổng các size)");
      }
    }

    if (lowAlerts.Count == 0)
    {
      return $"✅ Tất cả sản phẩm đều có tồn kho trên {threshold} đơn vị.";
    }

    return $"⚠️ CẢNH BÁO TỒN KHO THẤP (<={threshold}):\n\n{string.Join("\n", lowAlerts.Take(15))}";
  }

  // --- Helpers ---

  private static string? GetStrArg(JsonElement args, string name)
  {
    if (args.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.String)
      return el.GetString();
    return null;
  }

  private static int GetIntArg(JsonElement args, string name, int defaultValue)
  {
    if (args.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.Number)
      return el.GetInt32();
    return defaultValue;
  }

  private static ToolDefinition T(string name, string desc, Dictionary<string, object?> parameters) =>
    new(name, desc, parameters);

  private static Dictionary<string, object?> P(params (string name, Dictionary<string, object?> def)[] props)
  {
    var d = new Dictionary<string, object?>();
    foreach (var (n, def) in props) d[n] = def;
    d["type"] = "object";
    d["properties"] = props.ToDictionary(p => p.name, p => (object)p.def);
    return d;
  }

  private static Dictionary<string, object?> O(string type, string? desc = null)
  {
    var d = new Dictionary<string, object?> { ["type"] = type };
    if (desc is not null) d["description"] = desc;
    return d;
  }

  private static string Slugify(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return "untitled";
    // Simple Vietnamese slug: lowercase, replace spaces with hyphens, strip special chars
    var normalized = new string(text
      .Normalize(System.Text.NormalizationForm.FormD)
      .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
      .ToArray());
    var slug = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9\s-]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
    slug = slug.Trim('-').ToLowerInvariant();
    if (slug.Length > 200) slug = slug[..200];
    if (string.IsNullOrEmpty(slug)) slug = "untitled";
    // Append short random suffix for uniqueness
    return $"{slug}-{Random.Shared.Next(1000, 9999)}";
  }

  private sealed record ToolResult(string Content, bool NeedsConfirmation, string Description, string RiskLevel);
}
