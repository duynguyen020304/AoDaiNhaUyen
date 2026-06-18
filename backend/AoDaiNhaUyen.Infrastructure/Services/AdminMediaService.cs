using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminMediaService(
  AppDbContext dbContext,
  IStorageService storageService,
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
