using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminToolPreparationService
{
  Task<ToolPreparationResult> PrepareAsync(
    string toolName,
    string draftArgsJson,
    IReadOnlyList<ToolDefinition> tools,
    IReadOnlyList<AdminLlmMessage> history,
    Guid adminUserId,
    CancellationToken ct = default);
}
