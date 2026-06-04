using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminMediaService(
  AppDbContext dbContext,
  IStorageService storageService) : IAdminMediaService
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

    var items = await query
      .OrderByDescending(x => x.CreatedAt)
      .Skip((normalizedPage - 1) * normalizedPageSize)
      .Take(normalizedPageSize)
      .Select(x => new UserImageDto(
        x.Id,
        x.ObjectKey,
        x.Url,
        x.Kind,
        x.MimeType,
        x.OriginalFileName,
        x.FileSizeBytes,
        x.SourceType,
        new DateTimeOffset(x.CreatedAt, TimeSpan.Zero)))
      .ToListAsync(ct);

    return new UserImageListDto(items, normalizedPage, normalizedPageSize, totalItems, totalPages);
  }

  public async Task<UserImageDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
  {
    return await dbContext.UserGeneratedImages
      .AsNoTracking()
      .Where(x => x.Id == id && !x.IsDeleted)
      .Select(x => new UserImageDto(
        x.Id,
        x.ObjectKey,
        x.Url,
        x.Kind,
        x.MimeType,
        x.OriginalFileName,
        x.FileSizeBytes,
        x.SourceType,
        new DateTimeOffset(x.CreatedAt, TimeSpan.Zero)))
      .FirstOrDefaultAsync(ct);
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
