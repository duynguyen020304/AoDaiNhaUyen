using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Facebook;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/admin/facebook")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class FacebookController(IFacebookService facebookService) : ControllerBase
{
  [HttpGet("connections")]
  public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
  {
    var connections = await facebookService.GetConnectionsAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(connections));
  }

  [HttpPost("connections")]
  public async Task<IActionResult> ConnectPage([FromBody] ConnectFacebookPageRequest request, CancellationToken cancellationToken)
  {
    var result = await facebookService.ConnectPageAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Kết nối Facebook Page thành công."));
  }

  [HttpDelete("connections/{pageId}")]
  public async Task<IActionResult> DisconnectPage(string pageId, CancellationToken cancellationToken)
  {
    await facebookService.DisconnectPageAsync(pageId, cancellationToken);
    return NoContent();
  }

  [HttpGet("{pageId}/info")]
  public async Task<IActionResult> GetPageInfo(string pageId, CancellationToken cancellationToken)
  {
    var page = await facebookService.GetPageInfoAsync(pageId, cancellationToken);
    return Ok(ApiResponseFactory.Success(page));
  }

  [HttpGet("{pageId}/posts")]
  public async Task<IActionResult> GetPosts(
    string pageId,
    [FromQuery] string? cursor,
    [FromQuery] int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var posts = await facebookService.GetPostsAsync(pageId, cursor, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(posts));
  }

  [HttpPost("{pageId}/posts")]
  public async Task<IActionResult> PublishPost(
    string pageId,
    [FromBody] CreateFacebookPostRequest request,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.PublishPostAsync(pageId, request, cancellationToken);
    return Created($"/api/admin/facebook/posts/{result.Id}", ApiResponseFactory.Success(result, "Đăng bài Facebook thành công."));
  }

  [HttpPost("{pageId}/photos")]
  [Consumes("multipart/form-data")]
  public async Task<IActionResult> PublishPhoto(
    string pageId,
    [FromForm] IFormFile file,
    [FromForm] string? caption,
    [FromForm] DateTimeOffset? scheduledPublishTime,
    [FromForm] bool published = true,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateUpload(file, ["image/jpeg", "image/png", "image/gif", "image/webp"], 10 * 1024 * 1024);
    if (validation is not null) return validation;

    await using var stream = file.OpenReadStream();
    var result = await facebookService.PublishPhotoAsync(
      pageId,
      stream,
      file.FileName,
      file.ContentType,
      caption,
      scheduledPublishTime,
      published,
      cancellationToken);

    return Created($"/api/admin/facebook/posts/{result.PostId ?? result.Id}", ApiResponseFactory.Success(result, "Đăng ảnh Facebook thành công."));
  }

  [HttpPost("{pageId}/videos")]
  [Consumes("multipart/form-data")]
  public async Task<IActionResult> PublishVideo(
    string pageId,
    [FromForm] IFormFile file,
    [FromForm] string? description,
    [FromForm] DateTimeOffset? scheduledPublishTime,
    [FromForm] bool published = true,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateUpload(file, ["video/mp4", "video/quicktime", "video/webm"], 200 * 1024 * 1024);
    if (validation is not null) return validation;

    await using var stream = file.OpenReadStream();
    var result = await facebookService.PublishVideoAsync(
      pageId,
      stream,
      file.FileName,
      file.ContentType,
      description,
      scheduledPublishTime,
      published,
      cancellationToken);

    return Created($"/api/admin/facebook/posts/{result.PostId ?? result.Id}", ApiResponseFactory.Success(result, "Đăng video Facebook thành công."));
  }

  [HttpGet("posts/{postId}")]
  public async Task<IActionResult> GetPost(string postId, CancellationToken cancellationToken)
  {
    var post = await facebookService.GetPostAsync(postId, cancellationToken);
    return Ok(ApiResponseFactory.Success(post));
  }

  [HttpPut("posts/{postId}")]
  public async Task<IActionResult> UpdatePost(
    string postId,
    [FromBody] UpdateFacebookPostRequest request,
    CancellationToken cancellationToken)
  {
    var post = await facebookService.UpdatePostAsync(postId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(post, "Cập nhật bài viết Facebook thành công."));
  }

  [HttpDelete("posts/{postId}")]
  public async Task<IActionResult> DeletePost(string postId, CancellationToken cancellationToken)
  {
    await facebookService.DeletePostAsync(postId, cancellationToken);
    return NoContent();
  }

  private static IActionResult? ValidateUpload(IFormFile? file, IReadOnlyCollection<string> allowedContentTypes, long maxBytes)
  {
    if (file is null || file.Length == 0)
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("File không hợp lệ", "bad_request", "Vui lòng chọn file hợp lệ."));
    }

    if (file.Length > maxBytes)
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("File quá lớn", "file_too_large", "Vui lòng chọn file nhỏ hơn giới hạn cho phép."));
    }

    if (!allowedContentTypes.Contains(file.ContentType))
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("Định dạng file không hỗ trợ", "unsupported_media_type", "Vui lòng chọn đúng định dạng file."));
    }

    return null;
  }
}
