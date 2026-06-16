using System.Text.Encodings.Web;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
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
  private readonly IBlogAiDraftService _blogAiDrafts;
  private readonly IAutoModeStore _autoMode;
  private readonly ILogger<AdminAgentService> _logger;

  private static readonly JsonSerializerOptions ToolResultJsonOptions = new(JsonSerializerDefaults.Web)
  {
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  private readonly IPendingActionStore _pendingStore;
  private readonly IConversationStore _conversationStore;
  private readonly IAdminChatPersistence _chatPersistence;

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
    IBlogAiDraftService blogAiDrafts,
    IAutoModeStore autoMode,
    ILogger<AdminAgentService> logger,
    IPendingActionStore pendingStore,
    IConversationStore conversationStore,
    IAdminChatPersistence chatPersistence)
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
    _blogAiDrafts = blogAiDrafts;
    _autoMode = autoMode;
    _logger = logger;
    _pendingStore = pendingStore;
    _conversationStore = conversationStore;
    _chatPersistence = chatPersistence;
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

    T("get_top_products", "Lấy top sản phẩm bán chạy theo doanh số; không phải toàn bộ catalog.",
      P(("limit", O("integer", "Số lượng sản phẩm. Mặc định: 5")))),

    // Products
    T("list_products", "Search/list sản phẩm admin với phân trang và lọc. DÙNG KHI admin hỏi sản phẩm bằng tên/nhóm hoặc duyệt catalog. KẾT QUẢ có total,page,pageSize,totalPages,hasMore,filtersApplied,completeness. KHÔNG kết luận không có sản phẩm trừ khi total == 0. Nếu items rỗng nhưng total > 0 hoặc hasMore=true, kiểm tra page/search tiếp.",
      P(
        ("page", O("integer", "Trang hiện tại, 1-based, mặc định 1. Dùng >1 khi còn tiếp = có")),
        ("pageSize", O("integer", "Số sản phẩm mỗi trang, mặc định 20; response có thể chỉ hiển thị tối đa 10 mục")),
        ("search", O("string", "Từ khóa từ admin; ưu tiên dùng thay vì page 1 không filter khi tìm sản phẩm/nhóm sản phẩm")),
        ("status", O("string", "Lọc theo trạng thái: active, inactive, draft (tùy chọn)")))),

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
    T("list_categories", "Liệt kê tất cả danh mục. Dùng trước khi tạo danh mục hoặc tạo sản phẩm cần categoryId.", P()),

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
    T("list_users", "Liệt kê người dùng có phân trang. Dùng search theo tên/email/sđt khi admin hỏi người dùng cụ thể; page 1 không đại diện toàn bộ dữ liệu.",
      P(
        ("page", O("integer", "Trang hiện tại, 1-based, mặc định 1. Dùng >1 khi còn tiếp = có")),
        ("pageSize", O("integer", "Số người dùng mỗi trang, mặc định 20; response có thể chỉ hiển thị tối đa 10 mục")),
        ("search", O("string", "Từ khóa tên/email/sđt từ admin; ưu tiên khi tìm người dùng cụ thể")))),

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
    T("list_orders", "Liệt kê đơn hàng theo trạng thái/giới hạn. Nếu admin hỏi một đơn cụ thể, tìm ID rồi dùng get_order để xem chi tiết.",
      P(
        ("status", O("string", "Lọc theo trạng thái: pending, confirmed, processing, shipping, completed, cancelled. Mặc định: tất cả")),
        ("limit", O("integer", "Số lượng đơn hàng. Mặc định: 10; tăng limit nếu cần rà soát thêm")))),

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

    // Blog content
    T("generate_blog_draft", "Tạo bản nháp blog tiếng Việt dạng JSON BlogBlock[] kèm SEO/E-E-A-T để admin mở trong trình soạn. Không tự xuất bản.",
      P(
        ("topic", O("string", "Chủ đề bài viết, bắt buộc")),
        ("targetKeyword", O("string", "Từ khóa SEO chính (tùy chọn)")),
        ("audience", O("string", "Độc giả mục tiêu (tùy chọn)")),
        ("tone", O("string", "Giọng văn: trang nhã, tư vấn, chuyên sâu...")),
        ("template", O("string", "StandardArticle, PhotoGallery, VideoFeature, ProductSpotlight, HowTo")),
        ("length", O("string", "short, standard, long")),
        ("includeFaq", O("boolean", "Có thêm FAQ hay không")),
        ("notes", O("string", "Ghi chú bổ sung từ admin")))),

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

    T("generate_weekly_report", "Tạo báo cáo tuần tổng hợp theo periodDays từ dữ liệu dashboard. Dữ liệu chỉ đúng cho khoảng thời gian đã chọn; khi nhận định sau đó hãy trích cùng số liệu hoặc refresh bằng tool.",
      P(("periodDays", O("integer", "Số ngày phân tích. Mặc định: 7")))),

    T("check_inventory_alerts", "Kiểm tra sản phẩm sắp hết hàng (tồn kho thấp).",
      P(("threshold", O("integer", "Ngưỡng tồn kho thấp. Mặc định: 10")))),
  ];

  public async IAsyncEnumerable<LlmChunk> StreamChatAsync(
    AdminAiChatRequest request,
    Guid adminUserId,
    [EnumeratorCancellation] CancellationToken ct)
  {
    var thread = await ResolveThreadAsync(request.ConversationId, adminUserId, ct);
    var conversationId = thread.Id.ToString();
    var conversation = _conversationStore.GetOrAdd(conversationId, () => (new List<AdminLlmMessage>(), adminUserId));
    if (conversation.AdminUserId != adminUserId)
    {
      _logger.LogWarning("[AdminAgent] Admin {AdminId} attempted to access conversation {ConversationId} owned by {OwnerId}",
        adminUserId, conversationId, conversation.AdminUserId);
      yield return new LlmChunk("error", "Không có quyền truy cập cuộc trò chuyện này.");
      yield break;
    }

    _conversationStore.Touch(conversationId);
    var history = conversation.History;
    if (history.Count == 0)
    {
      var dbMessages = await _chatPersistence.GetMessagesAsync(thread.Id, adminUserId, ct);
      history.AddRange(dbMessages
        .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
        .Select(MapToLlmMessage));
    }
    _conversationStore.TrimHistory(conversationId, 50);

    // Tell frontend the conversation ID so it can continue after confirmations
    yield return new LlmChunk("conversation", conversationId);

    if (!string.IsNullOrWhiteSpace(request.Message))
    {
      history.Add(new AdminLlmMessage(AdminLlmRole.User, request.Message));
      await _chatPersistence.AddMessageAsync(thread.Id, adminUserId, "user", request.Message, null, null, ct);
    }

    var maxIterations = 5;
    for (var iteration = 0; iteration < maxIterations; iteration++)
    {
      var hadToolCall = false;
      var assistantText = "";

      var injectedHistory = BuildInjectableHistory(history);
      await foreach (var chunk in _llm.StreamChatAsync(injectedHistory, Tools, ct))
      {
        if (chunk.Type == "text") assistantText += chunk.Content;
        yield return chunk;

        if (chunk.Type == "tool_call" && chunk.ToolName is not null)
        {
          hadToolCall = true;
          var toolResult = await ExecuteToolAsync(
            chunk.ToolName, chunk.Content, adminUserId, ct, false);

          // If tool needs confirmation, hold it
          if (toolResult.NeedsConfirmation)
          {
            // Save assistant text in history so LLM context continues correctly after confirm
            if (!string.IsNullOrWhiteSpace(assistantText))
            {
              history.Add(new AdminLlmMessage(AdminLlmRole.Assistant, assistantText));
              await _chatPersistence.AddMessageAsync(thread.Id, adminUserId, "assistant", assistantText, null, null, ct);
            }

            var actionId = Guid.NewGuid().ToString("N");
            _pendingStore.Add(actionId, new AdminPendingAction(
              actionId, chunk.ToolName,
              toolResult.Description,
              toolResult.RiskLevel.ToString(),
              DateTime.UtcNow,
              conversationId,
              chunk.Content,
              assistantText,
              adminUserId,
              chunk.ThoughtSignature));

            yield return new LlmChunk("confirmation", toolResult.Description, chunk.ToolName, actionId);
            yield return new LlmChunk("done", "", null, null);
            yield break; // Stop until user confirms
          }

          yield return new LlmChunk("tool_result", toolResult.Content, chunk.ToolName, chunk.ToolCallId);

          // Replay native Gemini tool call + function response in history.
          var toolCall = new AdminLlmMessage(AdminLlmRole.ToolCall, chunk.Content, chunk.ToolName, chunk.ToolCallId, ThoughtSignature: chunk.ThoughtSignature);
          var toolResponseJson = BuildToolResponseJson(toolResult.Content);
          var toolResponse = new AdminLlmMessage(
            AdminLlmRole.ToolResponse,
            toolResult.Content,
            chunk.ToolName,
            chunk.ToolCallId,
            toolResponseJson);
          history.Add(toolCall);
          history.Add(toolResponse);
          await PersistLlmMessageAsync(thread.Id, adminUserId, toolCall, ct);
          await PersistLlmMessageAsync(thread.Id, adminUserId, toolResponse, ct);
          assistantText = ""; // reset for next iteration
        }
      }

      if (!string.IsNullOrWhiteSpace(assistantText))
      {
        history.Add(new AdminLlmMessage(AdminLlmRole.Assistant, assistantText));
        await _chatPersistence.AddMessageAsync(thread.Id, adminUserId, "assistant", assistantText, null, null, ct);
      }

      if (!hadToolCall) break;
    }
  }

  private async Task<ChatThread> ResolveThreadAsync(string? conversationId, Guid adminUserId, CancellationToken ct)
  {
    if (Guid.TryParse(conversationId, out var threadId))
    {
      var existing = await _chatPersistence.GetThreadAsync(threadId, adminUserId, ct);
      if (existing is not null) return existing;
    }

    return await _chatPersistence.CreateThreadAsync(adminUserId, null, ct);
  }

  private static List<AdminLlmMessage> BuildInjectableHistory(List<AdminLlmMessage> history)
  {
    var cleaned = new List<AdminLlmMessage>(history.Count + 1);

    for (var i = 0; i < history.Count; i++)
    {
      var message = history[i];
      if (message.Role == AdminLlmRole.System)
        continue;

      if (message.Role == AdminLlmRole.ToolCall)
      {
        if (string.IsNullOrWhiteSpace(message.ToolName) || i + 1 >= history.Count)
          continue;

        var response = history[i + 1];
        if (response.Role != AdminLlmRole.ToolResponse ||
            string.IsNullOrWhiteSpace(response.ToolName) ||
            !string.Equals(message.ToolName, response.ToolName, StringComparison.Ordinal))
          continue;

        cleaned.Add(message);
        cleaned.Add(response);
        i++;
        continue;
      }

      if (message.Role == AdminLlmRole.ToolResponse)
        continue;

      cleaned.Add(message);
    }

    var summary = BuildVerifiedContextSummary(cleaned);
    if (!string.IsNullOrWhiteSpace(summary))
    {
      var insertAt = cleaned.FindLastIndex(m => m.Role == AdminLlmRole.User);
      var contextMessage = new AdminLlmMessage(AdminLlmRole.User, summary);
      if (insertAt >= 0)
        cleaned.Insert(insertAt, contextMessage);
      else
        cleaned.Insert(0, contextMessage);
    }

    return cleaned;
  }

  private static string? BuildVerifiedContextSummary(List<AdminLlmMessage> history)
  {
    var facts = history
      .Where(m => m.Role == AdminLlmRole.ToolResponse && !string.IsNullOrWhiteSpace(m.ToolName))
      .TakeLast(5)
      .Select(m => $"- tool_verified/{m.ToolName}: {TruncateText(m.Content, 180)}")
      .ToList();

    if (facts.Count == 0) return null;

    var summary = "TÓM TẮT NGỮ CẢNH ĐÃ XÁC MINH (dữ liệu tool thắng assistant_claim/lịch sử cũ):\n" +
      string.Join("\n", facts);
    return TruncateText(summary, 1200);
  }

  private static string TruncateText(string value, int maxLength)
  {
    if (value.Length <= maxLength) return value;
    return value[..maxLength] + "...";
  }

  private async Task PersistLlmMessageAsync(Guid threadId, Guid adminUserId, AdminLlmMessage message, CancellationToken ct)
  {
    var role = message.Role switch
    {
      AdminLlmRole.System => "system",
      AdminLlmRole.User => "user",
      AdminLlmRole.Assistant => "assistant",
      AdminLlmRole.ToolCall => "tool_call",
      AdminLlmRole.ToolResponse => "tool_response",
      _ => "assistant"
    };

    var toolCallsJson = message.Role is AdminLlmRole.ToolCall or AdminLlmRole.ToolResponse
      ? BuildToolCallJson(message.ToolName, message.ToolCallId, message.ThoughtSignature)
      : null;
    var structuredPayloadJson = message.Role == AdminLlmRole.ToolResponse ? message.ToolResponseJson : null;

    await _chatPersistence.AddMessageAsync(threadId, adminUserId, role, message.Content, toolCallsJson, structuredPayloadJson, ct);
  }

  private static AdminLlmMessage MapToLlmMessage(ChatMessage message)
  {
    return message.Role switch
    {
      "user" => new AdminLlmMessage(AdminLlmRole.User, message.Content),
      "assistant" => new AdminLlmMessage(AdminLlmRole.Assistant, message.Content),
      "tool_call" => MapToolCallMessage(message),
      "tool_response" => MapToolResponseMessage(message),
      _ => new AdminLlmMessage(AdminLlmRole.Assistant, message.Content)
    };
  }

  private static AdminLlmMessage MapToolCallMessage(ChatMessage message)
  {
    var (toolName, toolCallId, thoughtSignature) = ReadToolCallJson(message.ToolCallsJsonb);
    return new AdminLlmMessage(AdminLlmRole.ToolCall, message.Content, toolName, toolCallId, ThoughtSignature: thoughtSignature);
  }

  private static AdminLlmMessage MapToolResponseMessage(ChatMessage message)
  {
    var (toolName, toolCallId, thoughtSignature) = ReadToolCallJson(message.ToolCallsJsonb);
    return new AdminLlmMessage(
      AdminLlmRole.ToolResponse,
      message.Content,
      toolName,
      toolCallId,
      message.StructuredPayloadJsonb,
      thoughtSignature);
  }

  private static string BuildToolCallJson(string? toolName, string? toolCallId, string? thoughtSignature) =>
    JsonSerializer.Serialize(new Dictionary<string, string?>
    {
      ["name"] = toolName,
      ["callId"] = toolCallId,
      ["thoughtSignature"] = thoughtSignature
    });

  private static (string? ToolName, string? ToolCallId, string? ThoughtSignature) ReadToolCallJson(string? json)
  {
    if (string.IsNullOrWhiteSpace(json)) return (null, null, null);

    try
    {
      using var doc = JsonDocument.Parse(json);
      var root = doc.RootElement;
      var toolName = root.TryGetProperty("name", out var name) ? name.GetString() : null;
      var toolCallId = root.TryGetProperty("callId", out var callId) ? callId.GetString() : null;
      var thoughtSignature = root.TryGetProperty("thoughtSignature", out var signature) ? signature.GetString() : null;
      return (toolName, toolCallId, thoughtSignature);
    }
    catch (JsonException)
    {
      return (null, null, null);
    }
  }

  public async Task<bool> ConfirmActionAsync(string actionId, bool approved, Guid adminUserId, CancellationToken ct)
  {
    if (_pendingStore.Remove(actionId) is not { } pending)
    {
      _logger.LogWarning("[AdminAgent] Pending action {ActionId} not found", actionId);
      return false;
    }

    if (pending.AdminUserId is not null && pending.AdminUserId != adminUserId)
    {
      _logger.LogWarning("[AdminAgent] Admin {AdminId} attempted to confirm action {ActionId} owned by {OwnerId}",
        adminUserId, actionId, pending.AdminUserId);
      return false;
    }

    _logger.LogInformation("[AdminAgent] Action {ActionId} {Result} by admin {AdminId}",
      actionId, approved ? "approved" : "rejected", adminUserId);

    // Find the conversation and add the tool result
    if (pending.ConversationId is not null
      && _conversationStore.TryGetValue(pending.ConversationId, out var conv))
    {
      if (conv.AdminUserId != adminUserId)
      {
        _logger.LogWarning("[AdminAgent] Admin {AdminId} attempted to continue conversation {ConversationId} owned by {OwnerId}",
          adminUserId, pending.ConversationId, conv.AdminUserId);
        return false;
      }

      if (approved)
      {
        // Execute the tool and add result to history
        var toolResult = await ExecuteToolAsync(
          pending.ToolName, pending.ToolArgsJson ?? "{}", adminUserId, ct, skipConfirmation: true);
        var toolCall = new AdminLlmMessage(AdminLlmRole.ToolCall, pending.ToolArgsJson ?? "{}", pending.ToolName, pending.ActionId, ThoughtSignature: pending.ThoughtSignature);
        var toolResponse = new AdminLlmMessage(
          AdminLlmRole.ToolResponse,
          toolResult.Content,
          pending.ToolName,
          pending.ActionId,
          BuildToolResponseJson(toolResult.Content));
        conv.History.Add(toolCall);
        conv.History.Add(toolResponse);
        if (Guid.TryParse(pending.ConversationId, out var threadId))
        {
          await PersistLlmMessageAsync(threadId, adminUserId, toolCall, ct);
          await PersistLlmMessageAsync(threadId, adminUserId, toolResponse, ct);
        }
      }
      else
      {
        var rejection = $"[Người dùng đã từ chối thực hiện hành động '{pending.ToolName}']";
        conv.History.Add(new AdminLlmMessage(AdminLlmRole.User, rejection));
        if (Guid.TryParse(pending.ConversationId, out var threadId))
          await _chatPersistence.AddMessageAsync(threadId, adminUserId, "user", rejection, null, null, ct);
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

      var lookupRequired = EnforceLookupBeforeWrite(toolName, args);
      if (lookupRequired is not null)
        return new ToolResult(
          SerializeToolError(lookupRequired, "lookup_required", riskLevel.ToString()),
          false,
          lookupRequired,
          riskLevel.ToString());

      var requiresConfirmation = await _safety.RequiresConfirmationAsync(toolName, ct);
      if (!skipConfirmation && requiresConfirmation && !_autoMode.IsAutoApproved(adminUserId, riskLevel.ToString()))
      {
        var description = BuildConfirmationDescription(toolName, args, riskLevel);
        return new ToolResult(string.Empty, true, description, riskLevel.ToString());
      }

      var result = toolName switch
      {
        // Dashboard
        "get_dashboard_summary" => await DashboardSummary(ct),
        "get_revenue" => await GetRevenue(ClampInt(GetIntArg(args, "period", 7), 1, 90), ct),
        "get_orders_by_status" => await OrdersByStatus(ct),
        "get_recent_orders" => await RecentOrders(ClampInt(GetIntArg(args, "limit", 10), 1, 20), ct),
        "get_top_products" => await TopProducts(ClampInt(GetIntArg(args, "limit", 5), 1, 20), ct),

        // Products
        "list_products" => await ListProducts(
          ClampInt(GetIntArg(args, "page", 1), 1, 10000), ClampInt(GetIntArg(args, "pageSize", 20), 1, 50),
          GetStrArg(args, "search"), GetStrArg(args, "status"), ct),
        "get_product" => await GetProduct(RequiredGuid(args, "id"), ct),
        "create_product" => await CreateProduct(args, ct),
        "update_product" => await UpdateProduct(args, ct),
        "delete_product" => await DeleteProduct(RequiredGuid(args, "id"), ct),
        "toggle_product_status" => await ToggleProductStatus(
          RequiredGuid(args, "id"), RequiredEnum(args, "status", "active", "inactive"), ct),

        // Categories
        "list_categories" => await ListCategories(ct),
        "create_category" => await CreateCategory(args, ct),
        "update_category" => await UpdateCategory(args, ct),
        "delete_category" => await DeleteCategory(RequiredGuid(args, "id"), ct),

        // Users
        "list_users" => await ListUsers(
          ClampInt(GetIntArg(args, "page", 1), 1, 10000), ClampInt(GetIntArg(args, "pageSize", 20), 1, 50),
          GetStrArg(args, "search"), ct),
        "get_user" => await GetUser(RequiredGuid(args, "id"), ct),
        "update_user_status" => await UpdateUserStatus(
          RequiredGuid(args, "id"), RequiredEnum(args, "status", "active", "inactive"), ct),
        "update_user_role" => await UpdateUserRole(
          RequiredGuid(args, "id"), RequiredEnum(args, "role", "admin", "customer"), ct),

        // Orders
        "list_orders" => await ListOrders(OptionalEnum(args, "status", "pending", "confirmed", "processing", "shipping", "completed", "cancelled"), ClampInt(GetIntArg(args, "limit", 10), 1, 50), ct),
        "get_order" => await GetOrder(RequiredGuid(args, "orderId"), ct),
        "confirm_order" => await UpdateOrderStatus(RequiredGuid(args, "orderId"), "confirmed", ct),
        "start_processing_order" => await UpdateOrderStatus(RequiredGuid(args, "orderId"), "processing", ct),
        "ship_order" => await ShipOrder(args, ct),
        "cancel_order" => await CancelOrder(RequiredGuid(args, "orderId"), ct),

        // Inventory & Store Health
        "get_inventory_summary" => await GetInventorySummary(ClampInt(GetIntArg(args, "threshold", 10), 0, 500), ct),
        "get_store_health_score" => await GetStoreHealthScore(ct),

        // Reviews & Comments
        "list_recent_reviews" => await ListRecentReviews(ClampInt(GetIntArg(args, "limit", 10), 1, 20), ct),
        "list_recent_comments" => await ListRecentComments(ClampInt(GetIntArg(args, "limit", 10), 1, 20), ct),
        "reply_to_review" => await ReplyToComment(adminUserId, args, ct),
        "reply_to_comment" => await ReplyToComment(adminUserId, args, ct),

        // Promotions
        "list_promo_codes" => await ListPromoCodes(ct),
        "create_promo_code" => await CreatePromoCode(args, ct),

        // Purchase Note + Daily Report
        "create_purchase_note" => CreatePurchaseNote(args),
        "generate_daily_report" => await GenerateDailyReport(ct),

        // Blog content
        "generate_blog_draft" => await GenerateBlogDraft(args, ct),

        // Autonomy Mode
        "toggle_autonomy" => ToggleAutonomy(adminUserId, args),
        "get_autonomy_status" => GetAutonomyStatus(adminUserId),

        // Phase 3: Intelligence
        "generate_product_description" => await GenerateProductDescription(args, ct),
        "generate_weekly_report" => await GenerateWeeklyReport(ClampInt(GetIntArg(args, "periodDays", 7), 1, 90), ct),
        "check_inventory_alerts" => await CheckInventoryAlerts(ClampInt(GetIntArg(args, "threshold", 10), 0, 500), ct),

        _ => "❌ Không tìm thấy công cụ này."
      };

      var wrappedResult = MaybeWrapToolResult(result, riskLevel.ToString(), requiresConfirmation);
      return new ToolResult(wrappedResult, false, wrappedResult, riskLevel.ToString());
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "[AdminAgent] Tool {ToolName} failed", toolName);
      return new ToolResult("❌ Tham số công cụ không hợp lệ hoặc thao tác thất bại. Vui lòng kiểm tra lại dữ liệu đầu vào.", false, "Tool validation or execution failed", riskLevel.ToString());
    }
  }


  private sealed class ToolValidationException(string message) : Exception(message);
  private static int ClampInt(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
  private static Guid RequiredGuid(JsonElement args, string name)
  {
    var value = GetStrArg(args, name);
    if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var guid))
      throw new ToolValidationException($"Thiếu hoặc sai định dạng GUID: {name}.");
    return guid;
  }
  private static string RequiredString(JsonElement args, string name, int maxLength = 500)
  {
    var value = GetStrArg(args, name);
    if (string.IsNullOrWhiteSpace(value)) throw new ToolValidationException($"Thiếu tham số bắt buộc: {name}.");
    value = value.Trim();
    return value.Length <= maxLength ? value : value[..maxLength];
  }
  private static string? GetOptionalString(JsonElement args, string name, int maxLength = 500)
  {
    var value = GetStrArg(args, name);
    if (string.IsNullOrWhiteSpace(value)) return null;
    value = value.Trim();
    return value.Length <= maxLength ? value : value[..maxLength];
  }

  private static string RequiredEnum(JsonElement args, string name, params string[] allowed)
  {
    var value = RequiredString(args, name, 80);
    if (!allowed.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)))
      throw new ToolValidationException($"Giá trị không hợp lệ cho {name}. Cho phép: {string.Join(", ", allowed)}.");
    return value.ToLowerInvariant();
  }
  private static string? OptionalEnum(JsonElement args, string name, params string[] allowed)
  {
    var value = GetStrArg(args, name);
    if (string.IsNullOrWhiteSpace(value)) return null;
    if (!allowed.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)))
      throw new ToolValidationException($"Giá trị không hợp lệ cho {name}. Cho phép: {string.Join(", ", allowed)}.");
    return value.ToLowerInvariant();
  }

  private static string? EnforceLookupBeforeWrite(string toolName, JsonElement args)
  {
    var requiresExplicitId = toolName is
      "update_product" or "delete_product" or "toggle_product_status" or
      "update_user_status" or "update_user_role" or
      "delete_category" or "update_category";

    if (!requiresExplicitId) return null;

    var idName = toolName.Contains("user", StringComparison.OrdinalIgnoreCase)
      ? "id"
      : toolName.Contains("category", StringComparison.OrdinalIgnoreCase)
        ? "id"
        : "id";

    var hasId = args.TryGetProperty(idName, out var idElement)
      && idElement.ValueKind == JsonValueKind.String
      && Guid.TryParse(idElement.GetString(), out _);

    return hasId
      ? null
      : "lookup_required: Cần GUID hợp lệ trước khi thực hiện hành động ghi. Hãy dùng tool list/search/get phù hợp để xác định đúng ID; không tự đoán từ tên.";
  }

  private static string BuildConfirmationDescription(string toolName, JsonElement args, RiskLevel riskLevel)
  {
    var argsPreview = args.GetRawText();
    if (argsPreview.Length > 600) argsPreview = argsPreview[..600] + "...";
    return $"Cần xác nhận hành động {riskLevel}: {toolName}. Tham số: {argsPreview}";
  }

  private static string SerializeToolResult<T>(
    T data,
    string riskLevel = "read",
    bool requiresConfirmation = false,
    AdminToolResultMeta? meta = null,
    string? code = "ok",
    string? message = null)
  {
    var warning = riskLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
      || riskLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
        ? "Hành động này có rủi ro cao. Cần xác nhận từ admin."
        : null;

    return JsonSerializer.Serialize(new AdminToolResult<T>(
      true,
      code,
      message,
      data,
      meta,
      new AdminToolSafety(riskLevel.ToLowerInvariant(), requiresConfirmation, warning)), ToolResultJsonOptions);
  }

  private static string SerializeToolError(
    string message,
    string code = "error",
    string riskLevel = "read") =>
    JsonSerializer.Serialize(new AdminToolResult<object>(
      false,
      code,
      message,
      null,
      null,
      new AdminToolSafety(riskLevel.ToLowerInvariant(), false, null)), ToolResultJsonOptions);

  private static string MaybeWrapToolResult(string result, string riskLevel, bool requiresConfirmation = false)
  {
    if (IsAdminToolResultJson(result)) return result;

    if (TryParseJsonElement(result) is { } parsed)
      return SerializeToolResult(parsed, riskLevel, requiresConfirmation);

    return SerializeToolResult(new { message = result }, riskLevel, requiresConfirmation);
  }

  private static bool IsAdminToolResultJson(string result)
  {
    if (string.IsNullOrWhiteSpace(result)) return false;

    try
    {
      using var doc = JsonDocument.Parse(result);
      var root = doc.RootElement;
      return root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("success", out _)
        && root.TryGetProperty("data", out _);
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private static JsonElement? TryParseJsonElement(string result)
  {
    if (string.IsNullOrWhiteSpace(result)) return null;

    try
    {
      using var doc = JsonDocument.Parse(result);
      return doc.RootElement.Clone();
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static string SerializePaginatedToolResult<T>(
    IReadOnlyCollection<T> items,
    int total,
    int page,
    int pageSize,
    object? filtersApplied = null,
    string riskLevel = "read",
    string? successMessage = null)
  {
    var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
    var hasMore = page < totalPages;
    var completeness = total == 0 ? "empty_result" : hasMore ? "partial_page" : "complete_page";
    var code = items.Count == 0 && total > 0 ? "empty_page_but_results_exist" : "ok";
    var message = code == "empty_page_but_results_exist"
      ? "Trang này không có dữ liệu nhưng vẫn có kết quả ở trang khác."
      : successMessage;

    return SerializeToolResult(
      new { items, total, page, pageSize },
      riskLevel,
      false,
      new AdminToolResultMeta(page, pageSize, total, totalPages, hasMore, completeness, filtersApplied),
      code,
      message);
  }

  private static string BuildToolResponseJson(string content) =>
    JsonSerializer.Serialize(new Dictionary<string, object?>
    {
      ["result"] = content
    });

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
    var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    var cleanStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
    var (items, total) = await _products.GetPagedAsync(cleanSearch, cleanStatus, page, pageSize, false, ct);
    var displayLimit = 10;
    var shown = Math.Min(items.Count, displayLimit);
    var hasMore = page * pageSize < total || items.Count > displayLimit;
    var message = $"Tìm thấy {total} sản phẩm (hiển thị {shown}/{items.Count}, còn tiếp: {(hasMore ? "có" : "không")})." +
                  (hasMore ? " Gợi ý: dùng search cụ thể hoặc page tiếp theo trước khi kết luận thiếu dữ liệu." : string.Empty);
    return SerializePaginatedToolResult(items, total, page, pageSize, new { search = cleanSearch, status = cleanStatus }, successMessage: message);
  }

  private async Task<string> GetProduct(Guid id, CancellationToken ct)
  {
    var p = await _products.GetByIdAsync(id, ct);
    if (p is null) return "❌ Không tìm thấy sản phẩm.";
    return JsonSerializer.Serialize(p);
  }

  private async Task<string> CreateProduct(JsonElement args, CancellationToken ct)
  {
    var name = RequiredString(args, "name", 200);
    var description = GetStrArg(args, "description");
    var productType = OptionalEnum(args, "productType", "ao_dai", "phu_kien") ?? "ao_dai";

    var slug = Slugify(name);
    var categoryId = RequiredGuid(args, "categoryId");

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
    var id = RequiredGuid(args, "id");

    var existing = await _products.GetByIdAsync(id, ct);
    if (existing is null) return "❌ Không tìm thấy sản phẩm.";

    var name = GetOptionalString(args, "name", 200) ?? existing.Name;
    var description = GetOptionalString(args, "description", 2000) ?? existing.Description;
    var productType = OptionalEnum(args, "productType", "ao_dai", "phu_kien") ?? existing.ProductType;

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
    var name = RequiredString(args, "name", 120);
    var description = GetStrArg(args, "description");
    var slug = Slugify(name);
    var dto = new CreateCategoryRequest { Name = name, Slug = slug, Description = description };
    var result = await _categories.CreateAsync(dto, ct);
    return $"✅ Đã tạo danh mục '{result.Name}' (ID: {result.Id}).";
  }

  private async Task<string> UpdateCategory(JsonElement args, CancellationToken ct)
  {
    var id = RequiredGuid(args, "id");
    var existing = await _categories.GetByIdAsync(id, ct);
    if (existing is null) return "❌ Không tìm thấy danh mục.";

    var name = GetOptionalString(args, "name", 120) ?? existing.Name;
    var description = GetOptionalString(args, "description", 1000) ?? existing.Description;
    var dto = new UpdateCategoryRequest
    {
      Name = name,
      Slug = name != existing.Name ? Slugify(name) : existing.Slug,
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
    var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    var r = await _users.GetUsersAsync(cleanSearch, page, pageSize, false, ct);
    return SerializePaginatedToolResult(r.Items, r.TotalCount, page, pageSize, new { search = cleanSearch });
  }

  private async Task<string> GetUser(Guid id, CancellationToken ct)
  {
    var u = await _users.GetUserByIdAsync(id, ct);
    return u is null ? "❌ Không tìm thấy người dùng." : JsonSerializer.Serialize(u);
  }

  private async Task<string> UpdateUserStatus(Guid id, string status, CancellationToken ct)
  {
    var result = await _users.UpdateUserStatusAsync(Guid.Empty, id, new UpdateUserStatusRequest { Status = status }, ct);
    return result.Succeeded ? $"✅ Đã chuyển trạng thái người dùng thành '{status}'." : $"❌ {result.ErrorMessage ?? "Không tìm thấy người dùng."}";
  }

  private async Task<string> UpdateUserRole(Guid id, string role, CancellationToken ct)
  {
    // Map role name to role ID via existing role list
    var roles = await _roles.GetRolesAsync(ct);
    var targetRole = roles.FirstOrDefault(r => r.Name?.Equals(role, StringComparison.OrdinalIgnoreCase) == true);
    if (targetRole is null)
      return $"❌ Không tìm thấy vai trò '{role}'. Các vai trò hiện có: {string.Join(", ", roles.Select(r => r.Name))}";

    var result = await _users.UpdateUserRoleAsync(Guid.Empty, id, new UpdateUserRoleRequest { RoleId = targetRole.Id }, ct);
    return result.Succeeded ? $"✅ Đã đổi vai trò người dùng thành '{role}'." : $"❌ {result.ErrorMessage ?? "Không tìm thấy người dùng."}";
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
    return $"📦 {orders.Count} đơn hàng{statusLabel} (limit={limit}; tăng limit hoặc lọc status nếu cần rà soát thêm):\n" +
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
    var orderId = RequiredGuid(args, "orderId");
    var carrier = GetOptionalString(args, "carrier", 80);
    var trackingNumber = GetOptionalString(args, "trackingNumber", 120);

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
    var commentId = RequiredGuid(args, "commentId");
    var productId = RequiredGuid(args, "productId");
    var content = RequiredString(args, "content", 1000);

    var result = await _reviews.ReplyToCommentAsync(adminUserId, commentId, productId, content, ct);
    return result.Success
      ? $"✅ {result.Message}"
      : $"❌ {result.Message}";
  }

  // --- Autonomy Mode tools ---

  private string ToggleAutonomy(Guid adminUserId, JsonElement args)
  {
    if (!args.TryGetProperty("enabled", out var el) || el.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
      throw new ToolValidationException("Thiếu tham số enabled dạng boolean.");

    var enabled = el.GetBoolean();
    if (enabled) _autoMode.Enable(adminUserId);
    else _autoMode.Disable(adminUserId);

    _logger.LogInformation("[AdminAgent] Autonomy mode {State} by admin {AdminId}", enabled ? "ENABLED" : "DISABLED", adminUserId);

    return enabled
      ? "🤖 Chế độ tự động đã BẬT trong 30 phút cho quản trị viên hiện tại. Các hành động Medium risk sẽ được tự động thực hiện. High/Critical vẫn cần xác nhận."
      : "🔒 Chế độ tự động đã TẮT cho quản trị viên hiện tại. Tất cả hành động Medium+ cần xác nhận thủ công.";
  }

  private string GetAutonomyStatus(Guid adminUserId)
  {
    var isOn = _autoMode.IsAutoModeEnabled(adminUserId);
    return isOn
      ? "🤖 Chế độ tự động: ĐANG BẬT cho quản trị viên hiện tại\n- Read/Low/Medium: tự động duyệt\n- High/Critical: cần xác nhận\n- Tự hết hạn sau tối đa 30 phút"
      : "🔒 Chế độ tự động: ĐANG TẮT\n- Read/Low: tự động duyệt\n- Medium/High/Critical: cần xác nhận";
  }

  // --- Purchase Note + Daily Report tools ---

  private string CreatePurchaseNote(JsonElement args)
  {
    var productName = RequiredString(args, "productName", 200);
    var quantity = ClampInt(GetIntArg(args, "quantity", 0), 1, 10000);
    var note = GetOptionalString(args, "note", 1000);

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
    var code = RequiredString(args, "code", 40).ToUpperInvariant();
    var discountType = RequiredEnum(args, "discountType", "percentage", "fixed");
    var discountValue = discountType == "percentage"
      ? ClampDecimal(GetDecimalArg(args, "discountValue", 0m), 1m, 100m)
      : ClampDecimal(GetDecimalArg(args, "discountValue", 0m), 1000m, 100000000m);
    var minOrderAmount = ClampDecimal(GetDecimalArg(args, "minOrderAmount", 0m), 0m, 1000000000m);
    var maxUses = ClampInt(GetIntArg(args, "maxUses", 0), 0, 100000);
    var endDateStr = GetOptionalString(args, "endDate", 40);

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

  private async Task<string> GenerateBlogDraft(JsonElement args, CancellationToken ct)
  {
    var topic = RequiredString(args, "topic", 500);
    var templateRaw = GetOptionalString(args, "template", 80);
    var template = Enum.TryParse<BlogPostTemplate>(templateRaw, true, out var parsedTemplate)
      ? parsedTemplate
      : BlogPostTemplate.StandardArticle;

    var request = new GenerateBlogDraftRequest
    {
      Topic = topic,
      TargetKeyword = GetOptionalString(args, "targetKeyword", 200),
      Audience = GetOptionalString(args, "audience", 200),
      Tone = GetOptionalString(args, "tone", 100),
      Template = template,
      Length = OptionalEnum(args, "length", "short", "standard", "long") ?? "standard",
      IncludeFaq = GetBoolArg(args, "includeFaq", true),
      Notes = GetOptionalString(args, "notes", 2000)
    };

    var draft = await _blogAiDrafts.GenerateDraftAsync(request, ct);
    return SerializeToolResult(
      new
      {
        kind = "blog_draft",
        draft,
        handoffKey = "admin-blog-ai-draft",
        guidance = "Admin cần mở trong trình soạn, kiểm duyệt, rồi tự lưu/xuất bản."
      },
      "read",
      false,
      code: "blog_draft_generated",
      message: "Đã tạo bản nháp blog AI.");
  }

  private async Task<string> GenerateProductDescription(JsonElement args, CancellationToken ct)
  {
    var id = RequiredGuid(args, "productId");

    var product = await _products.GetByIdAsync(id, ct);
    if (product is null) return "❌ Không tìm thấy sản phẩm.";

    var focus = OptionalEnum(args, "focus", "all", "material", "style", "occasion", "seo") ?? "all";

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

  private static decimal GetDecimalArg(JsonElement args, string name, decimal defaultValue)
  {
    if (args.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.Number && el.TryGetDecimal(out var value))
      return value;
    return defaultValue;
  }

  private static decimal ClampDecimal(decimal value, decimal min, decimal max) => Math.Min(Math.Max(value, min), max);

  private static bool GetBoolArg(JsonElement args, string name, bool defaultValue)
  {
    if (args.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False)
      return el.GetBoolean();
    return defaultValue;
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
