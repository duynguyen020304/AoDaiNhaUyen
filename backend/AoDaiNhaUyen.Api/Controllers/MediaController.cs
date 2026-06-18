using System.Security.Claims;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public sealed class MediaController(
  AppDbContext dbContext,
  IStorageService storageService) : ControllerBase
{
  /// <summary>
  /// Lấy danh sách ảnh đã tạo của người dùng hiện tại.
  /// </summary>
  [HttpGet("my-images")]
  [Authorize(Policy = "RequireAdminOrCustomer")]
  public async Task<IActionResult> GetMyImages(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 12,
    [FromQuery] string? sourceType = null,
    CancellationToken cancellationToken = default)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
    {
      return Unauthorized(ApiResponseFactory.Failure("Không xác định được người dùng", "unauthorized", "Vui lòng đăng nhập lại."));
    }

    var query = dbContext.UserGeneratedImages
      .AsNoTracking()
      .Where(x => x.UserId == userId && !x.IsDeleted);

    if (!string.IsNullOrWhiteSpace(sourceType))
    {
      query = query.Where(x => x.SourceType == sourceType);
    }

    var totalItems = await query.CountAsync(cancellationToken);
    var normalizedPage = Math.Max(1, page);
    var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
    var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)normalizedPageSize));

    var rows = await query
      .OrderByDescending(x => x.CreatedAt)
      .Skip((normalizedPage - 1) * normalizedPageSize)
      .Take(normalizedPageSize)
      .ToListAsync(cancellationToken);

    var items = new List<UserImageDto>(rows.Count);
    foreach (var image in rows)
    {
      var imageUrl = await ResolveImageUrlAsync(image.ObjectKey, image.Url, 3600, cancellationToken);
      items.Add(new UserImageDto(
        image.Id,
        image.ObjectKey,
        imageUrl,
        image.Kind,
        image.MimeType,
        image.OriginalFileName,
        image.FileSizeBytes,
        image.SourceType,
        new DateTimeOffset(image.CreatedAt, TimeSpan.Zero)));
    }

    return Ok(ApiResponseFactory.Success(
      new UserImageListDto(items, normalizedPage, normalizedPageSize, totalItems, totalPages)));
  }

  /// <summary>
  /// Lấy presigned URL cho ảnh.
  /// </summary>
  [HttpGet("{id:guid}/url")]
  [Authorize(Policy = "RequireAdminOrCustomer")]
  public async Task<IActionResult> GetImageUrl(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
    {
      return Unauthorized(ApiResponseFactory.Failure("Không xác định được người dùng", "unauthorized", "Vui lòng đăng nhập lại."));
    }

    var image = await dbContext.UserGeneratedImages
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted, cancellationToken);

    if (image is null)
    {
      return NotFound(ApiResponseFactory.Failure("Không tìm thấy ảnh", "not_found", "Ảnh không tồn tại."));
    }

    var presignedUrl = await ResolveImageUrlAsync(image.ObjectKey, image.Url, 3600, cancellationToken);

    return Ok(ApiResponseFactory.Success(new { url = presignedUrl, image.MimeType, image.OriginalFileName }));
  }

  /// <summary>
  /// Tải ảnh về (redirect đến presigned URL).
  /// </summary>
  [HttpGet("{id:guid}/download")]
  [Authorize(Policy = "RequireAdminOrCustomer")]
  public async Task<IActionResult> DownloadImage(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
    {
      return Unauthorized();
    }

    var image = await dbContext.UserGeneratedImages
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted, cancellationToken);

    if (image is null)
    {
      return NotFound();
    }

    var presignedUrl = await ResolveImageUrlAsync(image.ObjectKey, image.Url, 300, cancellationToken);
    return Redirect(presignedUrl);
  }

  private async Task<string> ResolveImageUrlAsync(
    string objectKey,
    string fallbackUrl,
    int expirationSeconds,
    CancellationToken cancellationToken)
  {
    if (!string.IsNullOrWhiteSpace(objectKey))
    {
      return await storageService.GeneratePresignedGetUrlAsync(objectKey, expirationSeconds, cancellationToken);
    }

    return fallbackUrl;
  }
}
