using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class UploadStoragePathResolver(string uploadRootPath) : IUploadStoragePathResolver
{
  private const string UploadRequestPathPrefix = "/upload";
  private readonly string uploadRootPath = Path.GetFullPath(uploadRootPath);

  public string UploadRootPath => uploadRootPath;

  public string GetChatThreadDirectory(long threadId) =>
    GetAbsolutePathForRelativePath(Path.Combine("chat", threadId.ToString()));

  public string GetAbsolutePathForRelativePath(string relativePath)
  {
    if (string.IsNullOrWhiteSpace(relativePath))
    {
      throw new InvalidOperationException("Đường dẫn upload tương đối không hợp lệ.");
    }

    return EnsureWithinUploadRoot(relativePath.Replace('/', Path.DirectorySeparatorChar));
  }

  public string GetAbsolutePathForRequestPath(string requestPath)
  {
    if (!TryGetAbsolutePathForRequestPath(requestPath, out var absolutePath))
    {
      throw new InvalidOperationException("Đường dẫn upload nội bộ không hợp lệ.");
    }

    return absolutePath;
  }

  public bool TryGetAbsolutePathForRequestPath(string requestPath, out string absolutePath)
  {
    absolutePath = string.Empty;

    if (string.IsNullOrWhiteSpace(requestPath) ||
        !requestPath.StartsWith(UploadRequestPathPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (requestPath.Length > UploadRequestPathPrefix.Length &&
        requestPath[UploadRequestPathPrefix.Length] != '/')
    {
      return false;
    }

    var relativePath = requestPath[UploadRequestPathPrefix.Length..].TrimStart('/');
    if (string.IsNullOrWhiteSpace(relativePath))
    {
      return false;
    }

    try
    {
      absolutePath = EnsureWithinUploadRoot(relativePath.Replace('/', Path.DirectorySeparatorChar));
      return true;
    }
    catch (InvalidOperationException)
    {
      return false;
    }
  }

  private string EnsureWithinUploadRoot(string relativePath)
  {
    var candidatePath = Path.GetFullPath(Path.Combine(uploadRootPath, relativePath));
    var rootWithSeparator = uploadRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            + Path.DirectorySeparatorChar;

    if (!candidatePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(candidatePath, uploadRootPath, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("Đường dẫn upload vượt ra ngoài thư mục cho phép.");
    }

    return candidatePath;
  }
}
