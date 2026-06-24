using System.Net;
using AoDaiNhaUyen.Api.Controllers.Admin;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

/// <summary>
/// Offline integration tests for <see cref="AdminAiTryOnController.Generate"/>.
/// Verifies success criteria #7 (backend reuses catalog try-on engine and
/// returns a presigned URL) and #9 (clear fallback messages on each failure
/// mode) without requiring live Facebook/Hermes/Gemini credentials.
/// </summary>
public sealed class AdminAiTryOnControllerTests
{
  private static readonly Guid GarmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private const string PersonImageUrl = "https://storage.example.com/private/social-inbox/c/priv.jpg?sig=x";

  [Fact]
  public async Task Generate_Success_ReturnsResultDtoWithPresignedUrl()
  {
    var expectedUrl = "https://storage.example.com/private/ai-tryon/tryon-abc.jpg?sig=presigned";
    var catalog = new StubCatalogTryOnService(
      _ => new AiTryOnResultDto(expectedUrl, "image/jpeg", GeneratedImageId: null));
    var controller = CreateController(catalog, ServeImage([1, 2, 3, 4]));

    var request = new AdminGenerateTryOnRequestDto(
      GarmentProductId: GarmentId,
      GarmentVariantId: null,
      AccessoryProductIds: null,
      PersonImageUrl: PersonImageUrl);

    var result = await controller.Generate(request, CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result);
    var envelope = ReadEnvelope(ok.Value!);
    Assert.True(envelope.Success);
    Assert.Equal("Tạo ảnh thử đồ thành công", envelope.Message);
    var dto = Assert.IsType<AdminGenerateTryOnResultDto>(envelope.Data);
    Assert.Equal(expectedUrl, dto.ResultImageUrl);
    Assert.Equal("image/jpeg", dto.MimeType);
    // Catalog service must have received the downloaded person bytes + the chosen garment.
    Assert.Single(catalog.Calls);
    Assert.Equal(GarmentId, catalog.Calls[0].GarmentProductId);
    Assert.Equal(new byte[] { 1, 2, 3, 4 }, catalog.Calls[0].PersonImageBytes);
  }

  [Fact]
  public async Task Generate_ImageValidationProviderFailure_MapsToImageValidationFailed()
  {
    var catalog = new StubCatalogTryOnService(_ => throw new ImageValidationProviderException("bad face"));
    var controller = CreateController(catalog, ServeImage([1, 2, 3, 4]));

    var result = await controller.Generate(NewRequest(), CancellationToken.None);

    var status = Assert.IsType<ObjectResult>(result);
    Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
    var (code, message) = AssertEnvelopeFailure(status.Value!);
    Assert.Equal("image_validation_failed", code);
    Assert.Contains("ảnh rõ hơn", message);
  }

  [Fact]
  public async Task Generate_MissingTryOnAsset_MapsToMissingAssetCode()
  {
    var catalog = new StubCatalogTryOnService(_ => throw new FileNotFoundException("no asset"));
    var controller = CreateController(catalog, ServeImage([1, 2, 3, 4]));

    var result = await controller.Generate(NewRequest(), CancellationToken.None);

    var bad = Assert.IsType<BadRequestObjectResult>(result);
    var (code, _) = AssertEnvelopeFailure(bad.Value!);
    Assert.Equal("missing_tryon_asset", code);
  }

  [Fact]
  public async Task Generate_VertexAiProviderFailure_MapsToVertexAiFailed()
  {
    var catalog = new StubCatalogTryOnService(_ => throw new AiTryOnProviderException("vertex down"));
    var controller = CreateController(catalog, ServeImage([1, 2, 3, 4]));

    var result = await controller.Generate(NewRequest(), CancellationToken.None);

    var status = Assert.IsType<ObjectResult>(result);
    Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
    var (code, _) = AssertEnvelopeFailure(status.Value!);
    Assert.Equal("vertex_ai_failed", code);
  }

