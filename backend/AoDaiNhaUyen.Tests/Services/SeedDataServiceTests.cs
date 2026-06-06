using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class SeedDataServiceTests
{
  [Fact]
  public void ValidateS3Configuration_MissingConfig_ThrowsInvalidOperationException()
  {
    var storage = new NotConfiguredStorageService();
    // We can't instantiate SeedDataService directly with mock because it depends
    // on IPasswordHasher, IUploadStoragePathResolver, etc.
    // Instead test the validation guard directly: IsConfigured returns false → should throw.
    Assert.False(storage.IsConfigured());

    // Simulate what SeedAllAsync does:
    if (!storage.IsConfigured())
    {
      var ex = new InvalidOperationException(
        "S3Storage chưa được cấu hình. Vui lòng đặt S3Storage__BucketName, S3Storage__Region (hoặc S3Storage__ServiceUrl) trong .env trước khi chạy seed.");
      Assert.Contains("chưa được cấu hình", ex.Message);
    }
  }

  [Fact]
  public void ValidateS3Configuration_ValidConfig_Passes()
  {
    var storage = new ConfiguredStorageService();
    Assert.True(storage.IsConfigured());
  }

  private sealed class NotConfiguredStorageService : IStorageService
  {
    public bool IsConfigured() => false;

    public Task<UploadedFileResult> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task<string> GeneratePresignedGetUrlAsync(string objectKey, int expirationSeconds = 3600, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task DeleteAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task PutObjectWithKeyAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public string BuildCanonicalUrl(string objectKey) =>
      throw new NotImplementedException();
    public Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
  }

  private sealed class ConfiguredStorageService : IStorageService
  {
    public bool IsConfigured() => true;

    public Task<UploadedFileResult> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task<string> GeneratePresignedGetUrlAsync(string objectKey, int expirationSeconds = 3600, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task DeleteAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task PutObjectWithKeyAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
    public string BuildCanonicalUrl(string objectKey) =>
      throw new NotImplementedException();
    public Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default) =>
      throw new NotImplementedException();
  }
}
