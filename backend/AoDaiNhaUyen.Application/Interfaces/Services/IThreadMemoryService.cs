using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IThreadMemoryService
{
  ThreadMemoryStateDto Read(ChatThreadMemory? memory);

  void ApplyUserTurn(
    ThreadMemoryStateDto memory,
    IReadOnlyList<ChatAttachment> attachments);

  void ApplyUserConversationTurn(
    ThreadMemoryStateDto memory,
    string userMessage);

  void ApplyAssistantTurn(
    ThreadMemoryStateDto memory,
    IntentClassificationDto classification,
    ChatStructuredPayloadDto? structuredPayload,
    long? tryOnResultAttachmentId,
    long? tryOnResultMessageId,
    string? assistantMessage = null);

  void Persist(
    ChatThread thread,
    ThreadMemoryStateDto memory,
    long? lastMessageId);
}
