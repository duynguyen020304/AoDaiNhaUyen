using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AoDaiNhaUyen.Api.Configuration;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Api.Services;

public sealed class S3StorageService : IStorageService
{
  private const string ObjectKeyRootPrefix = "aodainhauyen";
  private const string PrivatePrefix = "aodainhauyen/private";
  private const string PublicPrefix = "aodainhauyen/public";
  private const string PublicProductsPrefix = "aodainhauyen/public/products";
  private const long MultipartUploadThresholdBytes = 50L * 1024 * 1024;
  private const long MultipartPartSizeBytes = 10L * 1024 * 1024;

  private readonly IAmazonS3 _amazonS3;
  private readonly S3StorageSettings _settings;
  private readonly ILogger<S3StorageService> _logger;

  public S3StorageService(
    IAmazonS3 amazonS3,
    IOptions<S3StorageSettings> settings,
    ILogger<S3StorageService> logger)
  {
    _amazonS3 = amazonS3;
    _settings = settings.Value;
    _logger = logger;
  }

  public async Task<UploadedFileResult> UploadAsync(
    Stream stream,
    string fileName,
    string contentType,
    string? folder = null,
    CancellationToken ct = default)
  {
    EnsureConfigured();

    var objectKey = ResolveUploadObjectKey(fileName, folder);
    var contentDisposition = BuildContentDisposition(fileName);

    if (stream.Length > MultipartUploadThresholdBytes)
    {
      return await UploadMultipartAsync(stream, fileName, contentType, objectKey, contentDisposition, ct);
    }

    var request = new PutObjectRequest
    {
      BucketName = _settings.BucketName,
      Key = objectKey,
      InputStream = stream,
      ContentType = contentType,
      Headers = { ContentDisposition = contentDisposition }
    };

    var fileSize = stream.Length;

    await _amazonS3.PutObjectAsync(request, ct);

    var url = BuildCanonicalUrl(objectKey);

    _logger.LogInformation("Uploaded {FileName} to S3 key {ObjectKey}", fileName, objectKey);

    return new UploadedFileResult(
      objectKey,
      url,
      null,
      contentType,
      fileSize,
      fileName);
  }

  public async Task<string> GeneratePresignedGetUrlAsync(
    string objectKey,
    int expirationSeconds = 3600,
    CancellationToken ct = default)
  {
    EnsureConfigured();

    var normalizedKey = NormalizeObjectKey(objectKey);
    var expires = DateTime.UtcNow.AddSeconds(
      Math.Clamp(expirationSeconds, 60, 604800));

    var request = new GetPreSignedUrlRequest
    {
      BucketName = _settings.BucketName,
      Key = normalizedKey,
      Expires = expires,
      Verb = HttpVerb.GET
    };

    return await Task.FromResult(_amazonS3.GetPreSignedURL(request));
  }

  public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
  {
    EnsureConfigured();
    var normalizedKey = NormalizeObjectKey(objectKey);

    await _amazonS3.DeleteObjectAsync(
      _settings.BucketName,
      normalizedKey,
      ct);

    _logger.LogInformation("Deleted S3 key {ObjectKey}", normalizedKey);
  }

  public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
  {
    EnsureConfigured();
    var normalizedKey = NormalizeObjectKey(objectKey);

    var response = await _amazonS3.GetObjectAsync(
      _settings.BucketName,
      normalizedKey,
      ct);

    return response.ResponseStream;
  }



