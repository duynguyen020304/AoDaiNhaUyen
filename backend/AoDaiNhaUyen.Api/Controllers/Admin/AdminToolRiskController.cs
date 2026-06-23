using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Admin tool risk configuration endpoints.</summary>
[ApiController]
[Route("api/admin/tools-risk")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminToolRiskController(IAdminToolRiskService service) : ControllerBase
{
  /// <summary>Get all tool risk configurations.</summary>
  [HttpGet]
  public async Task<ActionResult<ApiResponse<IReadOnlyList<ToolRiskConfigDto>>>> GetAll(
    CancellationToken cancellationToken)
  {
    await service.SeedDefaultsAsync(cancellationToken);
    var configs = await service.GetAllAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(configs, "Lấy cấu hình rủi ro thành công."));
  }

  /// <summary>Update risk level for a tool.</summary>
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<ApiResponse<object>>> Update(
    Guid id,
    UpdateToolRiskRequest request,
    CancellationToken cancellationToken)
  {
    var ok = await service.UpdateAsync(id, request, cancellationToken);
    return ok
      ? Ok(ApiResponseFactory.Success<object?>(null, "Đã cập nhật cấu hình rủi ro."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy cấu hình.", "not_found", "Tool risk config không tồn tại."));
  }
}
