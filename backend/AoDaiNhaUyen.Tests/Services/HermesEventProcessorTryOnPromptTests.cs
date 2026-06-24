using System.Reflection;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Services;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

/// <summary>
/// Verifies that the try-on SOP is wired into the prompts the backend ships to
/// the external Hermes agent. Because <see cref="HermesEventProcessor.BuildInput"/>
/// and <see cref="HermesEventProcessor.BuildBatchInput"/> are private static pure
/// functions, this test invokes them via reflection on a synthetic
/// <c>social_message_received</c> outbox event and asserts the generated prompt
/// text instructs the agent through every step of the Facebook Messenger
/// virtual try-on flow (criteria #3–#6, #8). This is a verifiable contract that
/// the SOP endpoints appear in the agent's instructions, independent of the
/// live Hermes VPS runner.
/// </summary>
public sealed class HermesEventProcessorTryOnPromptTests
{
  private const string TryOnCatalogEndpoint = "GET /api/admin/ai-tryon/catalog";
  private const string MessageImageEndpoint = "/api/admin/social/messages/";
  private const string GenerateEndpoint = "POST /api/admin/ai-tryon/generate";
  private const string ReplyConversationEndpoint = "/api/admin/social/conversations/";
  private const string AttachmentTypeImage = "attachmentType=image";

  private static readonly HermesEventOutbox SocialMessageEvent = new()
  {
    EventType = "social_message_received",
    AggregateType = "SocialInbox",
    AggregateId = "msg-tryon-1",
    PayloadJson = """
    {
      "eventName": "message.received",
      "platform": "facebook",
      "conversationId": "conv-1",
      "containsUserGeneratedText": true
    }
    """,
    OccurredAt = DateTimeOffset.UtcNow
  };

  [Fact]
  public void BuildInput_ForSocialMessageReceived_IncludesFullTryOnSop()
  {
    var prompt = InvokeBuildInput(SocialMessageEvent);

    // #3 detection: agent must recognize image-bearing social messages as try-on intent.
    Assert.Contains("social_message_received", prompt);
    Assert.Contains("thử đồ", prompt, StringComparison.OrdinalIgnoreCase);

    // #4 ask-to-choose: catalog endpoint + instruction to offer 2-4 garments.
    Assert.Contains(TryOnCatalogEndpoint, prompt, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("HỎI CHỌN MẪU", prompt);

    // #5 retrieve stored image URL via the dedicated presigned-URL endpoint.
    Assert.Contains(MessageImageEndpoint, prompt, StringComparison.OrdinalIgnoreCase);

    // #6 call the try-on generation endpoint.
    Assert.Contains(GenerateEndpoint, prompt, StringComparison.OrdinalIgnoreCase);

    // #8 reply with the result image via the audited social conversation endpoint.
    Assert.Contains(ReplyConversationEndpoint, prompt, StringComparison.OrdinalIgnoreCase);
    Assert.Contains(AttachmentTypeImage, prompt, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void BuildBatchInput_ForSocialMessageReceived_IncludesFullTryOnSop()
  {
    var prompt = InvokeBuildBatchInput([SocialMessageEvent]);

    Assert.Contains("social_message_received", prompt);
    Assert.Contains(TryOnCatalogEndpoint, prompt, StringComparison.OrdinalIgnoreCase);
    Assert.Contains(MessageImageEndpoint, prompt, StringComparison.OrdinalIgnoreCase);
    Assert.Contains(GenerateEndpoint, prompt, StringComparison.OrdinalIgnoreCase);
    Assert.Contains(ReplyConversationEndpoint, prompt, StringComparison.OrdinalIgnoreCase);
    Assert.Contains(AttachmentTypeImage, prompt, StringComparison.OrdinalIgnoreCase);
  }

  private static string InvokeBuildInput(HermesEventOutbox item)
  {
    var method = typeof(HermesEventProcessor).GetMethod(
      "BuildInput",
      BindingFlags.NonPublic | BindingFlags.Static,
      null,
      [typeof(HermesEventOutbox)],
      null)
      ?? throw new InvalidOperationException("BuildInput not found.");
    return (string)method.Invoke(null, [item])!;
  }

  private static string InvokeBuildBatchInput(IReadOnlyList<HermesEventOutbox> items)
  {
    var method = typeof(HermesEventProcessor).GetMethod(
      "BuildBatchInput",
      BindingFlags.NonPublic | BindingFlags.Static,
      null,
      [typeof(IReadOnlyList<HermesEventOutbox>)],
      null)
      ?? throw new InvalidOperationException("BuildBatchInput not found.");
    return (string)method.Invoke(null, [items])!;
  }
}