  public async Task PutObjectWithKeyAsync(
    string objectKey,
    Stream stream,
    string contentType,
    CancellationToken ct = default)
  {
    EnsureConfigured();
    var normalizedKey = NormalizeObjectKey(objectKey);

    var request = new PutObjectRequest
    {
      BucketName = _settings.BucketName,
      Key = normalizedKey,
      InputStream = stream,
      ContentType = contentType
    };

    await _amazonS3.PutObjectAsync(request, ct);

    _logger.LogInformation("Uploaded to S3 key {ObjectKey}", normalizedKey);
  }
  public async Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default)
  {
    EnsureConfigured();
    var normalizedKey = NormalizeObjectKey(objectKey);

    try
    {
      await _amazonS3.GetObjectMetadataAsync(
        _settings.BucketName,
        normalizedKey,
        ct);
      return true;
    }
    catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      return false;
    }
    catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // IAM/bucket policy lacks s3:GetObject — treat as "unknown, skip exists check"
      _logger.LogWarning("Forbidden checking existence of {ObjectKey}: {Message}. Check IAM policy has s3:GetObject on {BucketName}/*.",
        normalizedKey, ex.Message, _settings.BucketName);
      return false;
    }
  }

  public string BuildCanonicalUrl(string objectKey)
  {
    var encodedKey = EncodeObjectKey(objectKey);

    if (!string.IsNullOrWhiteSpace(_settings.ServiceUrl))
    {
      return $"{_settings.ServiceUrl.TrimEnd('/')}/{_settings.BucketName}/{encodedKey}";
    }

    if (_settings.UsePathStyle)
    {
      return $"https://s3.{_settings.Region}.amazonaws.com/{_settings.BucketName}/{encodedKey}";
    }

    return $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{encodedKey}";
  }

  public async Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default)
  {
    EnsureConfigured();
    var normalizedKey = NormalizeObjectKey(objectKey);

    var fileName = normalizedKey[(normalizedKey.LastIndexOf('/') + 1)..];
    var publicKey = $"{PublicProductsPrefix}/{fileName}";

    var copyRequest = new CopyObjectRequest
    {
      SourceBucket = _settings.BucketName,
      SourceKey = normalizedKey,
      DestinationBucket = _settings.BucketName,
      DestinationKey = publicKey
    };
    await _amazonS3.CopyObjectAsync(copyRequest, ct);

    _logger.LogInformation("Copied {SourceKey} to public key {PublicKey}", normalizedKey, publicKey);

    return BuildCanonicalUrl(publicKey);
  }

  public bool IsConfigured()
  {
    return !string.IsNullOrWhiteSpace(_settings.BucketName)
      && (!string.IsNullOrWhiteSpace(_settings.Region) || !string.IsNullOrWhiteSpace(_settings.ServiceUrl));
  }

  private async Task<UploadedFileResult> UploadMultipartAsync(
    Stream stream,
    string fileName,
    string contentType,
    string objectKey,
    string contentDisposition,
    CancellationToken ct)
  {
    var transferRequest = new TransferUtilityUploadRequest
    {
      BucketName = _settings.BucketName,
      Key = objectKey,
      InputStream = stream,
      ContentType = contentType,
      PartSize = MultipartPartSizeBytes
    };
    transferRequest.Headers.ContentDisposition = contentDisposition;

    var fileSize = stream.Length;

    var transferUtility = new TransferUtility(_amazonS3);
    await transferUtility.UploadAsync(transferRequest, ct);

    var url = BuildCanonicalUrl(objectKey);

    _logger.LogInformation("Multipart uploaded {FileName} to S3 key {ObjectKey}", fileName, objectKey);

    return new UploadedFileResult(
      objectKey,
      url,
      null,
      contentType,
      fileSize,
      fileName);
  }

  private string ResolveUploadObjectKey(string fileName, string? folder)
  {
    var sanitized = SanitizeFileName(fileName);
    var uniqueName = $"{Guid.NewGuid():N}_{sanitized}";

    if (!string.IsNullOrWhiteSpace(folder))
    {
      var cleanFolder = folder.Replace("\\", "/").Trim().Trim('/');
      return $"{ObjectKeyRootPrefix}/{cleanFolder}/{uniqueName}";
    }

    return $"{ObjectKeyRootPrefix}/{uniqueName}";
  }

  private string NormalizeObjectKey(string objectKey)
  {
    if (string.IsNullOrWhiteSpace(objectKey))
    {
      throw new ArgumentException("Object key không được để trống.");
    }

    var normalized = objectKey
      .Replace("\\", "/")
      .Trim()
      .TrimStart('/');

    while (normalized.Contains("//", StringComparison.Ordinal))
    {
      normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
    }

    if (normalized.Contains("..", StringComparison.Ordinal))
    {
      throw new ArgumentException("Object key không được chứa path traversal.");
    }

    if (!normalized.StartsWith(ObjectKeyRootPrefix + "/", StringComparison.Ordinal)
        && !normalized.Equals(ObjectKeyRootPrefix, StringComparison.Ordinal))
    {
      normalized = $"{ObjectKeyRootPrefix}/{normalized}";
    }

    return normalized;
  }

  private static string SanitizeFileName(string fileName)
  {
    var invalidChars = Path.GetInvalidFileNameChars();
    var sanitized = new StringBuilder(fileName.Length);
    foreach (var c in fileName)
    {
      sanitized.Append(invalidChars.Contains(c) ? '_' : c);
    }
    return sanitized.ToString();
  }

  private static string BuildContentDisposition(string fileName)
  {
    var encodedFileName = Uri.EscapeDataString(fileName);
    return $"inline; filename=\"{fileName}\"; filename*=UTF-8''{encodedFileName}";
  }

  private static string EncodeObjectKey(string objectKey)
  {
    return string.Join("/",
      objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries)
        .Select(Uri.EscapeDataString));
  }

  private void EnsureConfigured()
  {
    if (string.IsNullOrWhiteSpace(_settings.BucketName))
    {
      throw new InvalidOperationException("S3Storage:BucketName chưa được cấu hình.");
    }
  }
}
