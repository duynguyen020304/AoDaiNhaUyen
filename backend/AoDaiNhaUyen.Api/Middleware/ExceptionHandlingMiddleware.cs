using AoDaiNhaUyen.Api.Responses;
using System.Net;
using System.Text.Json;

namespace AoDaiNhaUyen.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await next(context);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unhandled exception while processing request");

      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
      context.Response.ContentType = "application/json";

      var payload = JsonSerializer.Serialize(
        ApiResponseFactory.Failure(
          "Có lỗi xảy ra",
          "internal_server_error",
          "An unexpected error occurred."),
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

      await context.Response.WriteAsync(payload);
    }
  }
}
