namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IUploadStoragePathResolver
{
  string UploadRootPath { get; }

  string GetChatThreadDirectory(Guid threadId);

  string GetAbsolutePathForRelativePath(string relativePath);

  string GetAbsolutePathForRequestPath(string requestPath);

  bool TryGetAbsolutePathForRequestPath(string requestPath, out string absolutePath);
}