  [Fact]
  public async Task Generate_PersonImageTooLarge_MapsToDownloadFailed()
  {
    // Serve a payload bigger than 8MB so the streaming size guard trips.
    var oversized = new byte[(8 * 1024 * 1024) + 1];
    var catalog = new StubCatalogTryOnService(_ => new AiTryOnResultDto("u", "image/jpeg", null));
    var controller = CreateController(catalog, ServeImage(oversized));

    var result = await controller.Generate(NewRequest(), CancellationToken.None);

    var bad = Assert.IsType<BadRequestObjectResult>(result);
    var (code, _) = AssertEnvelopeFailure(bad.Value!);
    Assert.Equal("person_image_download_failed", code);
    Assert.Empty(catalog.Calls); // catalog engine never invoked when download fails
  }

  private static AdminGenerateTryOnRequestDto NewRequest() => new(
    GarmentProductId: GarmentId,
    GarmentVariantId: null,
    AccessoryProductIds: null,
    PersonImageUrl: PersonImageUrl);

  private static AdminAiTryOnController CreateController(ICatalogTryOnService catalog, IHttpClientFactory httpFactory) =>
    new(catalog, httpFactory, NullLogger<AdminAiTryOnController>.Instance);

  /// <summary>Serves the given bytes for any GET, with declared image/jpeg content.</summary>
  private static IHttpClientFactory ServeImage(byte[] bytes) =>
    new StubHttpClientFactory(new FixedBytesHandler(bytes));

  /// <summary>
  /// Reads envelope fields by reflection on the runtime type, independent of
  /// JSON serialization options (test-time serialization defaults to PascalCase
  /// while the API pipeline uses camelCase — reflection avoids that mismatch).
  /// </summary>
  private static (bool Success, string Message, object? Data, object? Errors) ReadEnvelope(object value)
  {
    var type = value.GetType();
    var success = (bool)(type.GetProperty("Success")?.GetValue(value) ?? false);
    var message = (string?)(type.GetProperty("Message")?.GetValue(value) ?? string.Empty) ?? string.Empty;
    var data = type.GetProperty("Data")?.GetValue(value);
    var errors = type.GetProperty("Errors")?.GetValue(value);
    return (success, message, data, errors);
  }

  private static void AssertEnvelopeSuccess(object value, string expectedMessage)
  {
    var (success, message, data, _) = ReadEnvelope(value);
    Assert.True(success);
    Assert.Equal(expectedMessage, message);
    Assert.NotNull(data);
  }

  private static (string Code, string Message) AssertEnvelopeFailure(object value)
  {
    var (success, _, _, errorsObj) = ReadEnvelope(value);
    Assert.False(success);
    Assert.NotNull(errorsObj);
    // Errors is IReadOnlyList<ApiError>; reflect the first element's Code/Message.
    var errorsList = (System.Collections.IEnumerable)errorsObj!;
    object? first = null;
    foreach (var item in errorsList) { first = item; break; }
    Assert.NotNull(first);
    var errType = first!.GetType();
    var code = (string?)(errType.GetProperty("Code")?.GetValue(first) ?? string.Empty) ?? string.Empty;
    var msg = (string?)(errType.GetProperty("Message")?.GetValue(first) ?? string.Empty) ?? string.Empty;
    return (code, msg);
  }

  private sealed class StubCatalogTryOnService(Func<CatalogAiTryOnRequestDto, AiTryOnResultDto> impl) : ICatalogTryOnService
  {
    public List<CatalogAiTryOnRequestDto> Calls { get; } = new();

    public Task<AiTryOnCatalogDto> GetCatalogAsync(AiTryOnCatalogQueryDto query, CancellationToken cancellationToken = default)
      => throw new NotImplementedException();

    public Task<AiTryOnResultDto> CreateAsync(CatalogAiTryOnRequestDto request, CancellationToken cancellationToken = default)
    {
      Calls.Add(request);
      return Task.FromResult(impl(request));
    }
  }

  private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
  }

  /// <summary>A handler returning fixed bytes (default 200 OK) for any request.</summary>
  private sealed class FixedBytesHandler(byte[] bytes) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var content = new ByteArrayContent(bytes);
      content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
      content.Headers.ContentLength = bytes.Length;
      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }
  }
}
