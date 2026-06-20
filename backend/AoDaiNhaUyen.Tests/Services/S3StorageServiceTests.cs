using System.Reflection;
using AoDaiNhaUyen.Infrastructure.Services;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class S3StorageServiceTests
{
  [Fact]
  public void SanitizeFileName_RemovesVietnameseDiacriticsAndNonAsciiCharacters()
  {
    var sanitized = InvokePrivateStatic<string>("SanitizeFileName", "ảnh áo dài mùa hè.png");

    Assert.Equal("anh_ao_dai_mua_he.png", sanitized);
  }

  [Fact]
  public void BuildContentDisposition_UsesAsciiFallbackAndUtf8EncodedFileName()
  {
    var contentDisposition = InvokePrivateStatic<string>("BuildContentDisposition", "ảnh áo dài mùa hè.png");

    Assert.StartsWith("inline; filename=\"anh_ao_dai_mua_he.png\"; filename*=UTF-8''", contentDisposition);
    Assert.Contains("%E1%BA%A3nh%20%C3%A1o%20d%C3%A0i%20m%C3%B9a%20h%C3%A8.png", contentDisposition);
    Assert.DoesNotContain("filename=\"ảnh", contentDisposition);
  }

  [Fact]
  public void SanitizeFileName_MapsVietnameseDCharacters()
  {
    var sanitized = InvokePrivateStatic<string>("SanitizeFileName", "đầm Đẹp.png");

    Assert.Equal("dam_Dep.png", sanitized);
  }

  [Fact]
  public void SanitizeFileName_RemovesClientPathSegmentsAcrossPlatforms()
  {
    var sanitized = InvokePrivateStatic<string>("SanitizeFileName", "C:\\Thư Mục\\ảnh áo dài.png");

    Assert.Equal("anh_ao_dai.png", sanitized);
  }

  [Fact]
  public void SanitizeFileName_FallsBackWhenNameHasNoSafeAsciiCharacters()
  {
    var sanitized = InvokePrivateStatic<string>("SanitizeFileName", "🔥");

    Assert.Equal("upload", sanitized);
  }

  private static T InvokePrivateStatic<T>(string methodName, string argument)
  {
    var method = typeof(S3StorageService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
    Assert.NotNull(method);

    var result = method.Invoke(null, [argument]);
    return Assert.IsType<T>(result);
  }
}
