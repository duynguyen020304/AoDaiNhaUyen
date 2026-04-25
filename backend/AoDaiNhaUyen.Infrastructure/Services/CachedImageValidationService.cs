using System.Security.Cryptography;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class CachedImageValidationService(
  AppDbContext dbContext,
  IImageValidationService imageValidationService,
  IOptions<ImageValidationOptions> validationOptions,
  IOptions<GoogleCloudOptions> googleCloudOptions) : ICachedImageValidationService
{
  private const string ProviderName = "vertex-ai";
  private const string InvalidLocalCategory = "invalid_file";
  private readonly ImageValidationOptions options = validationOptions.Value;
  private readonly GoogleCloudOptions cloudOptions = googleCloudOptions.Value;

  public async Task<ImageValidationResultDto> ValidatePersonImageAsync(
    byte[] imageBytes,
    string mimeType,
    string? fileName = null,
    CancellationToken cancellationToken = default)
  {
    var localResult = TryValidateLocalImage(imageBytes, mimeType, fileName, out var metadata);
    if (localResult is not null)
    {
      return localResult;
    }

    var hash = ComputeSha256Hash(imageBytes);
    var now = DateTime.UtcNow;
    var cached = await dbContext.ImageValidationCacheEntries
      .FirstOrDefaultAsync(entry => entry.Sha256Hash == hash && entry.ExpiresAt > now, cancellationToken);

    if (cached is not null)
    {
      cached.LastUsedAt = now;
      await dbContext.SaveChangesAsync(cancellationToken);
      return new ImageValidationResultDto(cached.IsValid, cached.Reason, cached.Category, cached.Confidence);
    }

    var validation = await imageValidationService.ValidatePersonImageAsync(imageBytes, mimeType, cancellationToken);
    await SaveCacheEntryAsync(hash, imageBytes.Length, mimeType, metadata.Width, metadata.Height, validation, now, cancellationToken);
    return validation;
  }

  private ImageValidationResultDto? TryValidateLocalImage(
    byte[] imageBytes,
    string mimeType,
    string? fileName,
    out ImageMetadata metadata)
  {
    metadata = new ImageMetadata(0, 0);

    if (imageBytes.Length == 0 || imageBytes.Length > options.MaxImageBytes)
    {
      return Invalid("Ảnh phải nhỏ hơn 8MB và không được để trống.");
    }

    if (string.IsNullOrWhiteSpace(mimeType) ||
        !mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mimeType, "image/gif", StringComparison.OrdinalIgnoreCase))
    {
      return Invalid("Chỉ hỗ trợ ảnh PNG, JPG hoặc WEBP.");
    }

    if (!string.IsNullOrWhiteSpace(fileName))
    {
      var extension = Path.GetExtension(fileName).ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(extension) ||
          !options.AllowedExtensions.Any(allowed => string.Equals(allowed, extension, StringComparison.OrdinalIgnoreCase)))
      {
        return Invalid("Chỉ hỗ trợ ảnh PNG, JPG hoặc WEBP.");
      }
    }

    try
    {
      using var stream = new MemoryStream(imageBytes);
      var imageInfo = Image.Identify(stream);
      if (imageInfo is null)
      {
        return Invalid("Không thể đọc kích thước ảnh. Vui lòng chọn ảnh PNG, JPG hoặc WEBP hợp lệ.");
      }

      metadata = new ImageMetadata(imageInfo.Width, imageInfo.Height);
    }
    catch (UnknownImageFormatException)
    {
      return Invalid("Không thể đọc định dạng ảnh. Vui lòng chọn ảnh PNG, JPG hoặc WEBP hợp lệ.");
    }
    catch (InvalidImageContentException)
    {
      return Invalid("Ảnh không hợp lệ hoặc bị lỗi. Vui lòng chọn ảnh khác.");
    }

    if (metadata.Width < options.MinWidth || metadata.Height < options.MinHeight)
    {
      return Invalid($"Ảnh phải có kích thước tối thiểu {options.MinWidth}x{options.MinHeight}px.");
    }

    if (metadata.Width > options.MaxWidth || metadata.Height > options.MaxHeight)
    {
      return Invalid($"Ảnh phải có kích thước tối đa {options.MaxWidth}x{options.MaxHeight}px.");
    }

    return null;
  }

  private async Task SaveCacheEntryAsync(
    string hash,
    long fileSizeBytes,
    string mimeType,
    int width,
    int height,
    ImageValidationResultDto validation,
    DateTime now,
    CancellationToken cancellationToken)
  {
    var staleEntries = await dbContext.ImageValidationCacheEntries
      .Where(entry => entry.Sha256Hash == hash || entry.ExpiresAt <= now)
      .ToListAsync(cancellationToken);
    dbContext.ImageValidationCacheEntries.RemoveRange(staleEntries);

    dbContext.ImageValidationCacheEntries.Add(new ImageValidationCacheEntry
    {
      Sha256Hash = hash,
      MimeType = mimeType,
      FileSizeBytes = fileSizeBytes,
      Width = width,
      Height = height,
      IsValid = validation.IsValid,
      Reason = validation.Reason,
      Category = validation.Category,
      Confidence = validation.Confidence,
      Provider = ProviderName,
      Model = cloudOptions.ImageValidationModel,
      CreatedAt = now,
      ExpiresAt = now.AddDays(options.CacheTtlDays)
    });

    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      dbContext.ChangeTracker.Clear();
      var existing = await dbContext.ImageValidationCacheEntries
        .FirstOrDefaultAsync(entry => entry.Sha256Hash == hash, cancellationToken);
      if (existing is null)
      {
        throw;
      }

      existing.MimeType = mimeType;
      existing.FileSizeBytes = fileSizeBytes;
      existing.Width = width;
      existing.Height = height;
      existing.IsValid = validation.IsValid;
      existing.Reason = validation.Reason;
      existing.Category = validation.Category;
      existing.Confidence = validation.Confidence;
      existing.Provider = ProviderName;
      existing.Model = cloudOptions.ImageValidationModel;
      existing.CreatedAt = now;
      existing.ExpiresAt = now.AddDays(options.CacheTtlDays);
      existing.LastUsedAt = null;
      await dbContext.SaveChangesAsync(cancellationToken);
    }
  }

  private static string ComputeSha256Hash(byte[] bytes) =>
    Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

  private static ImageValidationResultDto Invalid(string reason) =>
    new(false, reason, InvalidLocalCategory, null);

  private sealed record ImageMetadata(int Width, int Height);
}
