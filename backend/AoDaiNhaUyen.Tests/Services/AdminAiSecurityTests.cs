using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.DTOs.Collections;
using AoDaiNhaUyen.Application.DTOs.Facebook;

using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.DTOs.Order;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using AoDaiNhaUyen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class AdminAiSecurityTests
{
  private static readonly Guid AdminA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  private static readonly Guid AdminB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

  [Fact]
  public async Task StreamChatAsync_InjectsRecentShopEventContext()
  {
    var llm = new ScriptedLlmProvider(new LlmChunk("text", "done"));
    var service = CreateService(
      llm,
      eventContext: new FakeAdminShopEventContextService("NGỮ CẢNH SỰ KIỆN LIVE CỬA HÀNG: checkout_completed Order/AD-TEST"));

    await service.StreamChatAsync(new AdminAiChatRequest("Có gì mới?", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var firstHistory = Assert.Single(llm.HistorySnapshots);
    Assert.Contains(firstHistory, message =>
      message.Role == AdminLlmRole.User &&
      message.Content.Contains("TRUSTED_APP_CONTEXT_BEGIN", StringComparison.Ordinal) &&
      message.Content.Contains("checkout_completed", StringComparison.Ordinal) &&
      message.Content.Contains("TRUSTED_APP_CONTEXT_END", StringComparison.Ordinal));
  }

  [Fact]
  public async Task StreamChatAsync_DoesNotInjectShopEventContext_ForBareAcknowledgment()
  {
    var llm = new ScriptedLlmProvider(new LlmChunk("text", "done"));
    var service = CreateService(
      llm,
      eventContext: new FakeAdminShopEventContextService("NGỮ CẢNH SỰ KIỆN LIVE CỬA HÀNG: checkout_completed Order/AD-TEST"));

    await service.StreamChatAsync(new AdminAiChatRequest("ok", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var firstHistory = Assert.Single(llm.HistorySnapshots);
    Assert.DoesNotContain(firstHistory, message =>
      message.Content.Contains("TRUSTED_APP_CONTEXT_BEGIN", StringComparison.Ordinal));
  }

  [Fact]
  public async Task HighRiskTool_IsNotExecuted_BeforeConfirmation()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", $"{{\"id\":\"{ProductId}\"}}", "delete_product", "call-1"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Xóa sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    Assert.Contains(chunks, c => c.Type == "confirmation" && c.ToolName == "delete_product");
    Assert.Equal(0, products.DeleteCalls);
  }

  [Fact]
  public async Task Reject_DoesNotExecutePendingTool()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", $"{{\"id\":\"{ProductId}\"}}", "delete_product", "call-1"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Xóa sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);
    var actionId = Assert.Single(chunks, c => c.Type == "confirmation").ToolCallId!;

    var ok = await service.ConfirmActionAsync(actionId, approved: false, AdminA, CancellationToken.None);

    Assert.True(ok);
    Assert.Equal(0, products.DeleteCalls);
  }

  [Fact]
  public async Task CrossAdminConfirm_IsBlocked()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", $"{{\"id\":\"{ProductId}\"}}", "delete_product", "call-1"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Xóa sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);
    var actionId = Assert.Single(chunks, c => c.Type == "confirmation").ToolCallId!;

    var ok = await service.ConfirmActionAsync(actionId, approved: true, AdminB, CancellationToken.None);

    Assert.False(ok);
    Assert.Equal(0, products.DeleteCalls);
  }

  [Fact]
  public async Task AutoMode_DoesNotBypassHighRiskConfirmation()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", $"{{\"id\":\"{ProductId}\"}}", "delete_product", "call-1"));
    var autoMode = new AutoModeStore();
    autoMode.Enable(AdminA);
    var products = new FakeProductService();
    var service = CreateService(llm, products: products, autoMode: autoMode);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Xóa sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    Assert.Contains(chunks, c => c.Type == "confirmation" && c.ToolName == "delete_product");
    Assert.Equal(0, products.DeleteCalls);
  }

  [Fact]
  public async Task InvalidGuidToolArgs_ReturnSafeToolError_AndNoExecution()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{\"id\":\"not-a-guid\"}", "get_product", "call-1"),
      new LlmChunk("text", "done"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    await service.StreamChatAsync(new AdminAiChatRequest("Xem sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    Assert.Equal(0, products.GetByIdCalls);
    // The new structured-error path surfaces the validator's precise Vietnamese message
    // (not the old generic "Tham số công cụ không hợp lệ") so the LLM can self-correct.
    Assert.Contains(llm.HistorySnapshots.SelectMany(h => h), m =>
      m.Role == AdminLlmRole.ToolResponse &&
      m.ToolName == "get_product" &&
      m.Content.Contains("sai định dạng GUID: id", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task ToolValidation_PreservesPreciseMessage_ForMissingRequiredArg()
  {
    // get_product with an empty args object — id is missing entirely.
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{}", "get_product", "call-1"),
      new LlmChunk("text", "done"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Xem sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    Assert.Equal(0, products.GetByIdCalls);
    var toolResultChunk = Assert.Single(chunks, c => c.Type == "tool_result" && c.ToolName == "get_product");
    Assert.Contains("Thiếu hoặc sai định dạng GUID: id", toolResultChunk.Content, StringComparison.Ordinal);
    // The validator message must reach the LLM verbatim (with the ❌ prefix) in history
    // so it has actionable feedback to retry with a corrected argument.
    var replayHistory = Assert.Single(llm.HistorySnapshots, h => h.Any(m => m.Role == AdminLlmRole.ToolResponse && m.ToolName == "get_product"));
    var response = Assert.Single(replayHistory, m => m.Role == AdminLlmRole.ToolResponse && m.ToolName == "get_product");
    Assert.Contains("Thiếu hoặc sai định dạng GUID: id", response.Content, StringComparison.Ordinal);
    Assert.DoesNotContain("Tham số công cụ không hợp lệ", response.Content, StringComparison.Ordinal);
  }

  [Fact]
  public async Task BusinessRuleError_PreservesServiceMessage()
  {
    // A service-level business throw (duplicate promo code) must surface verbatim,
    // not be flattened to the generic message. Use create_promo_code which is
    // Read-classified in StaticSafetyGate (no confirmation), so it executes inline.
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{\"code\":\"DUP\",\"discountType\":\"percentage\",\"discountValue\":10}", "create_promo_code", "call-1"),
      new LlmChunk("text", "done"));
    var promos = new ThrowingPromoService(new InvalidOperationException("Mã 'DUP' đã tồn tại."));
    var service = CreateService(llm, promos: promos);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Tạo mã", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var toolResultChunk = Assert.Single(chunks, c => c.Type == "tool_result" && c.ToolName == "create_promo_code");
    Assert.Contains("Mã 'DUP' đã tồn tại", toolResultChunk.Content, StringComparison.Ordinal);
    Assert.DoesNotContain("Tham số công cụ không hợp lệ", toolResultChunk.Content, StringComparison.Ordinal);
  }

  [Fact]
  public async Task FailedTool_RetriesWithinBudget_ThenSucceeds()
  {
    // First call: bad id (validation error). Second call: good id (success). The agent
    // should let the LLM retry within the budget and NOT emit a terminal tool_error.
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{\"id\":\"not-a-guid\"}", "get_product", "call-bad"),
      new LlmChunk("tool_call", $"{{\"id\":\"{ProductId}\"}}", "get_product", "call-good"),
      new LlmChunk("text", "Xong"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Xem sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    // Two tool_call chunks (the retry), no terminal tool_error.
    Assert.Equal(2, chunks.Count(c => c.Type == "tool_call" && c.ToolName == "get_product"));
    Assert.DoesNotContain(chunks, c => c.Type == "tool_error");
    // The good id reached the service exactly once.
    Assert.Equal(1, products.GetByIdCalls);
  }

  [Fact]
  public async Task FailedTool_EmitsTerminalError_AfterBudgetExhausted()
  {
    // The LLM keeps calling get_product with a bad id. After MaxToolRetries+1 attempts,
    // the agent emits a terminal tool_error chunk and ends the turn.
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{\"id\":\"bad\"}", "get_product", "c1"),
      new LlmChunk("tool_call", "{\"id\":\"bad\"}", "get_product", "c2"),
      new LlmChunk("tool_call", "{\"id\":\"bad\"}", "get_product", "c3"),
      new LlmChunk("tool_call", "{\"id\":\"bad\"}", "get_product", "c4"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Xem sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var terminalError = Assert.Single(chunks, c => c.Type == "tool_error");
    Assert.Equal("get_product", terminalError.ToolName);
    Assert.Contains("thất bại sau", terminalError.Content, StringComparison.Ordinal);
    Assert.Contains("sai định dạng GUID: id", terminalError.Content, StringComparison.OrdinalIgnoreCase);
    // The stream ends with done after the terminal error.
    Assert.Equal("done", chunks[^1].Type);
    // The service was never reached (all attempts failed validation before the call).
    Assert.Equal(0, products.GetByIdCalls);
  }

  [Fact]
  public async Task OperationCanceled_SurfacesAsTimeout_NotGenericParamError()
  {
    // A non-cancellation OCE from a downstream service must surface as a timeout error,
    // not be masked as "Tham số công cụ không hợp lệ".
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{}", "get_dashboard_summary", "call-1"),
      new LlmChunk("text", "done"));
    var dashboard = new ThrowingDashboardService(new OperationCanceledException("simulated timeout"));
    var service = CreateService(llm, dashboard: dashboard);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Tổng quan", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var toolResultChunk = Assert.Single(chunks, c => c.Type == "tool_result" && c.ToolName == "get_dashboard_summary");
    Assert.Contains("timeout", toolResultChunk.Content, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("Tham số công cụ không hợp lệ", toolResultChunk.Content, StringComparison.Ordinal);
  }

  [Fact]
  public async Task PageSize_IsClamped_To50()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{\"page\":1,\"pageSize\":999}", "list_products", "call-1"),
      new LlmChunk("text", "done"));
    var products = new FakeProductService();
    var service = CreateService(llm, products: products);

    await service.StreamChatAsync(new AdminAiChatRequest("Liệt kê sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    Assert.Equal(50, products.LastPageSize);
  }

  [Fact]
  public async Task ListProducts_ToolResponse_IncludesPaginationMetadata()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{\"page\":1,\"pageSize\":20}", "list_products", "call-1"),
      new LlmChunk("text", "done"));
    var products = new FakeProductService
    {
      TotalCount = 48,
      Items = Enumerable.Range(1, 20)
        .Select(i => ProductItem($"Áo dài {i}"))
        .ToList()
    };
    var service = CreateService(llm, products: products);

    await service.StreamChatAsync(new AdminAiChatRequest("Liệt kê sản phẩm", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var replayHistory = Assert.Single(llm.HistorySnapshots, h => h.Any(m => m.Role == AdminLlmRole.ToolResponse));
    var response = Assert.Single(replayHistory, m => m.Role == AdminLlmRole.ToolResponse);
    Assert.Contains("Tìm thấy 48 sản phẩm", response.Content);
    Assert.Contains("hiển thị 10/20", response.Content);
    Assert.Contains("còn tiếp: có", response.Content);
    Assert.Contains("Gợi ý: dùng search cụ thể", response.Content);
  }

  [Fact]
  public async Task ToolResult_IsReplayed_AsNativeToolCallAndFunctionResponse()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{}", "get_dashboard_summary", "call-dashboard"),
      new LlmChunk("text", "Tóm tắt xong"));
    var service = CreateService(llm);

    await service.StreamChatAsync(new AdminAiChatRequest("Tóm tắt dashboard", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var replayHistory = Assert.Single(llm.HistorySnapshots, h => h.Any(m => m.Role == AdminLlmRole.ToolResponse));
    Assert.Contains(replayHistory, m => m.Role == AdminLlmRole.ToolCall && m.ToolName == "get_dashboard_summary" && m.ToolCallId == "call-dashboard");
    var response = Assert.Single(replayHistory, m => m.Role == AdminLlmRole.ToolResponse);
    Assert.Equal("get_dashboard_summary", response.ToolName);
    Assert.Equal("call-dashboard", response.ToolCallId);
    Assert.Contains("result", response.ToolResponseJson);
    Assert.DoesNotContain("untrusted_tool_data", response.ToolResponseJson);
    Assert.DoesNotContain("instruction", response.ToolResponseJson);
  }

  [Fact]
  public async Task ToolCallReplay_PreservesThoughtSignature()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{}", "get_dashboard_summary", "call-dashboard", "sig-123"),
      new LlmChunk("text", "Tóm tắt xong"));
    var service = CreateService(llm);

    await service.StreamChatAsync(new AdminAiChatRequest("Tóm tắt dashboard", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var replayHistory = Assert.Single(llm.HistorySnapshots, h => h.Any(m => m.Role == AdminLlmRole.ToolResponse));
    var call = Assert.Single(replayHistory, m => m.Role == AdminLlmRole.ToolCall);
    Assert.Equal("sig-123", call.ThoughtSignature);
  }
  [Fact]
  public async Task GenericChat_Exposes_NewAdminParityTools()
  {
    var llm = new ScriptedLlmProvider(new LlmChunk("text", "done"));
    var service = CreateService(llm);

    await service.StreamChatAsync(new AdminAiChatRequest("Bạn làm được gì?", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    var tools = Assert.Single(llm.ToolSnapshots);
    var names = tools.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
    Assert.Contains("complete_order", names);
    Assert.Contains("update_user_profile", names);
    Assert.Contains("create_role", names);
    Assert.Contains("save_blog_draft", names);
    Assert.Contains("publish_blog_post", names);
    Assert.Contains("list_marketing_options", names);
    Assert.Contains("send_marketing_campaign", names);

    var updateProduct = Assert.Single(tools, t => t.Name == "update_product");
    var updateProductSchema = JsonSerializer.Serialize(updateProduct.Parameters);
    Assert.Contains("shortDescription", updateProductSchema);
    Assert.Contains("careInstruction", updateProductSchema);
    Assert.Contains("isFeatured", updateProductSchema);

    var toggle = Assert.Single(tools, t => t.Name == "toggle_product_status");
    var serialized = JsonSerializer.Serialize(toggle.Parameters);
    Assert.Contains("out_of_stock", serialized);
  }

  [Fact]
  public async Task ListMarketingOptions_ToolExecutes_AsReadOnlyTool()
  {
    var llm = new ScriptedLlmProvider(
      new LlmChunk("tool_call", "{}", "list_marketing_options", "call-marketing"),
      new LlmChunk("text", "done"));
    var marketing = new FakeMarketingCampaignService
    {
      Options = [new MarketingContentOption(Guid.NewGuid(), "promo", "CHAOMUNG", "Giảm 15%", "https://aodainhauyen.io.vn/products", "HSD", "<div>CHAOMUNG</div>")]
    };
    var service = CreateService(llm, marketing: marketing);

    var chunks = await service.StreamChatAsync(new AdminAiChatRequest("Liệt kê lựa chọn marketing", null), AdminA, CancellationToken.None).ToListAsync(CancellationToken.None);

    Assert.DoesNotContain(chunks, c => c.Type == "confirmation");
    Assert.Equal(1, marketing.GetContentOptionsCalls);
    var replayHistory = Assert.Single(llm.HistorySnapshots, h => h.Any(m => m.Role == AdminLlmRole.ToolResponse));
    var response = Assert.Single(replayHistory, m => m.Role == AdminLlmRole.ToolResponse && m.ToolName == "list_marketing_options");
    Assert.Contains("CHAOMUNG", response.Content);
  }

  [Fact]
  public async Task AdminAiPersistence_ListsOnlyAdminAiThreads_ForOwner()
  {
    var persistence = new FakeAdminChatPersistence();
    var ownAdminThread = await persistence.CreateThreadAsync(AdminA, null, CancellationToken.None);
    var otherAdminThread = await persistence.CreateThreadAsync(AdminB, null, CancellationToken.None);
    persistence.Seed(new ChatThread { Id = Guid.NewGuid(), UserId = AdminA, Source = ChatSources.Web, Status = "active" });

    var threads = await persistence.ListThreadsAsync(AdminA, CancellationToken.None);

    Assert.Single(threads);
    Assert.Equal(ownAdminThread.Id, threads[0].Id);
    Assert.DoesNotContain(threads, t => t.Id == otherAdminThread.Id);
    Assert.All(threads, t => Assert.Equal(ChatSources.AdminAi, t.Source));
  }

  [Fact]
  public async Task AdminAiPersistence_BlocksCrossAdminReadAndDelete()
  {
    var persistence = new FakeAdminChatPersistence();
    var thread = await persistence.CreateThreadAsync(AdminA, null, CancellationToken.None);

    var crossRead = await persistence.GetThreadAsync(thread.Id, AdminB, CancellationToken.None);
    await persistence.DeleteThreadAsync(thread.Id, AdminB, CancellationToken.None);
    var ownerReadAfterDeleteAttempt = await persistence.GetThreadAsync(thread.Id, AdminA, CancellationToken.None);

    Assert.Null(crossRead);
    Assert.NotNull(ownerReadAfterDeleteAttempt);
    Assert.False(ownerReadAfterDeleteAttempt!.IsDeleted);
  }

  [Fact]
  public async Task AdminAiPersistence_CannotDeleteCustomerWebThread()
  {
    var persistence = new FakeAdminChatPersistence();
    var webThread = new ChatThread { Id = Guid.NewGuid(), UserId = AdminA, Source = ChatSources.Web, Status = "active" };
    persistence.Seed(webThread);

    await persistence.DeleteThreadAsync(webThread.Id, AdminA, CancellationToken.None);

    Assert.False(webThread.IsDeleted);
  }

  [Fact]
  public async Task AdminAiPersistence_BlocksCrossAdminMessages()
  {
    var persistence = new FakeAdminChatPersistence();
    var thread = await persistence.CreateThreadAsync(AdminA, null, CancellationToken.None);
    await persistence.AddMessageAsync(thread.Id, AdminA, "user", "secret", null, null, CancellationToken.None);

    var crossRead = await persistence.GetMessagesAsync(thread.Id, AdminB, CancellationToken.None);
    var crossWrite = await persistence.AddMessageAsync(thread.Id, AdminB, "user", "evil", null, null, CancellationToken.None);
    var ownerRead = await persistence.GetMessagesAsync(thread.Id, AdminA, CancellationToken.None);

    Assert.Empty(crossRead);
    Assert.Null(crossWrite);
    Assert.Single(ownerRead);
    Assert.Equal("secret", ownerRead[0].Content);
  }

  private static AdminAgentService CreateService(
    IAdminLlmProvider llm,
    FakeProductService? products = null,
    IAutoModeStore? autoMode = null,
    FakeMarketingCampaignService? marketing = null,
    IAdminShopEventContextService? eventContext = null,
    IAdminPromoService? promos = null,
    IAdminDashboardService? dashboard = null)
  {
    return new AdminAgentService(
      llm,
      new StaticSafetyGate(),
      products ?? new FakeProductService(),
      new FakeCategoryService(),
      new FakeCollectionService(),
      new FakeUserService(),
      new FakeRoleService(),
      dashboard ?? new FakeDashboardService(),
      new FakeOrderService(),
      new FakeInventoryService(),
      new FakeReviewService(),
      promos ?? new FakePromoService(),
      new FakeMediaService(),
      new FakeBlogAiDraftService(),
      new FakeBlogPostService(),
      marketing ?? new FakeMarketingCampaignService(),
      new FakeSubscriberService(),
      new FakeEmailJobService(),
      new FakeHermesFeedService(),
      new FakeHermesAgentService(),
      new FakeHermesEventOutboxService(),
      autoMode ?? new AutoModeStore(),
      NullLogger<AdminAgentService>.Instance,
      new PendingActionStore(),
      new ConversationStore(),
      new FakeAdminChatPersistence(),
      eventContext ?? new FakeAdminShopEventContextService(),
      CreateInMemoryDbContext(),
      new FakeFacebookService());
  }

  /// <summary>In-memory AppDbContext for the agent (count_by_created_range uses it).
  /// These tests don't exercise that tool, so an empty DB is fine.</summary>
  private static AppDbContext CreateInMemoryDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"admin-agent-{Guid.NewGuid():N}")
      .Options;
    return new AppDbContext(options);
  }

  private sealed class FakeAdminShopEventContextService(string? context = null) : IAdminShopEventContextService
  {
    public Task<string?> GetRecentContextAsync(CancellationToken cancellationToken = default)
      => Task.FromResult(context);
  }

  private sealed class FakeAdminChatPersistence : IAdminChatPersistence
  {
    private readonly List<ChatThread> _threads = [];
    private readonly List<ChatMessage> _messages = [];

    public void Seed(ChatThread thread) => _threads.Add(thread);

    public Task<ChatThread> CreateThreadAsync(Guid adminUserId, string? title, CancellationToken ct)
    {
      var thread = new ChatThread
      {
        Id = Guid.NewGuid(),
        UserId = adminUserId,
        Source = ChatSources.AdminAi,
        Status = "active",
        GuestKeyHash = null
      };
      _threads.Add(thread);
      return Task.FromResult(thread);
    }

    public Task<ChatThread?> GetThreadAsync(Guid threadId, Guid adminUserId, CancellationToken ct) =>
      Task.FromResult(_threads.FirstOrDefault(t =>
        t.Id == threadId && t.UserId == adminUserId && t.Source == ChatSources.AdminAi && !t.IsDeleted));

    public Task<List<ChatThread>> ListThreadsAsync(Guid adminUserId, CancellationToken ct) =>
      Task.FromResult(_threads
        .Where(t => t.UserId == adminUserId && t.Source == ChatSources.AdminAi && !t.IsDeleted)
        .OrderByDescending(t => t.UpdatedAt)
        .ToList());

    public Task<ChatMessage?> AddMessageAsync(Guid threadId, Guid adminUserId, string role, string content,
      string? toolCallsJson, string? structuredPayloadJson, CancellationToken ct)
    {
      var thread = _threads.FirstOrDefault(t =>
        t.Id == threadId && t.UserId == adminUserId && t.Source == ChatSources.AdminAi && !t.IsDeleted);
      if (thread is null) return Task.FromResult<ChatMessage?>(null);

      var message = new ChatMessage
      {
        Id = Guid.NewGuid(),
        ThreadId = threadId,
        Role = role,
        Content = content,
        ToolCallsJsonb = toolCallsJson,
        StructuredPayloadJsonb = structuredPayloadJson,
        CreatedAt = DateTime.UtcNow
      };
      _messages.Add(message);
      thread.UpdatedAt = DateTime.UtcNow;
      return Task.FromResult<ChatMessage?>(message);
    }

    public Task<List<ChatMessage>> GetMessagesAsync(Guid threadId, Guid adminUserId, CancellationToken ct) =>
      Task.FromResult(_messages
        .Where(m => m.ThreadId == threadId && _threads.Any(t =>
          t.Id == threadId && t.UserId == adminUserId && t.Source == ChatSources.AdminAi && !t.IsDeleted))
        .OrderBy(m => m.CreatedAt)
        .ToList());

    public async Task<bool> DeleteThreadAsync(Guid threadId, Guid adminUserId, CancellationToken ct)
    {
      var thread = await GetThreadAsync(threadId, adminUserId, ct);
      if (thread is null) return false;
      thread.IsDeleted = true;
      thread.DeletedAt = DateTime.UtcNow;
      return true;
    }
  }

  private sealed class ScriptedLlmProvider(params LlmChunk[] chunks) : IAdminLlmProvider
  {
    private int _calls;
    public List<List<AdminLlmMessage>> HistorySnapshots { get; } = [];
    public List<IReadOnlyList<ToolDefinition>> ToolSnapshots { get; } = [];

    public async IAsyncEnumerable<LlmChunk> StreamChatAsync(
      List<AdminLlmMessage> history,
      IReadOnlyList<ToolDefinition> tools,
      [EnumeratorCancellation] CancellationToken ct)
    {
      HistorySnapshots.Add(history.ToList());
      ToolSnapshots.Add(tools.ToList());
      if (_calls < chunks.Length)
      {
        yield return chunks[_calls++];
      }
      else
      {
        yield return new LlmChunk("text", "done");
      }
      await Task.CompletedTask;
    }
  }

  private sealed class StaticSafetyGate : ISafetyGate
  {
    public RiskLevel Classify(string toolName) => toolName switch
    {
      "delete_product" => RiskLevel.High,
      "toggle_autonomy" => RiskLevel.High,
      "get_product" => RiskLevel.Read,
      "list_products" => RiskLevel.Read,
      "get_dashboard_summary" => RiskLevel.Read,
      "list_marketing_options" => RiskLevel.Read,
      "create_promo_code" => RiskLevel.Read,
      _ => RiskLevel.Medium
    };

    public Task<RiskLevel> ClassifyAsync(string toolName, CancellationToken ct = default) => Task.FromResult(Classify(toolName));
    public bool RequiresConfirmation(RiskLevel level) => level >= RiskLevel.Medium;
    public Task<bool> RequiresConfirmationAsync(string toolName, CancellationToken ct = default) => Task.FromResult(Classify(toolName) >= RiskLevel.Medium);
    public bool IsAutoApproved(RiskLevel level) => level <= RiskLevel.Low;
    public string GetConfirmationPrompt(string toolName, string description) => description;
    public Task InvalidateCacheAsync(CancellationToken ct = default) => Task.CompletedTask;
  }

  private sealed class FakeProductService : IAdminProductService
  {
    public int DeleteCalls { get; private set; }
    public int GetByIdCalls { get; private set; }
    public int? LastPageSize { get; private set; }
    public IReadOnlyList<AdminProductListItemResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }

    public Task<(IReadOnlyList<AdminProductListItemResponse> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int page, int pageSize, bool includeDeleted = false, CancellationToken cancellationToken = default)
    { LastPageSize = pageSize; return Task.FromResult((Items, TotalCount)); }
    public Task<AdminProductDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { GetByIdCalls++; return Task.FromResult<AdminProductDetailResponse?>(Product(id)); }
    public Task<AdminProductDetailResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default) => Task.FromResult(Product(ProductId));
    public Task<AdminProductDetailResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default) => Task.FromResult<AdminProductDetailResponse?>(Product(id));
    public Task<AdminProductDetailResponse?> UpdateVariantStockAsync(Guid productId, Guid variantId, int stockQty, CancellationToken cancellationToken = default) => Task.FromResult<AdminProductDetailResponse?>(Product(productId));
    public Task<AdminProductDetailResponse?> DeleteVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default) => Task.FromResult<AdminProductDetailResponse?>(Product(productId));
    public Task<AdminProductDetailResponse?> CreateVariantAsync(Guid productId, CreateVariantRequest request, CancellationToken cancellationToken = default) => Task.FromResult<AdminProductDetailResponse?>(Product(productId));
    public Task<AdminProductDetailResponse?> UpdateVariantAsync(Guid productId, Guid variantId, UpdateVariantRequest request, CancellationToken cancellationToken = default) => Task.FromResult<AdminProductDetailResponse?>(Product(productId));
    public Task<bool> ToggleStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) { DeleteCalls++; return Task.FromResult(true); }
    public Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<AdminImageResponse?> UploadImageAsync(Guid productId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default) => Task.FromResult<AdminImageResponse?>(null);
    public Task<bool> DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> SetPrimaryImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    private static AdminProductDetailResponse Product(Guid id) => new(id, "Áo dài", "ao-dai", "ao_dai", Guid.NewGuid(), "Danh mục", null, null, null, null, null, null, "active", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [], []);
  }

  private static AdminProductListItemResponse ProductItem(string name) => new(
    Guid.NewGuid(),
    name,
    name.ToLowerInvariant().Replace(' ', '-'),
    "ao_dai",
    "Áo dài",
    "active",
    false,
    1,
    10,
    false,
    DateTimeOffset.UtcNow);

  private sealed class FakeBlogPostService : IBlogPostService
  {
    public Task<PagedResult<BlogPostListItemDto>> GetPostsAsync(BlogPostStatus? status, string? tag, string? categorySlug, string? search, int page, int pageSize, bool includeDeleted = false, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<BlogPostListItemDto>([], 0, page, pageSize));
    public Task<BlogPostDto?> GetBySlugAsync(string slug, bool includeDrafts = false, CancellationToken cancellationToken = default) => Task.FromResult<BlogPostDto?>(null);
    public Task<BlogPostDto?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default) => Task.FromResult<BlogPostDto?>(null);
    public Task<IReadOnlyList<BlogPostListItemDto>> GetRelatedAsync(string slug, int count, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<BlogPostListItemDto>)[]);
    public Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<string>)[]);
    public Task<BlogPostDto> CreateAsync(CreateBlogPostRequest request, CancellationToken cancellationToken = default) => Task.FromResult(BlogPost(Guid.NewGuid(), request.Title, request.Slug ?? "bai-viet", request.Status));
    public Task<BlogPostDto> UpdateAsync(Guid id, UpdateBlogPostRequest request, CancellationToken cancellationToken = default) => Task.FromResult(BlogPost(id, request.Title, request.Slug ?? "bai-viet", request.Status));
    public Task<BlogPostDto> UpdateSeoAsync(Guid id, UpdateBlogPostSeoRequest request, CancellationToken cancellationToken = default) => Task.FromResult(BlogPost(id, request.MetaTitle ?? "Bài viết", request.CanonicalUrl ?? "bai-viet", BlogPostStatus.Draft));
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<string> BuildBlogSitemapAsync(string siteBaseUrl, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
    public Task<string> BuildLlmsTextAsync(string siteBaseUrl, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);

    private static BlogPostDto BlogPost(Guid id, string title, string slug, BlogPostStatus status) => new(id, title, slug, string.Empty, null, null, null, BlogPostTemplate.StandardArticle, [], [], null, null, null, null, null, null, null, null, status, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
  }

  private sealed class FakeFacebookService : IFacebookService
  {
    public Task<IReadOnlyList<FacebookConnectionDto>> GetConnectionsAsync(CancellationToken ct = default)
      => Task.FromResult<IReadOnlyList<FacebookConnectionDto>>(new List<FacebookConnectionDto>());
    public Task<FacebookPostListDto> GetPostsAsync(string pageId, string? cursor = null, int limit = 25, CancellationToken ct = default)
      => Task.FromResult(new FacebookPostListDto(new List<FacebookPostDto>(), null, null, null));
    public Task<FacebookPostCommentListDto> GetPostCommentsAsync(string pageId, string postId, string? after = null, int limit = 25, CancellationToken ct = default)
      => Task.FromResult(new FacebookPostCommentListDto(new List<FacebookCommentDto>(), null, null, null));
    public Task<FacebookCommentActionResultDto> ReplyToCommentAsync(string pageId, string commentId, ReplyFacebookCommentRequest request, CancellationToken ct = default)
      => Task.FromResult(new FacebookCommentActionResultDto(true, "0", "OK"));

    // Unused by the admin agent — not exercised in these tests.
    public Task<FacebookConnectionDto> ConnectPageAsync(ConnectFacebookPageRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookOAuthUrlDto> GetOAuthUrlAsync(string redirectUri, string state, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<FacebookOAuthPageDto>> GetOAuthPagesAsync(FacebookOAuthPagesRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookConnectionDto> ConnectOAuthPageAsync(ConnectFacebookOAuthPageRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task DisconnectPageAsync(string pageId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPageInfoDto> GetPageInfoAsync(string pageId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPublishResultDto> PublishPostAsync(string pageId, CreateFacebookPostRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPublishResultDto> PublishPhotoAsync(string pageId, System.IO.Stream imageStream, string fileName, string contentType, string? caption, DateTimeOffset? scheduledPublishTime = null, bool published = true, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPublishResultDto> PublishVideoAsync(string pageId, System.IO.Stream videoStream, string fileName, string contentType, string? description, DateTimeOffset? scheduledPublishTime = null, bool published = true, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPostDto> GetPostAsync(string postId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookPostDto> UpdatePostAsync(string postId, UpdateFacebookPostRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookDeleteResultDto> DeletePostAsync(string postId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookCommentActionResultDto> CommentOnPostAsync(string pageId, string postId, CreateFacebookCommentRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookCommentActionResultDto> ToggleCommentHiddenAsync(string pageId, string commentId, ToggleFacebookCommentHiddenRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookDeleteResultDto> DeleteCommentAsync(string pageId, string commentId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookConversationListDto> GetConversationsAsync(string pageId, string? after = null, int limit = 25, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookMessageListDto> GetConversationMessagesAsync(string pageId, string conversationId, string? before = null, int limit = 50, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<FacebookMessageSendResultDto> SendMessageAsync(string pageId, string conversationId, SendFacebookMessageRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<MarkConversationReadResultDto> MarkConversationReadAsync(string pageId, string conversationId, CancellationToken ct = default) => throw new NotImplementedException();
  }

  private sealed class FakeMarketingCampaignService : IAdminMarketingCampaignService
  {
    public int GetContentOptionsCalls { get; private set; }
    public IReadOnlyList<MarketingContentOption> Options { get; init; } = [];

    public Task<IReadOnlyList<MarketingContentOption>> GetContentOptionsAsync(CancellationToken cancellationToken = default)
    {
      GetContentOptionsCalls++;
      return Task.FromResult(Options);
    }

    public Task<MarketingCampaignSendResult> QueueCampaignAsync(SendMarketingCampaignRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new MarketingCampaignSendResult(0, 0, []));
  }

  private sealed class FakeMediaService : IAdminMediaService
  {
    public Task<UserImageListDto> GetAllAsync(int page, int pageSize, string? sourceType, string? search, CancellationToken ct = default) => Task.FromResult(new UserImageListDto([], page, pageSize, 0, 0));
    public Task<UserImageDto?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<UserImageDto?>(null);
    public Task<UserImageDto> UploadAsync(byte[] bytes, string fileName, string contentType, string sourceType = "admin", CancellationToken ct = default) => Task.FromResult(new UserImageDto(Guid.NewGuid(), "key", "url", "admin_upload", contentType, fileName, bytes.Length, sourceType, DateTimeOffset.UtcNow));
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
    public Task<MediaStatsDto> GetStatsAsync(CancellationToken ct = default) => Task.FromResult(new MediaStatsDto(0, 0, 0, 0));
  }

  private sealed class FakeSubscriberService : IAdminSubscriberService
  {
    public Task<(IReadOnlyList<SubscriberListItem> Items, int TotalItem)> GetListAsync(string? search, string? status, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<SubscriberListItem>)[], 0));
    public Task<SubscriberDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<SubscriberDetail?>(null);
    public Task<bool> UnsubscribeAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<ImportSubscribersResult> BulkImportAsync(IReadOnlyList<string> emails, string source, CancellationToken cancellationToken = default) => Task.FromResult(new ImportSubscribersResult(0, 0));
  }

  private sealed class FakeEmailJobService : IAdminEmailJobService
  {
    public Task<(IReadOnlyList<EmailJobListItem> Items, int TotalItem)> GetListAsync(string? status, bool includeDeleted, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<EmailJobListItem>)[], 0));
    public Task<EmailJobDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<EmailJobDetail?>(null);
    public Task<QueueSingleEmailJobResponse> QueueSingleAsync(QueueSingleEmailJobRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new QueueSingleEmailJobResponse(Guid.NewGuid(), request.ToEmail ?? "a@example.com", request.TemplateKey, DateTime.UtcNow, false));
    public Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
  }

  private sealed class FakeCategoryService : IAdminCategoryService
  {
    public Task<IReadOnlyList<AdminCategoryListItemResponse>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<AdminCategoryListItemResponse>)[]);
    public Task<AdminCategoryDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AdminCategoryDetailResponse?>(new(id, null, "Danh mục", "danh-muc", null, null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    public Task<AdminCategoryDetailResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new AdminCategoryDetailResponse(Guid.NewGuid(), null, request.Name, request.Slug, request.Description, null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    public Task<AdminCategoryDetailResponse?> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default) => Task.FromResult<AdminCategoryDetailResponse?>(new(id, null, request.Name, request.Slug, request.Description, null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
  }

  private sealed class FakeCollectionService : IAdminCollectionService
  {
    public Task<PagedResult<CollectionListItemDto>> GetListAsync(string? search, bool includeDeleted, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new PagedResult<CollectionListItemDto>([], 0, page, pageSize));
    public Task<CollectionDetailDto?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default) => Task.FromResult<CollectionDetailDto?>(null);
    public Task<CollectionDetailDto> CreateAsync(CreateCollectionRequest request, CancellationToken ct = default) => Task.FromResult(Collection(Guid.NewGuid(), request.Name));
    public Task<CollectionDetailDto?> UpdateAsync(Guid id, UpdateCollectionRequest request, CancellationToken ct = default) => Task.FromResult<CollectionDetailDto?>(Collection(id, request.Name));
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> RestoreAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
    public Task<CollectionDetailDto?> AddProductAsync(Guid id, AddProductToCollectionRequest request, CancellationToken ct = default) => Task.FromResult<CollectionDetailDto?>(Collection(id, "Collection"));
    public Task<CollectionDetailDto?> RemoveProductAsync(Guid id, Guid productId, CancellationToken ct = default) => Task.FromResult<CollectionDetailDto?>(Collection(id, "Collection"));
    private static CollectionDetailDto Collection(Guid id, string name) => new(id, name, "collection", null, null, false, false, 0, null, DateTime.UtcNow, DateTime.UtcNow, false, []);
  }

  private sealed class FakeUserService : IAdminUserService
  {
    public Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(string? search, int page, int pageSize, bool includeDeleted = false, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<AdminUserListItemDto>([], 0, page, pageSize));
    public Task<AdminUserListItemDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AdminUserListItemDto?>(new(id, "User", "u@example.com", null, "active", ["customer"], DateTime.UtcNow, null));
    public Task<AdminUserListItemDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<AdminUserListItemDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<AdminMutationResult> UpdateUserRoleAsync(Guid actorUserId, Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default) => Task.FromResult(AdminMutationResult.Success());
    public Task<AdminMutationResult> UpdateUserStatusAsync(Guid actorUserId, Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default) => Task.FromResult(AdminMutationResult.Success());
    public Task<AdminMutationResult> DeleteUserAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult(AdminMutationResult.Success());
    public Task<AdminMutationResult> RestoreUserAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult(AdminMutationResult.Success());
  }

  private sealed class FakeRoleService : IAdminRoleService
  {
    public Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<RoleDto>)[new RoleDto(Guid.NewGuid(), "admin", null), new RoleDto(Guid.NewGuid(), "customer", null)]);
    public Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
  }

  private sealed class FakeDashboardService : IAdminDashboardService
  {
    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default) => Task.FromResult(new DashboardSummaryDto(100, 2, 3, 4, 0, 0, 0, 0));
    public Task<RevenueDataDto> GetRevenueAsync(int periodDays, CancellationToken ct = default) => Task.FromResult(new RevenueDataDto("test", []));
    public Task<OrderStatusDistributionDto> GetOrdersByStatusAsync(CancellationToken ct = default) => Task.FromResult(new OrderStatusDistributionDto(0, 0, 0, 0, 0, 0, 0));
    public Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int limit, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<RecentOrderDto>)[]);
    public Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int limit, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TopProductDto>)[]);
    public Task<UserGrowthDataDto> GetUserGrowthAsync(int periodDays, CancellationToken ct = default) => Task.FromResult(new UserGrowthDataDto([]));
    public Task<RevenueDataDto> GetRevenueByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default) => Task.FromResult(new RevenueDataDto("daily", []));
    public Task<OrderStatusDistributionDto> GetOrdersByStatusByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default) => Task.FromResult(new OrderStatusDistributionDto(0, 0, 0, 0, 0, 0, 0));
    public Task<IReadOnlyList<TopProductDto>> GetTopProductsByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, int limit, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TopProductDto>)[]);
    public Task<DashboardRangeMetricsDto> GetRangeMetricsAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default) => Task.FromResult(new DashboardRangeMetricsDto(startDateUtc, endDateUtc, 0, 0, 0, 0, 0, 0));
  }

  private sealed class FakeOrderService : IAdminOrderService
  {
    public Task<IReadOnlyList<AdminOrderListItem>> GetOrdersAsync(string? status, int limit, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminOrderListItem>)[]);
    public Task<IReadOnlyList<AdminOrderListItem>> GetOrdersByRangeAsync(string? status, DateTime? startDateUtc, DateTime? endDateUtc, int limit, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminOrderListItem>)[]);
    public Task<AdminOrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult<AdminOrderDetail?>(null);
    public Task<AdminOrderDetail?> GetOrderByCodeAsync(string orderCode, CancellationToken ct = default) => Task.FromResult<AdminOrderDetail?>(null);
    public Task<OrderUpdateResult> UpdateStatusAsync(Guid orderId, string newStatus, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, newStatus));
    public Task<OrderUpdateResult> CreateShipmentAsync(Guid orderId, string? carrier, string? trackingNumber, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "shipping"));
    public Task<OrderUpdateResult> CancelOrderAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "cancelled"));
    public Task<OrderUpdateResult> UpdateOrderAddressAsync(Guid orderId, AdminOrderAddressUpdate request, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "pending"));
    public Task<OrderUpdateResult> UpdateOrderItemsAsync(Guid orderId, IReadOnlyList<AdminOrderItemUpdate> items, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "pending"));
    public Task<OrderUpdateResult> DeleteOrderAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "pending"));
    public Task<OrderUpdateResult> RestoreOrderAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "pending"));
  }

  private sealed class FakeInventoryService : IAdminInventoryService
  {
    public Task<InventorySummary> GetInventorySummaryAsync(int threshold = 10, CancellationToken ct = default) => Task.FromResult(new InventorySummary(0, 0, 0, 0, []));
    public Task<StoreHealthScore> GetStoreHealthScoreAsync(CancellationToken ct = default) => Task.FromResult(new StoreHealthScore(100, 100, 100, 100, 100, "OK"));
  }

  private sealed class FakeReviewService : IAdminReviewService
  {
    public Task<IReadOnlyList<AdminReviewItem>> GetRecentReviewsAsync(int limit = 10, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminReviewItem>)[]);
    public Task<AdminReviewListResult> GetReviewsAsync(AdminReviewListQuery query, CancellationToken ct = default) => Task.FromResult(new AdminReviewListResult([], 0));
    public Task<AdminReviewActionResult> SetReviewVisibilityAsync(Guid id, bool isVisible, CancellationToken ct = default) => Task.FromResult(new AdminReviewActionResult(true, "OK"));
    public Task<AdminReviewActionResult> DeleteReviewAsync(Guid id, CancellationToken ct = default) => Task.FromResult(new AdminReviewActionResult(true, "OK"));
    public Task<IReadOnlyList<AdminCommentItem>> GetRecentCommentsAsync(int limit = 10, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminCommentItem>)[]);
    public Task<BadReviewRecoveryStats> GetBadReviewRecoveryStatsAsync(int days = 30, double slaHours = 4, CancellationToken ct = default) => Task.FromResult(new BadReviewRecoveryStats(days, slaHours, 0, 0, 0, 0, 100, 0, 0));
    public Task<AdminReplyResult> ReplyToCommentAsync(Guid adminUserId, Guid commentId, Guid productId, string content, CancellationToken ct = default) => Task.FromResult(new AdminReplyResult(true, "OK", Guid.NewGuid()));
  }

  private sealed class FakePromoService : IAdminPromoService
  {
    public Task<IReadOnlyList<AdminPromoItem>> GetAllAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminPromoItem>)[]);
    public Task<AdminPromoResult> CreateAsync(CreateAdminPromoRequest request, CancellationToken ct = default) => Task.FromResult(new AdminPromoResult(true, "OK", Guid.NewGuid()));
    public Task<(IReadOnlyList<AdminPromoListItemResponse> Items, int TotalItem)> GetAllAdminAsync(bool includeDeleted = false, string? search = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
      Task.FromResult(((IReadOnlyList<AdminPromoListItemResponse>)[], 0));

    public Task<AdminPromoDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AdminPromoDetailResponse?>(null);

    public Task<AdminPromoDetailResponse> CreatePromoAsync(CreatePromoRequest request, CancellationToken cancellationToken = default) =>
      Task.FromResult(new AdminPromoDetailResponse(Guid.NewGuid(), request.Code, request.DiscountType, request.DiscountValue, request.MinOrderAmount, request.MaxUses, 0, true, false, request.FreeShipping, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

    public Task<AdminPromoDetailResponse?> UpdateAsync(Guid id, UpdatePromoRequest request, CancellationToken cancellationToken = default) =>
      Task.FromResult<AdminPromoDetailResponse?>(new AdminPromoDetailResponse(id, request.Code, request.DiscountType, request.DiscountValue, request.MinOrderAmount, request.MaxUses, 0, request.IsActive, false, request.FreeShipping, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> ToggleActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult(true);
  }

  private sealed class FakeBlogAiDraftService : IBlogAiDraftService
  {
    public Task<GeneratedBlogDraftResponse> GenerateDraftAsync(
      GenerateBlogDraftRequest request,
      CancellationToken cancellationToken = default)
    {
      return Task.FromResult(new GeneratedBlogDraftResponse(
        request.Topic,
        "fake-draft",
        "Fake draft excerpt",
        request.Template,
        JsonSerializer.SerializeToElement(Array.Empty<object>()),
        [],
        request.Topic,
        null,
        null,
        null,
        null,
        null,
        null,
        request.CategoryId,
        ["Fake draft for testing"]));
    }
  }

  /// <summary>A promo service that throws on CreatePromoAsync to simulate a business-rule failure (duplicate code).</summary>
  private sealed class ThrowingPromoService(Exception toThrow) : IAdminPromoService
  {
    public Task<IReadOnlyList<AdminPromoItem>> GetAllAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminPromoItem>)[]);
    public Task<AdminPromoResult> CreateAsync(CreateAdminPromoRequest request, CancellationToken ct = default) => throw toThrow;
    public Task<(IReadOnlyList<AdminPromoListItemResponse> Items, int TotalItem)> GetAllAdminAsync(bool includeDeleted = false, string? search = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
      Task.FromResult(((IReadOnlyList<AdminPromoListItemResponse>)[], 0));
    public Task<AdminPromoDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AdminPromoDetailResponse?>(null);
    public Task<AdminPromoDetailResponse> CreatePromoAsync(CreatePromoRequest request, CancellationToken cancellationToken = default) => throw toThrow;
    public Task<AdminPromoDetailResponse?> UpdateAsync(Guid id, UpdatePromoRequest request, CancellationToken cancellationToken = default) => throw toThrow;
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> ToggleActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult(true);
  }

  /// <summary>A dashboard service whose GetSummaryAsync throws to simulate a downstream timeout.</summary>
  private sealed class ThrowingDashboardService(Exception toThrow) : IAdminDashboardService
  {
    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default) => throw toThrow;
    public Task<RevenueDataDto> GetRevenueAsync(int periodDays, CancellationToken ct = default) => throw toThrow;
    public Task<OrderStatusDistributionDto> GetOrdersByStatusAsync(CancellationToken ct = default) => throw toThrow;
    public Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int limit, CancellationToken ct = default) => throw toThrow;
    public Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int limit, CancellationToken ct = default) => throw toThrow;
    public Task<UserGrowthDataDto> GetUserGrowthAsync(int periodDays, CancellationToken ct = default) => throw toThrow;
    public Task<RevenueDataDto> GetRevenueByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default) => throw toThrow;
    public Task<OrderStatusDistributionDto> GetOrdersByStatusByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default) => throw toThrow;
    public Task<IReadOnlyList<TopProductDto>> GetTopProductsByRangeAsync(DateTime startDateUtc, DateTime endDateUtc, int limit, CancellationToken ct = default) => throw toThrow;
    public Task<DashboardRangeMetricsDto> GetRangeMetricsAsync(DateTime startDateUtc, DateTime endDateUtc, CancellationToken ct = default) => throw toThrow;
  }

  // --- Hermes fakes (empty by default; tests that need them can specialize) ---

  private sealed class FakeHermesFeedService : IHermesFeedService
  {
    public Task<HermesFeedSnapshotResponse> GetRecentFeedAsync(int maxItems, CancellationToken cancellationToken)
      => GetRecentFeedAsync(maxItems, null, null, cancellationToken);
    public Task<HermesFeedSnapshotResponse> GetRecentFeedAsync(int maxItems, DateTimeOffset? startDateUtc, DateTimeOffset? endDateUtc, CancellationToken cancellationToken)
      => Task.FromResult(new HermesFeedSnapshotResponse(Array.Empty<HermesFeedItemResponse>(), null, DateTimeOffset.UtcNow));
    public Task<HermesFeedHeartbeatResponse?> GetLatestHeartbeatAsync(CancellationToken cancellationToken)
      => Task.FromResult<HermesFeedHeartbeatResponse?>(null);
  }

  private sealed class FakeHermesAgentService : IHermesAgentService
  {
    public Task<HermesStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
      => Task.FromResult(new HermesStatusResponse("idle", "fake", DateTimeOffset.UtcNow, null, null, 0, null, false));
    public Task RecordHeartbeatAsync(HermesHeartbeatRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
    public IAsyncEnumerable<HermesStreamChunk> StreamChatAsync(HermesChatRequest request, Guid adminUserId, CancellationToken cancellationToken)
      => AsyncEnumerable.Empty<HermesStreamChunk>();
    public Task<IReadOnlyList<HermesRunSummaryResponse>> ListRunsAsync(CancellationToken cancellationToken)
      => Task.FromResult<IReadOnlyList<HermesRunSummaryResponse>>([]);
    public Task<HermesReportResponse> RecordReportAsync(HermesReportRequest request, CancellationToken cancellationToken)
      => throw new NotImplementedException();
    public Task<PagedResult<HermesReportListItemResponse>> ListReportsAsync(HermesReportSearchRequest request, CancellationToken cancellationToken)
      => Task.FromResult(new PagedResult<HermesReportListItemResponse>([], 0, request.Page, request.PageSize));
    public Task<HermesReportResponse?> GetReportAsync(Guid id, CancellationToken cancellationToken)
      => Task.FromResult<HermesReportResponse?>(null);
  }

  private sealed class FakeHermesEventOutboxService : IHermesEventOutboxService
  {
    public Task<PagedResult<HermesEventOutboxListItemResponse>> ListEventsAsync(HermesEventOutboxSearchRequest request, CancellationToken cancellationToken)
      => Task.FromResult(new PagedResult<HermesEventOutboxListItemResponse>([], 0, request.Page, request.PageSize));
    public Task<HermesEventOutboxResponse?> GetEventAsync(Guid id, CancellationToken cancellationToken)
      => Task.FromResult<HermesEventOutboxResponse?>(null);
    public Task<bool> RetryEventAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);
    public Task<bool> CancelEventAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);
  }
}
