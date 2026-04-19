using System.Globalization;
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
      Content = "Chào bạn, mình là stylist AI của Nhã Uyên. Bạn cần áo dài cho dịp nào, màu gì, hay muốn thử đồ ngay với ảnh của mình?",
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
    threadMemoryService.ApplyUserTurn(memory, userMessage.Content, savedAttachments);

    var classification = await intentClassifier.ClassifyAsync(
      userMessage.Content,
      savedAttachments.Select(MapAttachment).ToList(),
      memory,
      cancellationToken);

    var referencedProductIds = await ResolveReferencedProductIdsAsync(userMessage.Content, memory, cancellationToken);
    classification = classification with { ReferencedProductIds = referencedProductIds };
    userMessage.Intent = classification.Intent;

    var assistantTurn = await BuildAssistantTurnAsync(classification, memory, cancellationToken);
    var composedContent = await stylistResponseComposer.ComposeAsync(
      userMessage.Content,
      assistantTurn.Content,
      classification.Intent,
      thread.Memory?.Summary,
      assistantTurn.StructuredPayload,
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
      []);

    var fallbackContent = "Mình đã tạo ảnh thử đồ mới ngay trong cuộc trò chuyện. Nếu muốn, bạn có thể đổi mẫu hoặc thêm phụ kiện rồi thử lại tiếp.";
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
      new IntentClassificationDto("tryon_execute", memory.Scenario, memory.BudgetCeiling, memory.ColorFamily, memory.MaterialKeyword, [], false),
      structuredPayload,
      generatedAttachment.Id,
      assistantMessage.Id);
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
    threadMemoryService.ApplyUserTurn(memory, userMessage.Content, savedAttachments);

    var classification = await intentClassifier.ClassifyAsync(
      userMessage.Content,
      savedAttachments.Select(MapAttachment).ToList(),
      memory,
      cancellationToken);

    var referencedProductIds = await ResolveReferencedProductIdsAsync(userMessage.Content, memory, cancellationToken);
    classification = classification with { ReferencedProductIds = referencedProductIds };
    userMessage.Intent = classification.Intent;

    var assistantTurn = await BuildAssistantTurnAsync(classification, memory, cancellationToken);

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
      memory.ShortlistedProductIds,
      cancellationToken);

    if (references.Count > 0)
    {
      return references;
    }

    if (memory.SelectedGarmentProductId.HasValue && ChatTextUtils.Normalize(message).Contains("bo vua roi"))
    {
      return [memory.SelectedGarmentProductId.Value];
    }

    return [];
  }

  private async Task<AssistantTurn> BuildAssistantTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    return classification.Intent switch
    {
      "catalog_lookup" => await BuildLookupTurnAsync(classification, memory, cancellationToken),
      "outfit_recommendation" => await BuildRecommendationTurnAsync(classification, memory, cancellationToken),
      "product_comparison" => await BuildComparisonTurnAsync(classification, cancellationToken),
      "tryon_prepare" or "tryon_execute" => await BuildTryOnTurnAsync(classification, memory, cancellationToken),
      "image_style_analysis" => await BuildImageAnalysisTurnAsync(classification, memory, cancellationToken),
      "out_of_scope" => new AssistantTurn(
        "Mình đang tập trung hỗ trợ tư vấn áo dài, phối phụ kiện và thử đồ AI. Nếu bạn muốn, hãy cho mình biết dịp mặc hoặc tải ảnh lên để mình gợi ý đúng catalog.",
        null),
      _ => new AssistantTurn(
        "Để mình tư vấn đúng hơn, bạn cho mình biết dịp mặc, ngân sách dự kiến, và màu hoặc chất liệu bạn thích nhé.",
        null)
    };
  }

  private async Task<AssistantTurn> BuildLookupTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var query = string.Join(' ', new[]
    {
      classification.ColorFamily,
      classification.MaterialKeyword,
      classification.Scenario,
      "ao dai"
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    var products = await catalogStylingService.LookupAsync(
      query,
      classification.Scenario,
      classification.BudgetCeiling,
      classification.ColorFamily,
      classification.MaterialKeyword,
      4,
      cancellationToken);

    if (products.Count == 0)
    {
      return new AssistantTurn(
        "Mình chưa tìm thấy mẫu live nào khớp ngay với mô tả này. Bạn thử nói rõ hơn màu, chất liệu, hoặc dịp mặc để mình lọc lại chính xác hơn.",
        null);
    }

    return new AssistantTurn(
      $"Mình đã lọc được {products.Count} mẫu đang có trong catalog. Bạn có thể xem từng mẫu bên dưới, hoặc nhắn “gợi ý set cho mình” để mình phối thành set hoàn chỉnh.",
      new ChatStructuredPayloadDto(
        "catalog_results",
        classification.Scenario,
        false,
        false,
        memory.SelectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        [],
        products));
  }

  private async Task<AssistantTurn> BuildRecommendationTurnAsync(
    IntentClassificationDto classification,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken)
  {
    var scenario = classification.Scenario ?? memory.Scenario;
    var products = await catalogStylingService.RecommendAsync(
      scenario,
      classification.BudgetCeiling ?? memory.BudgetCeiling,
      classification.ColorFamily ?? memory.ColorFamily,
      classification.MaterialKeyword ?? memory.MaterialKeyword,
      3,
      cancellationToken);

    if (products.Count == 0)
    {
      return new AssistantTurn(
        "Mình chưa ghép được set phù hợp từ catalog hiện tại với các ràng buộc này. Bạn thử nới ngân sách hoặc đổi màu/chất liệu để mình tìm lại.",
        null);
    }

    return new AssistantTurn(
      BuildRecommendationCopy(products, scenario),
      new ChatStructuredPayloadDto(
        "recommendations",
        scenario,
        false,
        false,
        products[0].ProductId,
        [],
        memory.LatestPersonAttachmentId.HasValue ? [] : ["upload_person_image"],
        products));
  }

  private async Task<AssistantTurn> BuildComparisonTurnAsync(
    IntentClassificationDto classification,
    CancellationToken cancellationToken)
  {
    if (classification.ReferencedProductIds.Count < 2)
    {
      return new AssistantTurn(
        "Để so sánh rõ, bạn hãy chỉ ra ít nhất hai mẫu, ví dụ “so sánh mẫu đầu tiên với mẫu thứ hai”.",
        null);
    }

    var products = await catalogStylingService.CompareAsync(classification.ReferencedProductIds.Take(2).ToList(), cancellationToken);
    if (products.Count < 2)
    {
      return new AssistantTurn("Mình chưa đủ dữ liệu để so sánh hai mẫu đó.", null);
    }

    return new AssistantTurn(
      $"Mẫu {products[0].Name} thiên về {products[0].Rationale}, còn {products[1].Name} thì {products[1].Rationale}. Nếu bạn muốn thử đồ trước, mình khuyên bắt đầu với {products[0].Name}.",
      new ChatStructuredPayloadDto(
        "comparison",
        classification.Scenario,
        false,
        false,
        null,
        [],
        [],
        products));
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
        "Mình cần biết chính xác mẫu áo dài nào để thử. Bạn có thể nhắn “thử cái đầu tiên” sau khi mình gợi ý, hoặc chọn trực tiếp trong thẻ sản phẩm.",
        null);
    }

    var products = await catalogStylingService.CompareAsync([selectedGarmentProductId], cancellationToken);
    var requiresPersonImage = !memory.LatestPersonAttachmentId.HasValue;
    var pending = requiresPersonImage ? new List<string> { "upload_person_image" } : [];

    return new AssistantTurn(
      requiresPersonImage
        ? "Mình đã giữ sẵn mẫu bạn muốn thử. Gửi cho mình một ảnh người mặc, sau đó bấm “Thử ngay” trong khung chat là được."
        : "Mình đã sẵn sàng thử đồ với mẫu này. Bạn có thể bấm “Thử ngay” ngay trong khung chat hoặc thêm phụ kiện trước khi chạy lại.",
      new ChatStructuredPayloadDto(
        "tryon_ready",
        classification.Scenario ?? memory.Scenario,
        !requiresPersonImage,
        requiresPersonImage,
        selectedGarmentProductId,
        memory.SelectedAccessoryProductIds,
        pending,
        products));
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
        "Mình đã nhận ảnh của bạn. Để gợi ý đúng hơn, bạn cho mình biết đây là ảnh để đi dạy, đi tiệc, chụp ảnh hay dịp lễ Tết nhé.",
        null);
    }

    var products = await catalogStylingService.RecommendAsync(
      scenario,
      memory.BudgetCeiling,
      memory.ColorFamily,
      memory.MaterialKeyword,
      3,
      cancellationToken);

    return new AssistantTurn(
      $"Mình sẽ ưu tiên set hợp dịp {scenario.Replace('-', ' ')} và dễ lên ảnh. Dưới đây là {products.Count} mẫu mình chốt trước từ catalog live.",
      new ChatStructuredPayloadDto(
        "image_guided_recommendations",
        scenario,
        false,
        !memory.LatestPersonAttachmentId.HasValue,
        products.FirstOrDefault()?.ProductId,
        [],
        memory.LatestPersonAttachmentId.HasValue ? [] : ["upload_person_image"],
        products));
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

  private static string BuildRecommendationCopy(IReadOnlyList<ChatRecommendationItemDto> products, string? scenario)
  {
    var intro = string.IsNullOrWhiteSpace(scenario)
      ? $"Mình chốt trước {products.Count} mẫu đang hợp nhất từ catalog live:"
      : $"Với bối cảnh {scenario.Replace('-', ' ')}, mình chốt trước {products.Count} mẫu đang hợp nhất:";
    var highlights = products.Select((product, index) => $"{index + 1}. {product.Name}: {product.Rationale}");
    return $"{intro}\n{string.Join("\n", highlights)}";
  }

  private sealed record AssistantTurn(string Content, ChatStructuredPayloadDto? StructuredPayload);
}
