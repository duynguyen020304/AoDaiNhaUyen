using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ThreadMemoryService : IThreadMemoryService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public ThreadMemoryStateDto Read(ChatThreadMemory? memory)
  {
    if (memory is null)
    {
      return new ThreadMemoryStateDto();
    }

    var facts = Deserialize<StoredFacts>(memory.FactsJsonb) ?? new StoredFacts();
    var refs = Deserialize<StoredRefs>(memory.ResolvedRefsJsonb) ?? new StoredRefs();

    return new ThreadMemoryStateDto
    {
      Scenario = facts.Scenario,
      BudgetCeiling = facts.BudgetCeiling,
      ColorFamily = facts.ColorFamily,
      MaterialKeyword = facts.MaterialKeyword,
      ShortlistedProductIds = refs.LastShortlistProductIds ?? [],
      SelectedGarmentProductId = facts.SelectedGarmentProductId,
      SelectedAccessoryProductIds = facts.SelectedAccessoryProductIds ?? [],
      LatestPersonAttachmentId = facts.LatestPersonAttachmentId,
      LatestTryOnResultAttachmentId = facts.LatestTryOnResultAttachmentId,
      LatestTryOnResultMessageId = facts.LatestTryOnResultMessageId,
      PendingTryOnRequirements = facts.PendingTryOnRequirements ?? []
    };
  }

  public void ApplyUserTurn(
    ThreadMemoryStateDto memory,
    string message,
    IReadOnlyList<ChatAttachment> attachments)
  {
    var normalized = ChatTextUtils.Normalize(message);

    if (normalized.Contains("giao vien") || normalized.Contains("di day"))
    {
      memory.Scenario = "giao-vien";
    }
    else if (normalized.Contains("tet"))
    {
      memory.Scenario = "le-tet";
    }
    else if (normalized.Contains("tiec"))
    {
      memory.Scenario = "du-tiec";
    }
    else if (normalized.Contains("chup anh"))
    {
      memory.Scenario = "chup-anh";
    }

    memory.BudgetCeiling = ChatTextUtils.TryExtractBudget(message) ?? memory.BudgetCeiling;

    if (normalized.Contains("xanh"))
    {
      memory.ColorFamily = "blue";
    }
    else if (normalized.Contains("hong"))
    {
      memory.ColorFamily = "pink";
    }
    else if (normalized.Contains("do"))
    {
      memory.ColorFamily = "red";
    }
    else if (normalized.Contains("trang") || normalized.Contains("kem"))
    {
      memory.ColorFamily = "ivory";
    }

    if (normalized.Contains("lua"))
    {
      memory.MaterialKeyword = "lụa";
    }
    else if (normalized.Contains("gam"))
    {
      memory.MaterialKeyword = "gấm";
    }

    var latestPersonImage = attachments.LastOrDefault(attachment => attachment.Kind == "user_image");
    if (latestPersonImage is not null)
    {
      memory.LatestPersonAttachmentId = latestPersonImage.Id;
    }
  }

  public void ApplyAssistantTurn(
    ThreadMemoryStateDto memory,
    IntentClassificationDto classification,
    ChatStructuredPayloadDto? structuredPayload,
    long? tryOnResultAttachmentId,
    long? tryOnResultMessageId)
  {
    if (!string.IsNullOrWhiteSpace(classification.Scenario))
    {
      memory.Scenario = classification.Scenario;
    }

    if (classification.BudgetCeiling.HasValue)
    {
      memory.BudgetCeiling = classification.BudgetCeiling.Value;
    }

    if (!string.IsNullOrWhiteSpace(classification.ColorFamily))
    {
      memory.ColorFamily = classification.ColorFamily;
    }

    if (!string.IsNullOrWhiteSpace(classification.MaterialKeyword))
    {
      memory.MaterialKeyword = classification.MaterialKeyword;
    }

    if (structuredPayload is not null)
    {
      memory.ShortlistedProductIds = structuredPayload.Products.Select(product => product.ProductId).Distinct().ToList();
      memory.SelectedGarmentProductId = structuredPayload.SelectedGarmentProductId ?? memory.SelectedGarmentProductId;
      memory.SelectedAccessoryProductIds = structuredPayload.SelectedAccessoryProductIds.Distinct().ToList();
      memory.PendingTryOnRequirements = structuredPayload.PendingTryOnRequirements.Distinct().ToList();
    }

    if (tryOnResultAttachmentId.HasValue)
    {
      memory.LatestTryOnResultAttachmentId = tryOnResultAttachmentId.Value;
    }

    if (tryOnResultMessageId.HasValue)
    {
      memory.LatestTryOnResultMessageId = tryOnResultMessageId.Value;
    }
  }

  public void Persist(
    ChatThread thread,
    ThreadMemoryStateDto memory,
    long? lastMessageId)
  {
    thread.Memory ??= new ChatThreadMemory
    {
      ThreadId = thread.Id
    };

    thread.Memory.Summary = BuildSummary(memory);
    thread.Memory.FactsJsonb = JsonSerializer.Serialize(new StoredFacts
    {
      Scenario = memory.Scenario,
      BudgetCeiling = memory.BudgetCeiling,
      ColorFamily = memory.ColorFamily,
      MaterialKeyword = memory.MaterialKeyword,
      SelectedGarmentProductId = memory.SelectedGarmentProductId,
      SelectedAccessoryProductIds = memory.SelectedAccessoryProductIds,
      LatestPersonAttachmentId = memory.LatestPersonAttachmentId,
      LatestTryOnResultAttachmentId = memory.LatestTryOnResultAttachmentId,
      LatestTryOnResultMessageId = memory.LatestTryOnResultMessageId,
      PendingTryOnRequirements = memory.PendingTryOnRequirements
    }, JsonOptions);
    thread.Memory.ResolvedRefsJsonb = JsonSerializer.Serialize(new StoredRefs
    {
      LastShortlistProductIds = memory.ShortlistedProductIds
    }, JsonOptions);
    thread.Memory.LastMessageId = lastMessageId;
    thread.Memory.UpdatedAt = DateTime.UtcNow;
  }

  private static string BuildSummary(ThreadMemoryStateDto memory)
  {
    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(memory.Scenario))
    {
      parts.Add($"dịp {memory.Scenario}");
    }

    if (memory.BudgetCeiling.HasValue)
    {
      parts.Add($"ngân sách <= {memory.BudgetCeiling.Value:N0}đ");
    }

    if (!string.IsNullOrWhiteSpace(memory.ColorFamily))
    {
      parts.Add($"màu {memory.ColorFamily}");
    }

    if (!string.IsNullOrWhiteSpace(memory.MaterialKeyword))
    {
      parts.Add($"chất liệu {memory.MaterialKeyword}");
    }

    return parts.Count == 0
      ? "Chưa có ngữ cảnh ổn định."
      : string.Join("; ", parts);
  }

  private static T? Deserialize<T>(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return default;
    }

    try
    {
      return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
    catch (JsonException)
    {
      return default;
    }
  }

  private sealed class StoredFacts
  {
    public string? Scenario { get; set; }
    public decimal? BudgetCeiling { get; set; }
    public string? ColorFamily { get; set; }
    public string? MaterialKeyword { get; set; }
    public long? SelectedGarmentProductId { get; set; }
    public List<long>? SelectedAccessoryProductIds { get; set; }
    public long? LatestPersonAttachmentId { get; set; }
    public long? LatestTryOnResultAttachmentId { get; set; }
    public long? LatestTryOnResultMessageId { get; set; }
    public List<string>? PendingTryOnRequirements { get; set; }
  }

  private sealed class StoredRefs
  {
    public List<long>? LastShortlistProductIds { get; set; }
  }
}
