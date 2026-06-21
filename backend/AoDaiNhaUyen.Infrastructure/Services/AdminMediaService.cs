using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminMediaService(
  AppDbContext dbContext,
  IStorageService storageService,
  IImageUploadValidator imageUploadValidator,
  IHermesEventOutboxPublisher hermesEvents) : IAdminMediaService
{
  public async Task<UserImageListDto> GetAllAsync(
    int page,
    int pageSize,
    string? sourceType,
    string? search,
    CancellationToken ct = default)
  {
    var query = dbContext.UserGeneratedImages
      .AsNoTracking()
      .Where(x => !x.IsDeleted);

    if (!string.IsNullOrWhiteSpace(sourceType))
    {
      query = query.Where(x => x.SourceType == sourceType);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      query = query.Where(x =>
        (x.OriginalFileName != null && x.OriginalFileName.Contains(search)) ||
        x.ObjectKey.Contains(search));
    }

    var totalItems = await query.CountAsync(ct);
    var normalizedPage = Math.Max(1, page);
    var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
    var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)normalizedPageSize));

    var rows = await query
      .OrderByDescending(x => x.CreatedAt)
      .Skip((normalizedPage - 1) * normalizedPageSize)
      .Take(normalizedPageSize)
      .Select(x => new
      {
        x.Id,
        x.ObjectKey,
        x.Kind,
        x.MimeType,
        x.OriginalFileName,
        x.FileSizeBytes,
        x.SourceType,
        CreatedAt = new DateTimeOffset(x.CreatedAt, TimeSpan.Zero)
      })
      .ToListAsync(ct);

    // Generate presigned GET URLs so images render in admin UI even when bucket is private.
    var items = new List<UserImageDto>(rows.Count);
    foreach (var row in rows)
    {
      var presignedUrl = await storageService.GeneratePresignedGetUrlAsync(row.ObjectKey, 3600, ct);
      items.Add(new UserImageDto(
        row.Id,
        row.ObjectKey,
        presignedUrl,
        row.Kind,
        row.MimeType,
        row.OriginalFileName,
        row.FileSizeBytes,
        row.SourceType,
        row.CreatedAt));
    }

    return new UserImageListDto(items, normalizedPage, normalizedPageSize, totalItems, totalPages);
  }

  public async Task<UserImageDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
  {
    var row = await dbContext.UserGeneratedImages
      .AsNoTracking()
      .Where(x => x.Id == id && !x.IsDeleted)
      .Select(x => new
      {
        x.Id,
        x.ObjectKey,
        x.Kind,
        x.MimeType,
        x.OriginalFileName,
        x.FileSizeBytes,
        x.SourceType,
        CreatedAt = new DateTimeOffset(x.CreatedAt, TimeSpan.Zero)
      })
      .FirstOrDefaultAsync(ct);

    if (row is null) return null;

    var presignedUrl = await storageService.GeneratePresignedGetUrlAsync(row.ObjectKey, 3600, ct);
    return new UserImageDto(
      row.Id,
      row.ObjectKey,
      presignedUrl,
      row.Kind,
      row.MimeType,
      row.OriginalFileName,
      row.FileSizeBytes,
      row.SourceType,
      row.CreatedAt);
  }

  public async Task<UserImageDto> UploadAsync(byte[] bytes, string fileName, string contentType, string sourceType = "admin", CancellationToken ct = default)
  {
    const long maxBytes = 10 * 1024 * 1024;
    var validation = imageUploadValidator.Validate(contentType, bytes, bytes.LongLength, maxBytes);
    if (!validation.IsValid || validation.NormalizedContentType is null)
      throw new ArgumentException(validation.ErrorMessage ?? "Ảnh không hợp lệ.");

    var safeFileName = string.IsNullOrWhiteSpace(fileName) ? $"{Guid.NewGuid():N}" : Path.GetFileName(fileName.Trim());
    var ext = Path.GetExtension(safeFileName);
    if (string.IsNullOrWhiteSpace(ext))
    {
      ext = validation.NormalizedContentType switch
      {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        _ => ".bin"
      };
      safeFileName += ext;
    }

    await using var stream = new MemoryStream(bytes);
    var uploaded = await storageService.UploadAsync(stream, safeFileName, validation.NormalizedContentType, "private/admin-media", ct);
    var now = DateTime.UtcNow;
    var entity = new UserGeneratedImage
    {
      Id = Guid.NewGuid(),
      ObjectKey = uploaded.ObjectKey,
      Url = uploaded.Url,
      Kind = "admin_upload",
      MimeType = uploaded.MimeType,
      OriginalFileName = uploaded.OriginalFileName,
      FileSizeBytes = uploaded.FileSize,
      SourceType = string.IsNullOrWhiteSpace(sourceType) ? "admin" : sourceType.Trim(),
      CreatedAt = now,
      UpdatedAt = now
    };

    dbContext.UserGeneratedImages.Add(entity);
    await dbContext.SaveChangesAsync(ct);

    await hermesEvents.EnqueueAdminEventAsync(
      "media_uploaded",
      "Media",
      entity.Id.ToString("N"),
      new { mediaId = entity.Id, entity.Kind, entity.SourceType, fileName = entity.OriginalFileName, entity.ObjectKey, entity.FileSizeBytes },
      $"media_uploaded:Media:{entity.Id:N}:{now.Ticks}",
      null,
      ct);

    var presignedUrl = await storageService.GeneratePresignedGetUrlAsync(entity.ObjectKey, 3600, ct);
    return new UserImageDto(entity.Id, entity.ObjectKey, presignedUrl, entity.Kind, entity.MimeType, entity.OriginalFileName, entity.FileSizeBytes, entity.SourceType, new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero));
  }

  public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
  {
    var entity = await dbContext.UserGeneratedImages
      .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    if (entity is null)
    {
      return false;
    }

    try
    {
      await storageService.DeleteAsync(entity.ObjectKey, ct);
    }
    catch
    {
      // S3 delete may fail if already gone; continue with soft-delete
    }

    entity.IsDeleted = true;
    entity.DeletedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(ct);

    await hermesEvents.EnqueueAdminEventAsync(
      "media_deleted",
      "Media",
      entity.Id.ToString("N"),
      new { mediaId = entity.Id, kind = entity.Kind, sourceType = entity.SourceType, fileName = entity.OriginalFileName, objectKey = entity.ObjectKey, fileSizeBytes = entity.FileSizeBytes },
      $"media_deleted:Media:{entity.Id:N}:{entity.DeletedAt.GetValueOrDefault().Ticks}",
      null,
      ct);

    return true;
  }

  public async Task<MediaStatsDto> GetStatsAsync(CancellationToken ct = default)
  {
    var images = await dbContext.UserGeneratedImages
      .AsNoTracking()
      .Where(x => !x.IsDeleted)
      .ToListAsync(ct);

    return new MediaStatsDto(
      TotalImages: images.Count,
      TotalSizeBytes: images.Sum(x => x.FileSizeBytes),
      ChatImages: images.Count(x => x.SourceType == "chat"),
      AiTryOnImages: images.Count(x => x.SourceType == "ai_tryon"));
  }
}
