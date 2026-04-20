using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class StylistChatService(
  AppDbContext dbContext,
  IIntentClassifier intentClassifier,
  IThreadMemoryService threadMemoryService,
  ICatalogStylingService catalogStylingService,
  ICatalogTryOnService catalogTryOnService,
  IStylistResponseComposer stylistResponseComposer,
  IStylistFallbackTextService fallbackTextService,
  IUploadStoragePathResolver uploadStoragePathResolver) : IStylistChatService
{
  private const string AssistantRole = "assistant";
  private const string UserRole = "user";
  private const string PromptVersion = "deterministic-stylist-v1";
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<IReadOnlyList<ChatThreadSummaryDto>> ListThreadsAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);

    var threads = await BuildOwnedThreadQuery(userId, HashGuestKey(guestKey))
      .Include(thread => thread.Messages)
      .OrderByDescending(thread => thread.UpdatedAt)
      .ToListAsync(cancellationToken);

    return threads.Select(MapThreadSummary).ToList();
  }

  public async Task<ChatThreadDetailDto> CreateThreadAsync(
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);

    var thread = new ChatThread
    {
      UserId = userId,
      GuestKeyHash = userId.HasValue ? null : HashGuestKey(guestKey),
      Status = "active",
      Source = "web"
    };
    dbContext.ChatThreads.Add(thread);
    await dbContext.SaveChangesAsync(cancellationToken);

    dbContext.ChatMessages.Add(new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = fallbackTextService.Pick("thread_welcome"),
      Intent = "clarification",
      PromptVersion = PromptVersion,
      FinishReason = "stop"
    });
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    return await GetThreadAsync(thread.Id, userId, guestKey, cancellationToken);
  }

  public async Task<ChatThreadDetailDto> GetThreadAsync(
    long threadId,
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    return MapThreadDetail(thread);
  }

  public async Task<ChatThreadDetailDto> AddMessageAsync(
    long threadId,
    long? userId,
    string? guestKey,
    string message,
    string? clientMessageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    var memory = threadMemoryService.Read(thread.Memory);

    var userMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = UserRole,
      Content = message.Trim(),
      ClientMessageId = clientMessageId,
      PromptVersion = PromptVersion,
      FinishReason = "stop"
    };
    dbContext.ChatMessages.Add(userMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    var savedAttachments = await SaveIncomingAttachmentsAsync(thread.Id, userMessage.Id, attachments, cancellationToken);
    threadMemoryService.ApplyUserTurn(memory, savedAttachments);
    threadMemoryService.ApplyUserConversationTurn(memory, userMessage.Content);
    var previousAssistantMessage = memory.RecentAssistantMessages.LastOrDefault();
    var previousUserMessage = memory.RecentUserMessages.Count > 1
      ? memory.RecentUserMessages[^2]
      : null;

    var classification = await intentClassifier.ClassifyAsync(
      userMessage.Content,
      savedAttachments.Select(MapAttachment).ToList(),
      memory,
      previousUserMessage,
      previousAssistantMessage,
      cancellationToken);

    var referencedProductIds = await ResolveReferencedProductIdsAsync(userMessage.Content, memory, cancellationToken);
    classification = NormalizeClassification(classification with { ReferencedProductIds = referencedProductIds }, memory, savedAttachments.Count > 0);
    userMessage.Intent = classification.Intent;

    var assistantTurn = await BuildAssistantTurnAsync(userMessage.Content, classification, memory, cancellationToken);
    var composedContent = await stylistResponseComposer.ComposeAsync(
      userMessage.Content,
      assistantTurn.Content,
      classification.Intent,
      thread.Memory?.Summary,
      assistantTurn.StructuredPayload,
      previousUserMessage,
      previousAssistantMessage,
      cancellationToken);
    var assistantMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = composedContent,
      Intent = classification.Intent,
      PromptVersion = PromptVersion,
      FinishReason = "stop",
      StructuredPayloadJsonb = assistantTurn.StructuredPayload is null
        ? null
        : JsonSerializer.Serialize(assistantTurn.StructuredPayload, JsonOptions)
    };
    dbContext.ChatMessages.Add(assistantMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    threadMemoryService.ApplyAssistantTurn(memory, classification, assistantTurn.StructuredPayload, null, null);
    threadMemoryService.Persist(thread, memory, assistantMessage.Id);
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    return await GetThreadAsync(thread.Id, userId, guestKey, cancellationToken);
  }

  public async Task<ChatMessageDto> ExecuteTryOnAsync(
    long threadId,
    long? userId,
    string? guestKey,
    long? garmentProductId,
    IReadOnlyList<long> accessoryProductIds,
    CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    var memory = threadMemoryService.Read(thread.Memory);

    var personAttachmentId = memory.LatestPersonAttachmentId;
    if (!personAttachmentId.HasValue)
    {
      throw new InvalidOperationException("Bạn cần gửi ảnh người mặc trước khi thử đồ.");
    }

    var personAttachment = thread.Attachments.FirstOrDefault(attachment => attachment.Id == personAttachmentId.Value);
    if (personAttachment is null)
    {
      throw new InvalidOperationException("Không tìm thấy ảnh người mặc gần nhất.");
    }

    var selectedGarment = garmentProductId ?? memory.SelectedGarmentProductId;
    if (!selectedGarment.HasValue)
    {
      throw new InvalidOperationException("Bạn cần chọn một mẫu áo dài trước khi thử đồ.");
    }

    var personBytes = await ReadStoredAttachmentBytesAsync(personAttachment.FileUrl, cancellationToken);
    var result = await catalogTryOnService.CreateAsync(
      new CatalogAiTryOnRequestDto(
        null,
        personBytes,
        personAttachment.MimeType,
        selectedGarment.Value,
        null,
        accessoryProductIds.Count > 0 ? accessoryProductIds : memory.SelectedAccessoryProductIds,
        null,
        null,
        []),
      cancellationToken);

    var generatedAttachment = await SaveGeneratedTryOnAttachmentAsync(thread.Id, result, cancellationToken);
    var structuredPayload = new ChatStructuredPayloadDto(
      "tryon_result",
      memory.Scenario,
      false,
      false,
      selectedGarment,
      accessoryProductIds.Count > 0 ? accessoryProductIds.ToList() : memory.SelectedAccessoryProductIds,
      [],
      [],
      [],
      []);

    var fallbackContent = fallbackTextService.Pick("tryon_result");
    var assistantMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = await stylistResponseComposer.ComposeAsync(
        "Tạo ảnh thử đồ cho mình",
        fallbackContent,
        "tryon_execute",
        thread.Memory?.Summary,
        structuredPayload,
        memory.RecentUserMessages.LastOrDefault(),
        memory.RecentAssistantMessages.LastOrDefault(),
        cancellationToken),
      Intent = "tryon_execute",
      PromptVersion = PromptVersion,
      FinishReason = "stop",
      StructuredPayloadJsonb = JsonSerializer.Serialize(structuredPayload, JsonOptions)
    };
    dbContext.ChatMessages.Add(assistantMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    generatedAttachment.MessageId = assistantMessage.Id;
    threadMemoryService.ApplyAssistantTurn(
      memory,
      new IntentClassificationDto("tryon_execute", memory.Scenario, memory.BudgetCeiling, memory.ColorFamily, memory.MaterialKeyword, "ao_dai", [], false),
      structuredPayload,
      generatedAttachment.Id,
      assistantMessage.Id,
      assistantMessage.Content);
    threadMemoryService.Persist(thread, memory, assistantMessage.Id);
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    return MapMessage(assistantMessage, [generatedAttachment]);
  }

  public async IAsyncEnumerable<SseChatEvent> AddMessageStreamAsync(
    long threadId,
    long? userId,
    string? guestKey,
    string message,
    string? clientMessageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    await ClaimGuestThreadsAsync(userId, guestKey, cancellationToken);
    var thread = await LoadOwnedThreadAsync(threadId, userId, guestKey, cancellationToken);
    var memory = threadMemoryService.Read(thread.Memory);

    var userMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = UserRole,
      Content = message.Trim(),
      ClientMessageId = clientMessageId,
      PromptVersion = PromptVersion,
      FinishReason = "stop"
    };
    dbContext.ChatMessages.Add(userMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    var savedAttachments = await SaveIncomingAttachmentsAsync(thread.Id, userMessage.Id, attachments, cancellationToken);
    threadMemoryService.ApplyUserTurn(memory, savedAttachments);
    threadMemoryService.ApplyUserConversationTurn(memory, userMessage.Content);
    var previousAssistantMessage = memory.RecentAssistantMessages.LastOrDefault();
    var previousUserMessage = memory.RecentUserMessages.Count > 1
      ? memory.RecentUserMessages[^2]
      : null;

    var classification = await intentClassifier.ClassifyAsync(
      userMessage.Content,
      savedAttachments.Select(MapAttachment).ToList(),
      memory,
      previousUserMessage,
      previousAssistantMessage,
      cancellationToken);

    var referencedProductIds = await ResolveReferencedProductIdsAsync(userMessage.Content, memory, cancellationToken);
    classification = NormalizeClassification(classification with { ReferencedProductIds = referencedProductIds }, memory, savedAttachments.Count > 0);
    userMessage.Intent = classification.Intent;

    var assistantTurn = await BuildAssistantTurnAsync(userMessage.Content, classification, memory, cancellationToken);

    yield return new SseChatEvent.Created(
      0,
      AssistantRole,
      "",
      classification.Intent,
      DateTime.UtcNow,
      [],
      assistantTurn.StructuredPayload);

    var accumulatedText = new StringBuilder();

    await foreach (var chunk in stylistResponseComposer.ComposeStreamAsync(
      userMessage.Content,
      assistantTurn.Content,
      classification.Intent,
      thread.Memory?.Summary,
      assistantTurn.StructuredPayload,
      previousUserMessage,
      previousAssistantMessage,
      cancellationToken).WithCancellation(cancellationToken))
    {
      accumulatedText.Append(chunk);
      yield return new SseChatEvent.TextDelta(chunk);
    }

    var finalText = accumulatedText.Length > 0 ? accumulatedText.ToString() : assistantTurn.Content;
    var assistantMessage = new ChatMessage
    {
      ThreadId = thread.Id,
      Role = AssistantRole,
      Content = finalText,
      Intent = classification.Intent,
      PromptVersion = PromptVersion,
      FinishReason = "stop",
      StructuredPayloadJsonb = assistantTurn.StructuredPayload is null
        ? null
        : JsonSerializer.Serialize(assistantTurn.StructuredPayload, JsonOptions)
    };
    dbContext.ChatMessages.Add(assistantMessage);
    await dbContext.SaveChangesAsync(cancellationToken);

    threadMemoryService.ApplyAssistantTurn(memory, classification, assistantTurn.StructuredPayload, null, null);
    threadMemoryService.Persist(thread, memory, assistantMessage.Id);
    thread.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    yield return new SseChatEvent.TextDone(finalText, assistantMessage.Id, assistantMessage.CreatedAt);
    yield return new SseChatEvent.Done();
  }

  private async Task ClaimGuestThreadsAsync(long? userId, string? guestKey, CancellationToken cancellationToken)
  {
    if (!userId.HasValue || string.IsNullOrWhiteSpace(guestKey))
    {
      return;
    }

    var guestHash = HashGuestKey(guestKey);
    var guestThreads = await dbContext.ChatThreads
      .Where(thread => thread.GuestKeyHash == guestHash && thread.UserId == null)
      .ToListAsync(cancellationToken);

    if (guestThreads.Count == 0)
    {
      return;
    }

    foreach (var thread in guestThreads)
    {
      thread.UserId = userId.Value;
      thread.GuestKeyHash = null;
      thread.ClaimedAt = DateTime.UtcNow;
      thread.UpdatedAt = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private IQueryable<ChatThread> BuildOwnedThreadQuery(long? userId, string? guestHash)
  {
    return dbContext.ChatThreads.AsQueryable().Where(thread =>
      userId.HasValue ? thread.UserId == userId.Value : thread.GuestKeyHash == guestHash);
  }

  private async Task<ChatThread> LoadOwnedThreadAsync(
    long threadId,
    long? userId,
    string? guestKey,
    CancellationToken cancellationToken)
  {
    var thread = await BuildOwnedThreadQuery(userId, HashGuestKey(guestKey))
      .Include(item => item.Messages.OrderBy(message => message.CreatedAt))
      .Include(item => item.Attachments.OrderBy(attachment => attachment.CreatedAt))
      .Include(item => item.Memory)
      .FirstOrDefaultAsync(item => item.Id == threadId, cancellationToken);

    if (thread is null)
    {
      throw new InvalidOperationException("Không tìm thấy cuộc trò chuyện.");
    }

    return thread;
  }

  private async Task<IReadOnlyList<long>> ResolveReferencedProductIdsAsync(
    string message,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var references = await catalogStylingService.ResolveProductReferencesAsync(
      message,
      memory.ShortlistedProductIds.Count > 0 ? memory.ShortlistedProductIds : memory.GarmentShortlistedProductIds,
      cancellationToken);

    if (references.Count > 0)
    {
      return references;
    }

    var normalized = ChatTextUtils.Normalize(message);
    if (memory.SelectedGarmentProductId.HasValue && normalized.Contains("bo vua roi"))
    {
      return [memory.SelectedGarmentProductId.Value];
    }

    if (memory.ShortlistedProductIds.Count > 0 && (normalized.Contains("3 ao dai nay") || normalized.Contains("3 mau nay") || normalized.Contains("cac mau nay") || normalized.Contains("mau tren") || normalized.Contains("cac san pham tren") || normalized.Contains("may mau tren")))
    {
      var requestedCount = normalized.Contains("3 ") ? Math.Min(3, memory.ShortlistedProductIds.Count) : memory.ShortlistedProductIds.Count;
      return memory.ShortlistedProductIds.Take(requestedCount).ToList();
    }

    return [];
  }

  private async Task<AssistantTurn> BuildAssistantTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    return classification.Intent switch
    {
      "catalog_lookup" => await BuildLookupTurnAsync(userMessage, classification, memory, cancellationToken),
      "outfit_recommendation" => await BuildRecommendationTurnAsync(userMessage, classification, memory, cancellationToken),
      "accessory_recommendation" => await BuildAccessoryRecommendationTurnAsync(classification, memory, cancellationToken),
      "product_description" => await BuildProductDescriptionTurnAsync(classification, memory, cancellationToken),
      "product_comparison" => await BuildComparisonTurnAsync(classification, cancellationToken),
      "tryon_prepare" or "tryon_execute" => await BuildTryOnTurnAsync(classification, memory, cancellationToken),
      "image_style_analysis" => await BuildImageAnalysisTurnAsync(classification, memory, cancellationToken),
      "out_of_scope" => new AssistantTurn(
        fallbackTextService.Pick("out_of_scope"),
        null),
      _ => new AssistantTurn(
        fallbackTextService.Pick("clarification"),
        null)
    };
  }

  private static IntentClassificationDto NormalizeClassification(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    bool hasImageAttachments)
  {
    var hasPersonImage = hasImageAttachments || memory.LatestPersonAttachmentId.HasValue;
    var hasGarmentReference = classification.ReferencedProductIds.Count > 0 || memory.SelectedGarmentProductId.HasValue || memory.ShortlistedProductIds.Count > 0;

    var normalizedIntent = classification.Intent switch
    {
      "tryon_execute" when !hasGarmentReference => "clarification",
      "tryon_execute" when !hasPersonImage => "tryon_prepare",
      "tryon_prepare" when !hasGarmentReference => "clarification",
      "product_comparison" when classification.ReferencedProductIds.Count < 2 => "clarification",
      "image_style_analysis" when !hasImageAttachments => "clarification",
      "catalog_lookup" or "outfit_recommendation" or "accessory_recommendation" or "product_description" or "product_comparison" or "tryon_prepare" or "tryon_execute" or "image_style_analysis" or "clarification" or "out_of_scope" => classification.Intent,
      _ => "clarification"
    };

    return classification with
    {
      Intent = normalizedIntent,
      RequiresPersonImage = normalizedIntent is "tryon_prepare" or "tryon_execute" && !hasPersonImage,
      HasImageAttachments = hasImageAttachments
    };
  }

  private async Task<AssistantTurn> BuildLookupTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var query = string.Join(' ', new[]
    {
      classification.ColorFamily,
      classification.MaterialKeyword,
      classification.Scenario,
      classification.ProductType == "phu_kien" ? "phu kien" : "ao dai"
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    var products = await catalogStylingService.LookupAsync(
      query,
      classification.Scenario,
      classification.BudgetCeiling,
      classification.ColorFamily,
      classification.MaterialKeyword,
      4,
      cancellationToken);

    products = SelectProducts(products, memory, ShouldPreferUnseenAlternatives(userMessage, classification, memory), 4);

    if (products.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("catalog_lookup_empty"),
        null);
    }

    return new AssistantTurn(
      $"Mình đã lọc được {products.Count} mẫu đang có trong catalog. Bạn có thể xem từng mẫu bên dưới, hoặc nhắn “gợi ý set cho mình” để mình phối thành set hoàn chỉnh.",
      BuildStructuredPayload(
        "catalog_results",
        classification.Scenario,
        false,
        false,
        memory.SelectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        [],
        string.Equals(classification.ProductType, "phu_kien", StringComparison.OrdinalIgnoreCase) ? [] : products,
        string.Equals(classification.ProductType, "phu_kien", StringComparison.OrdinalIgnoreCase) ? products : []));
  }

  private async Task<AssistantTurn> BuildRecommendationTurnAsync(
    string userMessage,
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var scenario = classification.Scenario ?? memory.Scenario;
    var preferUnseenAlternatives = ShouldPreferUnseenAlternatives(userMessage, classification, memory);

    var garmentLimit = 3;
    var garmentProducts = SelectProducts(await catalogStylingService.RecommendAsync(
      scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      "ao_dai",
      preferUnseenAlternatives ? Math.Max(garmentLimit * 3, garmentLimit + memory.ShownProductIds.Count) : garmentLimit,
      null,
      cancellationToken), memory, preferUnseenAlternatives, garmentLimit);

    if (garmentProducts.Count == 0)
    {
      return new AssistantTurn(
        preferUnseenAlternatives
          ? BuildExhaustedRecommendationCopy(scenario)
          : fallbackTextService.Pick("recommendation_empty"),
        null);
    }

    var selectedGarment = garmentProducts[0].ProductId;
    var accessoryLimit = 2;
    var accessoryProducts = SelectProducts(await catalogStylingService.RecommendAsync(
      scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      "phu_kien",
      preferUnseenAlternatives ? Math.Max(accessoryLimit * 3, accessoryLimit + memory.ShownProductIds.Count) : accessoryLimit,
      null,
      cancellationToken), memory, preferUnseenAlternatives, accessoryLimit);
    var selectedAccessoryProductIds = accessoryProducts.Select(product => product.ProductId).ToList();
    var combinedProducts = garmentProducts.Concat(accessoryProducts).ToList();

    return new AssistantTurn(
      BuildRecommendationCopy(garmentProducts, accessoryProducts, scenario),
      BuildStructuredPayload(
        "recommendations",
        scenario,
        false,
        false,
        selectedGarment,
        selectedAccessoryProductIds,
        memory.LatestPersonAttachmentId.HasValue ? [] : ["upload_person_image"],
        garmentProducts,
        accessoryProducts,
        combinedProducts));
  }

  private async Task<AssistantTurn> BuildAccessoryRecommendationTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var scenario = classification.Scenario ?? memory.Scenario;
    var accessoryProducts = await catalogStylingService.RecommendAsync(
      scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      "phu_kien",
      3,
      null,
      cancellationToken);

    if (accessoryProducts.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("recommendation_empty"),
        null);
    }

    var shouldBuildCombo = ShouldBuildAccessoryCombo(memory, classification);
    var garmentProducts = shouldBuildCombo
      ? await LoadComboGarmentProductsAsync(classification, memory, cancellationToken)
      : [];
    var selectedGarmentProductId = garmentProducts.FirstOrDefault()?.ProductId ?? memory.SelectedGarmentProductId;
    var combinedProducts = garmentProducts.Concat(accessoryProducts).ToList();

    return new AssistantTurn(
      shouldBuildCombo
        ? BuildRecommendationCopy(garmentProducts, accessoryProducts, scenario)
        : "Mình chọn trước vài phụ kiện đang hợp với gu và bối cảnh bạn mô tả. Nếu bạn muốn, mình có thể phối tiếp theo mẫu áo dài bạn đang xem.",
      BuildStructuredPayload(
        "recommendations",
        scenario,
        false,
        false,
        selectedGarmentProductId,
        accessoryProducts.Select(product => product.ProductId).ToList(),
        memory.LatestPersonAttachmentId.HasValue ? [] : ["upload_person_image"],
        garmentProducts,
        accessoryProducts,
        shouldBuildCombo ? combinedProducts : accessoryProducts));
  }

  private async Task<AssistantTurn> BuildProductDescriptionTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var productIds = classification.ReferencedProductIds.Count > 0
      ? classification.ReferencedProductIds
      : memory.ShortlistedProductIds.Take(3).ToList();

    if (productIds.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("clarification"),
        null);
    }

    var products = await catalogStylingService.CompareAsync(productIds, cancellationToken);
    if (products.Count == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("clarification"),
        null);
    }

    var description = string.Join(
      "\n",
      products.Select((product, index) => $"{index + 1}. {product.Name}: {product.Rationale}"));

    return new AssistantTurn(
      $"Mình tóm tắt nhanh đặc tính của các mẫu bạn đang hỏi:\n{description}",
      BuildStructuredPayload(
        "comparison",
        classification.Scenario ?? memory.Scenario,
        false,
        false,
        memory.SelectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        [],
        products,
        []));
  }

  private async Task<AssistantTurn> BuildComparisonTurnAsync(
    IntentClassificationDto classification,
    CancellationToken cancellationToken)
  {
    if (classification.ReferencedProductIds.Count < 2)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("comparison_need_more_refs"),
        null);
    }

    var products = await catalogStylingService.CompareAsync(classification.ReferencedProductIds.Take(2).ToList(), cancellationToken);
    if (products.Count < 2)
    {
      return new AssistantTurn(fallbackTextService.Pick("comparison_insufficient_data"), null);
    }

    return new AssistantTurn(
      $"Mẫu {products[0].Name} thiên về {products[0].Rationale}, còn {products[1].Name} thì {products[1].Rationale}. Nếu bạn muốn thử đồ trước, mình khuyên bắt đầu với {products[0].Name}.",
      BuildStructuredPayload(
        "comparison",
        classification.Scenario,
        false,
        false,
        null,
        [],
        [],
        products,
        []));
  }

  private async Task<AssistantTurn> BuildTryOnTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var selectedGarmentProductId = classification.ReferencedProductIds.FirstOrDefault();
    if (selectedGarmentProductId == 0)
    {
      selectedGarmentProductId = memory.SelectedGarmentProductId ?? memory.ShortlistedProductIds.FirstOrDefault();
    }

    if (selectedGarmentProductId == 0)
    {
      return new AssistantTurn(
        fallbackTextService.Pick("tryon_need_garment"),
        null);
    }

    var products = await catalogStylingService.CompareAsync([selectedGarmentProductId], cancellationToken);
    var requiresPersonImage = !memory.LatestPersonAttachmentId.HasValue;
    var pending = requiresPersonImage ? new List<string> { "upload_person_image" } : [];

    return new AssistantTurn(
      requiresPersonImage
        ? fallbackTextService.Pick("tryon_need_person_image")
        : fallbackTextService.Pick("tryon_ready"),
      BuildStructuredPayload(
        "tryon_ready",
        classification.Scenario ?? memory.Scenario,
        !requiresPersonImage,
        requiresPersonImage,
        selectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        pending,
        products,
        []));
  }

  private async Task<AssistantTurn> BuildImageAnalysisTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var scenario = classification.Scenario ?? memory.Scenario;
    if (string.IsNullOrWhiteSpace(scenario))
    {
      return new AssistantTurn(
        fallbackTextService.Pick("image_analysis_need_scenario"),
        null);
    }

    var products = await catalogStylingService.RecommendAsync(
      scenario,
      memory.BudgetCeiling,
      memory.ColorFamily,
      memory.MaterialKeyword,
      classification.ProductType,
      3,
      null,
      cancellationToken);

    return new AssistantTurn(
      $"Mình sẽ ưu tiên set hợp dịp {scenario.Replace('-', ' ')} và dễ lên ảnh. Dưới đây là {products.Count} mẫu mình chốt trước từ catalog live.",
      BuildStructuredPayload(
        "image_guided_recommendations",
        scenario,
        false,
        !memory.LatestPersonAttachmentId.HasValue,
        products.FirstOrDefault()?.ProductId,
        [],
        memory.LatestPersonAttachmentId.HasValue ? [] : ["upload_person_image"],
        products,
        []));
  }

  private async Task<IReadOnlyList<ChatAttachment>> SaveIncomingAttachmentsAsync(
    long threadId,
    long messageId,
    IReadOnlyList<IncomingChatAttachmentDto> attachments,
    CancellationToken cancellationToken)
  {
    if (attachments.Count == 0)
    {
      return [];
    }

    var saved = new List<ChatAttachment>(attachments.Count);
    foreach (var attachment in attachments)
    {
      var extension = Path.GetExtension(attachment.OriginalFileName);
      if (string.IsNullOrWhiteSpace(extension))
      {
        extension = attachment.MimeType switch
        {
          "image/png" => ".png",
          "image/jpeg" => ".jpg",
          "image/webp" => ".webp",
          _ => ".bin"
        };
      }

      var fileName = $"{Guid.NewGuid():N}{extension}";
      var absoluteDirectory = uploadStoragePathResolver.GetChatThreadDirectory(threadId);
      Directory.CreateDirectory(absoluteDirectory);
      await File.WriteAllBytesAsync(Path.Combine(absoluteDirectory, fileName), attachment.Bytes, cancellationToken);

      var entity = new ChatAttachment
      {
        ThreadId = threadId,
        MessageId = messageId,
        Kind = attachment.Kind,
        FileUrl = $"/upload/chat/{threadId}/{fileName}",
        MimeType = attachment.MimeType,
        OriginalFileName = attachment.OriginalFileName,
        FileSizeBytes = attachment.Bytes.LongLength
      };
      dbContext.ChatAttachments.Add(entity);
      saved.Add(entity);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return saved;
  }

  private async Task<ChatAttachment> SaveGeneratedTryOnAttachmentAsync(
    long threadId,
    AiTryOnResultDto result,
    CancellationToken cancellationToken)
  {
    var (mimeType, bytes) = DecodeDataUrl(result.ResultImageUrl);
    var extension = mimeType switch
    {
      "image/jpeg" => ".jpg",
      "image/webp" => ".webp",
      _ => ".png"
    };
    var fileName = $"tryon-{Guid.NewGuid():N}{extension}";
    var absoluteDirectory = uploadStoragePathResolver.GetChatThreadDirectory(threadId);
    Directory.CreateDirectory(absoluteDirectory);
    await File.WriteAllBytesAsync(Path.Combine(absoluteDirectory, fileName), bytes, cancellationToken);

    var attachment = new ChatAttachment
    {
      ThreadId = threadId,
      Kind = "tryon_result",
      FileUrl = $"/upload/chat/{threadId}/{fileName}",
      MimeType = mimeType,
      OriginalFileName = fileName,
      FileSizeBytes = bytes.LongLength
    };
    dbContext.ChatAttachments.Add(attachment);
    await dbContext.SaveChangesAsync(cancellationToken);
    return attachment;
  }

  private async Task<byte[]> ReadStoredAttachmentBytesAsync(string fileUrl, CancellationToken cancellationToken)
  {
    if (!uploadStoragePathResolver.TryGetAbsolutePathForRequestPath(fileUrl, out var absolutePath))
    {
      throw new InvalidOperationException("Đường dẫn ảnh đã lưu trong chat không hợp lệ.");
    }

    if (!File.Exists(absolutePath))
    {
      throw new InvalidOperationException("Không tìm thấy ảnh người mặc đã lưu trên máy chủ. Vui lòng gửi lại ảnh.");
    }

    return await File.ReadAllBytesAsync(absolutePath, cancellationToken);
  }

  private static (string MimeType, byte[] Bytes) DecodeDataUrl(string dataUrl)
  {
    var separator = dataUrl.IndexOf(',', StringComparison.Ordinal);
    var header = dataUrl[..separator];
    var mimeType = header.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Replace("data:", string.Empty, StringComparison.OrdinalIgnoreCase);
    return (mimeType, Convert.FromBase64String(dataUrl[(separator + 1)..]));
  }

  private static string? HashGuestKey(string? guestKey)
  {
    if (string.IsNullOrWhiteSpace(guestKey))
    {
      return null;
    }

    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(guestKey.Trim()));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }

  private static ChatThreadSummaryDto MapThreadSummary(ChatThread thread)
  {
    var title = BuildThreadTitle(thread.Messages);
    var preview = thread.Messages.OrderByDescending(message => message.CreatedAt).FirstOrDefault()?.Content;
    return new ChatThreadSummaryDto(thread.Id, title, preview, thread.Status, thread.UpdatedAt);
  }

  private static ChatThreadDetailDto MapThreadDetail(ChatThread thread)
  {
    var groupedAttachments = thread.Attachments
      .Where(attachment => attachment.MessageId.HasValue)
      .GroupBy(attachment => attachment.MessageId!.Value)
      .ToDictionary(group => group.Key, group => group.ToList() as IReadOnlyList<ChatAttachment>);

    var messages = thread.Messages
      .OrderBy(message => message.CreatedAt)
      .Select(message => MapMessage(message, groupedAttachments.TryGetValue(message.Id, out var attachments) ? attachments : []))
      .ToList();

    return new ChatThreadDetailDto(
      thread.Id,
      BuildThreadTitle(thread.Messages),
      thread.Status,
      thread.Source,
      thread.CreatedAt,
      thread.UpdatedAt,
      messages);
  }

  private static ChatMessageDto MapMessage(ChatMessage message, IReadOnlyList<ChatAttachment> attachments)
  {
    ChatStructuredPayloadDto? payload = null;
    if (!string.IsNullOrWhiteSpace(message.StructuredPayloadJsonb))
    {
      try
      {
        payload = JsonSerializer.Deserialize<ChatStructuredPayloadDto>(message.StructuredPayloadJsonb, JsonOptions);
      }
      catch (JsonException)
      {
        payload = null;
      }
    }

    return new ChatMessageDto(
      message.Id,
      message.Role,
      message.Content,
      message.Intent,
      message.CreatedAt,
      attachments.Select(MapAttachment).ToList(),
      payload);
  }

  private static ChatAttachmentDto MapAttachment(ChatAttachment attachment) =>
    new(
      attachment.Id,
      attachment.Kind,
      attachment.FileUrl,
      attachment.MimeType,
      attachment.OriginalFileName,
      attachment.CreatedAt);

  private static string BuildThreadTitle(IEnumerable<ChatMessage> messages)
  {
    var firstUser = messages.FirstOrDefault(message => message.Role == UserRole)?.Content;
    if (string.IsNullOrWhiteSpace(firstUser))
    {
      return "Stylist mới";
    }

    return firstUser.Length <= 48 ? firstUser : string.Concat(firstUser.AsSpan(0, 48), "...");
  }

  private static string BuildRecommendationCopy(
    IReadOnlyList<ChatRecommendationItemDto> garmentProducts,
    IReadOnlyList<ChatRecommendationItemDto> accessoryProducts,
    string? scenario)
  {
    var intro = string.IsNullOrWhiteSpace(scenario)
      ? "Mình phối sẵn một set hoàn chỉnh từ catalog live:"
      : $"Với bối cảnh {scenario.Replace('-', ' ')}, mình phối sẵn một set hoàn chỉnh:";
    var garmentHighlights = garmentProducts.Select((product, index) => $"{index + 1}. {product.Name}: {product.Rationale}");
    var accessoryHighlights = accessoryProducts.Select((product, index) => $"{index + 1}. {product.Name}: {product.Rationale}");
    var sections = new List<string> { intro };

    if (garmentProducts.Count > 0)
    {
      sections.Add($"Áo dài gợi ý:\n{string.Join("\n", garmentHighlights)}");
    }

    if (accessoryProducts.Count > 0)
    {
      sections.Add($"Phụ kiện đi kèm:\n{string.Join("\n", accessoryHighlights)}");
    }

    return string.Join("\n", sections);
  }

  private static bool ShouldPreferUnseenAlternatives(string userMessage, IntentClassificationDto classification, ThreadMemoryStateDto memory)
  {
    if (memory.ShownProductIds.Count == 0)
    {
      return false;
    }

    if (classification.ReferencedProductIds.Count > 0)
    {
      return false;
    }

    var normalizedMessage = ChatTextUtils.Normalize(userMessage);
    if (RequestsDifferentOptions(normalizedMessage))
    {
      return true;
    }

    if (memory.SelectedGarmentProductId.HasValue)
    {
      return false;
    }

    return classification.Intent is "outfit_recommendation" or "catalog_lookup" or "image_style_analysis";
  }

  private static bool RequestsDifferentOptions(string normalizedMessage)
  {
    return normalizedMessage.Contains("khac")
      || normalizedMessage.Contains("khong thich")
      || normalizedMessage.Contains("ko thich")
      || normalizedMessage.Contains("k thich")
      || normalizedMessage.Contains("khong ung")
      || normalizedMessage.Contains("ko ung")
      || normalizedMessage.Contains("khong hop")
      || normalizedMessage.Contains("ko hop")
      || normalizedMessage.Contains("mau khac")
      || normalizedMessage.Contains("bo khac")
      || normalizedMessage.Contains("goi y khac")
      || normalizedMessage.Contains("them vai mau nua")
      || normalizedMessage.Contains("them mau nua");
  }

  private static IReadOnlyList<ChatRecommendationItemDto> SelectProducts(
    IReadOnlyList<ChatRecommendationItemDto> products,
    ThreadMemoryStateDto memory,
    bool preferUnseenAlternatives,
    int limit)
  {
    if (!preferUnseenAlternatives || memory.ShownProductIds.Count == 0)
    {
      return products.Take(limit).ToList();
    }

    var shownProductIds = memory.ShownProductIds.ToHashSet();
    var unseen = products.Where(product => !shownProductIds.Contains(product.ProductId));
    var seen = products.Where(product => shownProductIds.Contains(product.ProductId));
    return unseen.Concat(seen).Take(limit).ToList();
  }

  private static string BuildExhaustedRecommendationCopy(string? scenario)
  {
    var scenarioLabel = string.IsNullOrWhiteSpace(scenario) ? "nhu cầu hiện tại" : scenario.Replace('-', ' ');
    return $"Mình đã đi hết các mẫu áo dài chưa lặp lại cho bối cảnh {scenarioLabel}. Nếu bạn muốn, mình có thể chuyển sang hướng màu khác, khoảng giá khác hoặc phối lại phụ kiện trên các mẫu đã xem.";
  }

  private static ChatStructuredPayloadDto BuildStructuredPayload(
    string kind,
    string? scenario,
    bool canTryOn,
    bool requiresPersonImage,
    long? selectedGarmentProductId,
    IReadOnlyList<long> selectedAccessoryProductIds,
    IReadOnlyList<string> pendingTryOnRequirements,
    IReadOnlyList<ChatRecommendationItemDto> garmentProducts,
    IReadOnlyList<ChatRecommendationItemDto> accessoryProducts,
    IReadOnlyList<ChatRecommendationItemDto>? legacyProducts = null)
  {
    return new ChatStructuredPayloadDto(
      kind,
      scenario,
      canTryOn,
      requiresPersonImage,
      selectedGarmentProductId,
      selectedAccessoryProductIds,
      pendingTryOnRequirements,
      legacyProducts ?? garmentProducts.Concat(accessoryProducts).ToList(),
      garmentProducts,
      accessoryProducts);
  }

  private static bool ShouldBuildAccessoryCombo(ThreadMemoryStateDto memory, IntentClassificationDto classification)
  {
    var hasGarmentContext = memory.SelectedGarmentProductId.HasValue || memory.ShortlistedProductIds.Count > 0 || memory.GarmentShortlistedProductIds.Count > 0;
    return hasGarmentContext;
  }

  private async Task<IReadOnlyList<ChatRecommendationItemDto>> LoadComboGarmentProductsAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var garmentIds = memory.GarmentShortlistedProductIds.Count > 0
      ? memory.GarmentShortlistedProductIds
      : memory.ShortlistedProductIds;

    if (memory.SelectedGarmentProductId.HasValue)
    {
      garmentIds = [memory.SelectedGarmentProductId.Value, .. garmentIds.Where(id => id != memory.SelectedGarmentProductId.Value)];
    }

    if (garmentIds.Count > 0)
    {
      return await catalogStylingService.CompareAsync(garmentIds.Take(3).ToList(), cancellationToken);
    }

    return await catalogStylingService.RecommendAsync(
      classification.Scenario ?? memory.Scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      "ao_dai",
      3,
      null,
      cancellationToken);
  }

  private sealed record AssistantTurn(string Content, ChatStructuredPayloadDto? StructuredPayload);
}
