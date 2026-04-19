using AoDaiNhaUyen.Infrastructure.Services;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class UploadStoragePathResolverTests
{
  [Fact]
  public void Resolver_MapsUploadUrlIntoSameCanonicalRootUsedForStaticFiles()
  {
    using var contentRoot = new TemporaryDirectory();
    var resolver = new UploadStoragePathResolver(Path.Combine(contentRoot.Path, "upload"));

    var absolutePath = resolver.GetAbsolutePathForRequestPath("/upload/chat/42/result.png");

    Assert.Equal(Path.Combine(contentRoot.Path, "upload", "chat", "42", "result.png"), absolutePath);
    Assert.Equal(Path.Combine(contentRoot.Path, "upload"), resolver.UploadRootPath);
  }

  private sealed class TemporaryDirectory : IDisposable
  {
    public TemporaryDirectory()
    {
      Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"aodai-upload-resolver-{Guid.NewGuid():N}");
      Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
      if (Directory.Exists(Path))
      {
        Directory.Delete(Path, recursive: true);
      }
    }
  }
}
