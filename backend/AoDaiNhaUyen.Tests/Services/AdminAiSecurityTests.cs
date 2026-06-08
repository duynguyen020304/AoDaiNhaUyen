using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.DTOs.Dashboard;
using AoDaiNhaUyen.Application.DTOs.Order;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Infrastructure.Services;
using AoDaiNhaUyen.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class AdminAiSecurityTests
{
  private static readonly Guid AdminA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  private static readonly Guid AdminB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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
    Assert.Contains(llm.HistorySnapshots.SelectMany(h => h), m =>
      m.Role == AdminLlmRole.ToolResponse &&
      m.ToolName == "get_product" &&
      m.Content.Contains("Tham số công cụ không hợp lệ", StringComparison.Ordinal));
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
    IAutoModeStore? autoMode = null)
  {
    return new AdminAgentService(
      llm,
      new StaticSafetyGate(),
      products ?? new FakeProductService(),
      new FakeCategoryService(),
      new FakeUserService(),
      new FakeRoleService(),
      new FakeDashboardService(),
      new FakeOrderService(),
      new FakeInventoryService(),
      new FakeReviewService(),
      new FakePromoService(),
      autoMode ?? new AutoModeStore(),
      NullLogger<AdminAgentService>.Instance,
      new PendingActionStore(),
      new ConversationStore(),
      new FakeAdminChatPersistence());
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

    public async IAsyncEnumerable<LlmChunk> StreamChatAsync(
      List<AdminLlmMessage> history,
      IReadOnlyList<ToolDefinition> tools,
      [EnumeratorCancellation] CancellationToken ct)
    {
      HistorySnapshots.Add(history.ToList());
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

  private sealed class FakeCategoryService : IAdminCategoryService
  {
    public Task<IReadOnlyList<AdminCategoryListItemResponse>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<AdminCategoryListItemResponse>)[]);
    public Task<AdminCategoryDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AdminCategoryDetailResponse?>(new(id, null, "Danh mục", "danh-muc", null, null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    public Task<AdminCategoryDetailResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new AdminCategoryDetailResponse(Guid.NewGuid(), null, request.Name, request.Slug, request.Description, null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    public Task<AdminCategoryDetailResponse?> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default) => Task.FromResult<AdminCategoryDetailResponse?>(new(id, null, request.Name, request.Slug, request.Description, null, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
  }

  private sealed class FakeUserService : IAdminUserService
  {
    public Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(string? search, int page, int pageSize, bool includeDeleted = false, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<AdminUserListItemDto>([], 0, page, pageSize));
    public Task<AdminUserListItemDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AdminUserListItemDto?>(new(id, "User", "u@example.com", null, "active", ["customer"], DateTime.UtcNow, null));
    public Task<AdminUserListItemDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<AdminUserListItemDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> UpdateUserStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RestoreUserAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
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
  }

  private sealed class FakeOrderService : IAdminOrderService
  {
    public Task<IReadOnlyList<AdminOrderListItem>> GetOrdersAsync(string? status, int limit, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminOrderListItem>)[]);
    public Task<AdminOrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult<AdminOrderDetail?>(null);
    public Task<OrderUpdateResult> UpdateStatusAsync(Guid orderId, string newStatus, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, newStatus));
    public Task<OrderUpdateResult> CreateShipmentAsync(Guid orderId, string? carrier, string? trackingNumber, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "shipping"));
    public Task<OrderUpdateResult> CancelOrderAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult(new OrderUpdateResult(true, null, null, orderId, "cancelled"));
  }

  private sealed class FakeInventoryService : IAdminInventoryService
  {
    public Task<InventorySummary> GetInventorySummaryAsync(int threshold = 10, CancellationToken ct = default) => Task.FromResult(new InventorySummary(0, 0, 0, 0, []));
    public Task<StoreHealthScore> GetStoreHealthScoreAsync(CancellationToken ct = default) => Task.FromResult(new StoreHealthScore(100, 100, 100, 100, 100, "OK"));
  }

  private sealed class FakeReviewService : IAdminReviewService
  {
    public Task<IReadOnlyList<AdminReviewItem>> GetRecentReviewsAsync(int limit = 10, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminReviewItem>)[]);
    public Task<IReadOnlyList<AdminCommentItem>> GetRecentCommentsAsync(int limit = 10, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminCommentItem>)[]);
    public Task<AdminReplyResult> ReplyToCommentAsync(Guid adminUserId, Guid commentId, Guid productId, string content, CancellationToken ct = default) => Task.FromResult(new AdminReplyResult(true, "OK", Guid.NewGuid()));
  }

  private sealed class FakePromoService : IAdminPromoService
  {
    public Task<IReadOnlyList<AdminPromoItem>> GetAllAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AdminPromoItem>)[]);
    public Task<AdminPromoResult> CreateAsync(CreateAdminPromoRequest request, CancellationToken ct = default) => Task.FromResult(new AdminPromoResult(true, "OK", Guid.NewGuid()));
  }
}
