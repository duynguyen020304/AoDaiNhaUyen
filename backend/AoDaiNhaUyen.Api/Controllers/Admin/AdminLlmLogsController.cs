using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/llm-logs")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminLlmLogsController(ILlmAuditService llmAuditService) : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<PaginatedApiResponse<IReadOnlyList<LlmAuditLogListItemDto>>>> Search(
    [FromQuery] LlmAuditLogSearchRequest request,
    CancellationToken cancellationToken)
  {
    var result = await llmAuditService.SearchAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(
      result.Items,
      result.Page,
      result.PageSize,
      result.TotalCount,
      "Lấy nhật ký LLM thành công."));
  }

  [HttpGet("stats")]
  public async Task<ActionResult<ApiResponse<LlmAuditLogStatsDto>>> Stats(
    [FromQuery] LlmAuditLogSearchRequest request,
    CancellationToken cancellationToken)
  {
    var stats = await llmAuditService.GetStatsAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success(stats, "Lấy thống kê nhật ký LLM thành công."));
  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ApiResponse<LlmAuditLogDetailDto>>> Detail(
    Guid id,
    CancellationToken cancellationToken)
  {
    var detail = await llmAuditService.GetDetailAsync(id, cancellationToken);
    return detail is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy nhật ký LLM.", "not_found", "Nhật ký không tồn tại."))
      : Ok(ApiResponseFactory.Success(detail, "Lấy chi tiết nhật ký LLM thành công."));
  }
}
