using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminAuditLogsController(IAdminAuditLogService auditLogService) : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<PaginatedApiResponse<IReadOnlyList<AdminAuditLogListItemDto>>>> Search(
    [FromQuery] AdminAuditLogSearchRequest request,
    CancellationToken cancellationToken)
  {
    var result = await auditLogService.SearchAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(
      result.Items,
      result.Page,
      result.PageSize,
      result.TotalCount,
      "Lấy nhật ký thao tác quản trị thành công."));
  }

  [HttpGet("stats")]
  public async Task<ActionResult<ApiResponse<AdminAuditLogStatsDto>>> Stats(
    [FromQuery] AdminAuditLogSearchRequest request,
    CancellationToken cancellationToken)
  {
    var stats = await auditLogService.GetStatsAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success(stats, "Lấy thống kê nhật ký thao tác quản trị thành công."));
  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ApiResponse<AdminAuditLogDetailDto>>> Detail(Guid id, CancellationToken cancellationToken)
  {
    var detail = await auditLogService.GetDetailAsync(id, cancellationToken);
    return detail is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy nhật ký thao tác.", "not_found", "Nhật ký không tồn tại."))
      : Ok(ApiResponseFactory.Success(detail, "Lấy chi tiết nhật ký thao tác thành công."));
  }
}
