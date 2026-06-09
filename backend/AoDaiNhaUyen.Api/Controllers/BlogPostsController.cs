using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
public sealed class BlogPostsController(
  IBlogPostService blogPostService,
  IImageUploadValidator imageUploadValidator,
  IStorageService storageService) : ControllerBase
{
  [HttpGet("api/v1/blog")]
  public async Task<IActionResult> GetPublic(
    [FromQuery] string? tag,
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 12,
    CancellationToken cancellationToken = default)
  {
    var result = await blogPostService.GetPostsAsync(BlogPostStatus.Published, tag, search, page, pageSize, false, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(result.Items, result.Page, result.PageSize, result.TotalCount));
  }

  [HttpGet("api/v1/blog/tags")]
  public async Task<IActionResult> GetTags(CancellationToken cancellationToken)
  {
    var tags = await blogPostService.GetTagsAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(tags));
  }

  [HttpGet("api/v1/blog/{slug}")]
  public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
  {
    var post = await blogPostService.GetBySlugAsync(slug, false, cancellationToken);
    if (post is null)
    {
      return NotFound(ApiResponseFactory.Failure("Không tìm thấy bài viết", "not_found", "Blog post not found."));
    }

    return Ok(ApiResponseFactory.Success(post));
  }

  [HttpGet("api/v1/blog/{slug}/related")]
  public async Task<IActionResult> GetRelated(string slug, [FromQuery] int count = 3, CancellationToken cancellationToken = default)
  {
    var posts = await blogPostService.GetRelatedAsync(slug, count, cancellationToken);
    return Ok(ApiResponseFactory.Success(posts));
  }

  [HttpGet("api/v1/admin/blog")]
  [Authorize(Policy = "RequireAdminRole")]
  public async Task<IActionResult> GetAdmin(
    [FromQuery] BlogPostStatus? status,
    [FromQuery] string? tag,
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
  {
    var result = await blogPostService.GetPostsAsync(status, tag, search, page, pageSize, true, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(result.Items, result.Page, result.PageSize, result.TotalCount));
  }

  [HttpGet("api/v1/admin/blog/{id:guid}")]
  [Authorize(Policy = "RequireAdminRole")]
  public async Task<IActionResult> GetAdminById(Guid id, CancellationToken cancellationToken)
  {
    var post = await blogPostService.GetByIdAsync(id, true, cancellationToken);
    if (post is null)
    {
      return NotFound(ApiResponseFactory.Failure("Không tìm thấy bài viết", "not_found", "Blog post not found."));
    }

    return Ok(ApiResponseFactory.Success(post));
  }

  [HttpPost("api/v1/admin/blog")]
  [Authorize(Policy = "RequireAdminRole")]
  public async Task<IActionResult> Create([FromBody] CreateBlogPostRequest request, CancellationToken cancellationToken)
  {
    var post = await blogPostService.CreateAsync(request, cancellationToken);
    return Created($"/api/v1/admin/blog/{post.Id}", ApiResponseFactory.Success(post, "Tạo bài viết thành công"));
  }

  [HttpPut("api/v1/admin/blog/{id:guid}")]
  [Authorize(Policy = "RequireAdminRole")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBlogPostRequest request, CancellationToken cancellationToken)
  {
    var post = await blogPostService.UpdateAsync(id, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(post, "Cập nhật bài viết thành công"));
  }

  [HttpPost("api/v1/admin/blog/upload")]
  [Authorize(Policy = "RequireAdminRole")]
  [Consumes("multipart/form-data")]
  public async Task<IActionResult> UploadImage([FromForm] IFormFile file, CancellationToken cancellationToken = default)
  {
    if (file is null || file.Length == 0)
    {
      return BadRequest(ApiResponseFactory.Failure("File không hợp lệ", "bad_request", "Vui lòng chọn một file ảnh hợp lệ."));
    }

    await using var memory = new MemoryStream();
    await file.CopyToAsync(memory, cancellationToken);
    var bytes = memory.ToArray();
    var validation = imageUploadValidator.Validate(file.ContentType, bytes, file.Length, 8 * 1024 * 1024);
    if (!validation.IsValid)
    {
      return BadRequest(ApiResponseFactory.Failure(validation.ErrorMessage ?? "File ảnh không hợp lệ.", validation.ErrorCode ?? "invalid_image", "File ảnh không hợp lệ."));
    }

    memory.Position = 0;
    var upload = await storageService.UploadAsync(memory, file.FileName, validation.NormalizedContentType ?? file.ContentType, "public/blog", cancellationToken);
    var url = storageService.BuildCanonicalUrl(upload.ObjectKey);
    return Ok(ApiResponseFactory.Success(new BlogImageUploadResponse(url, null, null), "Tải ảnh lên thành công"));
  }

  [HttpDelete("api/v1/admin/blog/{id:guid}")]
  [Authorize(Policy = "RequireAdminRole")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
  {
    await blogPostService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }

  [HttpGet("api/v1/sitemap/blog")]
  public async Task<ContentResult> Sitemap(CancellationToken cancellationToken)
  {
    var xml = await blogPostService.BuildBlogSitemapAsync(GetSiteBaseUrl(), cancellationToken);
    return Content(xml, "application/xml; charset=utf-8");
  }

  [HttpGet("llms.txt")]
  public async Task<ContentResult> LlmsText(CancellationToken cancellationToken)
  {
    var text = await blogPostService.BuildLlmsTextAsync(GetSiteBaseUrl(), cancellationToken);
    return Content(text, "text/plain; charset=utf-8");
  }

  private string GetSiteBaseUrl()
  {
    var host = Request.Headers.TryGetValue("X-Forwarded-Host", out var forwardedHost)
      ? forwardedHost.ToString()
      : Request.Host.Value;
    var scheme = Request.Headers.TryGetValue("X-Forwarded-Proto", out var forwardedProto)
      ? forwardedProto.ToString()
      : Request.Scheme;
    return $"{scheme}://{host}";
  }
}
