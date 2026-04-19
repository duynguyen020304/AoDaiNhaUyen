using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class IntentClassifier : IIntentClassifier
{
  public Task<IntentClassificationDto> ClassifyAsync(
    string message,
    IReadOnlyList<ChatAttachmentDto> attachments,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken = default)
  {
    var normalized = ChatTextUtils.Normalize(message);
    var scenario = DetectScenario(normalized) ?? memory.Scenario;
    var budget = ChatTextUtils.TryExtractBudget(message) ?? memory.BudgetCeiling;
    var colorFamily = DetectColorFamily(normalized) ?? memory.ColorFamily;
    var materialKeyword = DetectMaterial(normalized) ?? memory.MaterialKeyword;

    var intent = normalized switch
    {
      _ when string.IsNullOrWhiteSpace(normalized) => "clarification",
      _ when ContainsAny(normalized, "don hang", "van chuyen", "hoan tra", "doi tra", "refund", "order") => "out_of_scope",
      _ when ContainsAny(normalized, "so sanh", "khac nhau") => "product_comparison",
      _ when ContainsAny(normalized, "thu", "mac thu", "try on") => attachments.Count > 0 || memory.LatestPersonAttachmentId.HasValue
        ? "tryon_execute"
        : "tryon_prepare",
      _ when attachments.Count > 0 => "image_style_analysis",
      _ when ContainsAny(normalized, "goi y", "tu van", "phoi", "mac gi", "muon tim", "hop") => "outfit_recommendation",
      _ when ContainsAny(normalized, "ao dai", "phu kien", "mau", "chat lieu", "lua", "gam") => "catalog_lookup",
      _ => "clarification"
    };

    return Task.FromResult(new IntentClassificationDto(
      intent,
      scenario,
      budget,
      colorFamily,
      materialKeyword,
      [],
      intent is "tryon_prepare" or "tryon_execute" && attachments.Count == 0 && !memory.LatestPersonAttachmentId.HasValue));
  }

  private static bool ContainsAny(string source, params string[] keywords) =>
    keywords.Any(source.Contains);

  private static string? DetectScenario(string normalized)
  {
    if (ContainsAny(normalized, "giao vien", "di day", "truong"))
    {
      return "giao-vien";
    }

    if (ContainsAny(normalized, "le tet", "tet", "xuan"))
    {
      return "le-tet";
    }

    if (ContainsAny(normalized, "du tiec", "su kien", "tiec"))
    {
      return "du-tiec";
    }

    if (ContainsAny(normalized, "chup anh", "len hinh"))
    {
      return "chup-anh";
    }

    return null;
  }

  private static string? DetectColorFamily(string normalized)
  {
    if (normalized.Contains("xanh"))
    {
      return "blue";
    }

    if (normalized.Contains("hong"))
    {
      return "pink";
    }

    if (normalized.Contains("do"))
    {
      return "red";
    }

    if (normalized.Contains("trang") || normalized.Contains("kem"))
    {
      return "ivory";
    }

    return null;
  }

  private static string? DetectMaterial(string normalized)
  {
    if (normalized.Contains("lua"))
    {
      return "lụa";
    }

    if (normalized.Contains("gam"))
    {
      return "gấm";
    }

    return null;
  }
}
