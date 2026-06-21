using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/email-templates")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminEmailTemplatesController(IAdminEmailTemplateService service) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool includeDeleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var (items, totalItem) = await service.GetListAsync(search, includeDeleted, page, pageSize, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(items, page, pageSize, totalItem));
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
  {
    var item = await service.GetByIdAsync(id, cancellationToken);
    return item is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy mẫu email.", "not_found", "Mẫu email không tồn tại."))
      : Ok(ApiResponseFactory.Success(item));
  }

  [HttpPost]
  public IActionResult Create()
  {
    return StatusCode(StatusCodes.Status405MethodNotAllowed, ApiResponseFactory.Failure("Không thể tạo mẫu email.", "templates_code_managed", "Mẫu email được dev lập trình sẵn bằng React Email."));
  }

  [HttpPut("{id:guid}")]
  public IActionResult Update(Guid id)
  {
    return StatusCode(StatusCodes.Status405MethodNotAllowed, ApiResponseFactory.Failure("Không thể cập nhật mẫu email.", "templates_code_managed", "Mẫu email được dev lập trình sẵn bằng React Email."));
  }

  [HttpDelete("{id:guid}")]
  public IActionResult Delete(Guid id)
  {
    return StatusCode(StatusCodes.Status405MethodNotAllowed, ApiResponseFactory.Failure("Không thể xóa mẫu email.", "templates_code_managed", "Mẫu email được dev lập trình sẵn bằng React Email."));
  }

  [HttpPatch("{id:guid}/restore")]
  public IActionResult Restore(Guid id)
  {
    return StatusCode(StatusCodes.Status405MethodNotAllowed, ApiResponseFactory.Failure("Không thể khôi phục mẫu email.", "templates_code_managed", "Mẫu email được dev lập trình sẵn bằng React Email."));
  }
}

[ApiController]
[Route("api/admin/subscribers")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminSubscribersController(IAdminSubscriberService service) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] bool includeDeleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var (items, totalItem) = await service.GetListAsync(search, status, includeDeleted, page, pageSize, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(items, page, pageSize, totalItem));
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
  {
    var item = await service.GetByIdAsync(id, cancellationToken);
    return item is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy người đăng ký.", "not_found", "Người đăng ký không tồn tại."))
      : Ok(ApiResponseFactory.Success(item));
  }

  [HttpPatch("{id:guid}/unsubscribe")]
  public async Task<IActionResult> Unsubscribe(Guid id, CancellationToken cancellationToken = default)
  {
    return await service.UnsubscribeAsync(id, cancellationToken)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Hủy đăng ký email thành công."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy người đăng ký.", "not_found", "Người đăng ký không tồn tại."));
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
  {
    return await service.DeleteAsync(id, cancellationToken)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Xóa người đăng ký thành công."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy người đăng ký.", "not_found", "Người đăng ký không tồn tại hoặc đã bị xóa."));
  }

  [HttpPost("import")]
  public async Task<IActionResult> Import(ImportSubscribersRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await service.BulkImportAsync(request.Emails, request.Source, cancellationToken);
      return Ok(ApiResponseFactory.Success(result, "Nhập danh sách email thành công."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu nhập email không hợp lệ.", "validation_error", ex.Message));
    }
  }
}

[ApiController]
[Route("api/admin/email-jobs")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminEmailJobsController(IAdminEmailJobService service) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] bool includeDeleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var (items, totalItem) = await service.GetListAsync(status, includeDeleted, page, pageSize, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(items, page, pageSize, totalItem));
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
  {
    var item = await service.GetByIdAsync(id, cancellationToken);
    return item is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy email job.", "not_found", "Email job không tồn tại."))
      : Ok(ApiResponseFactory.Success(item));
  }

  [HttpPost]
  public async Task<IActionResult> QueueSingle(QueueSingleEmailJobRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await service.QueueSingleAsync(request, cancellationToken);
      return Ok(ApiResponseFactory.Success(result, "Đã đưa email vào hàng đợi gửi."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu email không hợp lệ.", "validation_error", ex.Message));
    }
    catch (InvalidOperationException ex)
    {
      return Conflict(ApiResponseFactory.Failure("Không thể tạo email job.", "conflict", ex.Message));
    }
  }

  [HttpPatch("{id:guid}/retry")]
  public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken = default)
  {
    return await service.RetryAsync(id, cancellationToken)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Đưa email job về hàng đợi thành công."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy email job.", "not_found", "Email job không tồn tại."));
  }

  [HttpPatch("{id:guid}/cancel")]
  public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
  {
    return await service.CancelAsync(id, cancellationToken)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Hủy email job thành công."))
      : NotFound(ApiResponseFactory.Failure("Không thể hủy email job.", "not_found", "Email job không tồn tại hoặc không thể hủy."));
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
  {
    return await service.DeleteAsync(id, cancellationToken)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Xóa email job thành công."))
      : NotFound(ApiResponseFactory.Failure("Không thể xóa email job.", "not_found", "Email job không tồn tại, đã bị xóa hoặc đang được gửi."));
  }
}

[ApiController]
[Route("api/admin/marketing")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminMarketingController(IAdminMarketingStatsService statsService, IAdminMarketingCampaignService campaignService) : ControllerBase
{
  [HttpGet("stats")]
  public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
  {
    return Ok(ApiResponseFactory.Success(await statsService.GetStatsAsync(cancellationToken), "Lấy thống kê marketing thành công."));
  }

  [HttpGet("content-options")]
  public async Task<IActionResult> GetContentOptions(CancellationToken cancellationToken = default)
  {
    return Ok(ApiResponseFactory.Success(await campaignService.GetContentOptionsAsync(cancellationToken), "Lấy nội dung đính kèm thành công."));
  }

  [HttpPost("campaigns/send")]
  public async Task<IActionResult> SendCampaign(SendMarketingCampaignRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await campaignService.QueueCampaignAsync(request, cancellationToken);
      return Ok(ApiResponseFactory.Success(result, "Đã đưa email marketing vào hàng đợi gửi."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu gửi email không hợp lệ.", "validation_error", ex.Message));
    }
    catch (InvalidOperationException ex)
    {
      return Conflict(ApiResponseFactory.Failure("Không thể gửi email marketing.", "conflict", ex.Message));
    }
  }
}
