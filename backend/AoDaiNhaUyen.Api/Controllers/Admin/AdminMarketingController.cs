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
  public async Task<IActionResult> Create(CreateEmailTemplateRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var item = await service.CreateAsync(request, cancellationToken);
      return CreatedAtAction(nameof(GetById), new { id = item.Id }, ApiResponseFactory.Success(item, "Tạo mẫu email thành công."));
    }
    catch (InvalidOperationException ex)
    {
      return Conflict(ApiResponseFactory.Failure("Không thể tạo mẫu email.", "conflict", ex.Message));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu mẫu email không hợp lệ.", "validation_error", ex.Message));
    }
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var item = await service.UpdateAsync(id, request, cancellationToken);
      return item is null
        ? NotFound(ApiResponseFactory.Failure("Không tìm thấy mẫu email.", "not_found", "Mẫu email không tồn tại."))
        : Ok(ApiResponseFactory.Success(item, "Cập nhật mẫu email thành công."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu mẫu email không hợp lệ.", "validation_error", ex.Message));
    }
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
  {
    return await service.DeleteAsync(id, cancellationToken)
      ? NoContent()
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy mẫu email.", "not_found", "Mẫu email không tồn tại hoặc đã bị xóa."));
  }

  [HttpPatch("{id:guid}/restore")]
  public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken = default)
  {
    return await service.RestoreAsync(id, cancellationToken)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Khôi phục mẫu email thành công."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy mẫu email.", "not_found", "Mẫu email không tồn tại hoặc chưa bị xóa."));
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
  public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var (items, totalItem) = await service.GetListAsync(status, page, pageSize, cancellationToken);
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
