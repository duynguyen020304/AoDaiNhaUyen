using System.Text.Json;
using AoDaiNhaUyen.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  [HttpGet("provinces")]
  public async Task<IActionResult> GetProvinces(CancellationToken cancellationToken)
  {
    var provinces = await GetFromProvincesApiAsync<IReadOnlyList<ProvinceOptionDto>>("/p/", cancellationToken);
    return Ok(ApiResponseFactory.Success(provinces, "Lấy danh sách tỉnh/thành thành công."));
  }

  [HttpGet("provinces/{provinceCode:int}/wards")]
  public async Task<IActionResult> GetWards(int provinceCode, CancellationToken cancellationToken)
  {
    if (provinceCode <= 0)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Mã tỉnh/thành không hợp lệ",
        "invalid_province_code",
        "Vui lòng chọn tỉnh/thành hợp lệ."));
    }

    var province = await GetFromProvincesApiAsync<ProvinceWithWardsDto>($"/p/{provinceCode}?depth=2", cancellationToken);
    return Ok(ApiResponseFactory.Success(province.Wards ?? [], "Lấy danh sách phường/xã thành công."));
  }

  private async Task<T> GetFromProvincesApiAsync<T>(string path, CancellationToken cancellationToken)
  {
    var baseUrl = configuration["ProvincesApi:BaseUrl"]?.Trim().TrimEnd('/');
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
      throw new InvalidOperationException("Thiếu cấu hình ProvincesApi__BaseUrl trong .env.");
    }

    var requestUri = $"{baseUrl}{path}";
    var httpClient = httpClientFactory.CreateClient();
    using var response = await httpClient.GetAsync(requestUri, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      throw new HttpRequestException($"Không thể tải dữ liệu địa giới từ {requestUri}.");
    }

    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
      ?? throw new InvalidOperationException("Dữ liệu địa giới trả về không hợp lệ.");
  }

  private sealed record ProvinceOptionDto(int Code, string Name);
  private sealed record WardOptionDto(int Code, string Name);
  private sealed record ProvinceWithWardsDto(int Code, string Name, IReadOnlyList<WardOptionDto>? Wards);
}
