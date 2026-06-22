using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminToolInstructionRegistry
{
  bool TryGetInstruction(string toolName, out ToolInstruction instruction);
}
