using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.Exceptions;
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
      if (ex is FacebookApiException facebookApiException)
      {
        logger.LogWarning(
          facebookApiException,
          "Facebook API error while processing request. Code={ErrorCode} Status={StatusCode}",
          facebookApiException.ErrorCode,
          facebookApiException.StatusCode);
      }
      else
      {
        logger.LogError(ex, "Unhandled exception while processing request");
      }

      if (context.Response.HasStarted)
      {
        logger.LogWarning("Response has already started; cannot write error payload");
        return;
      }

      var statusCode = ex is FacebookApiException facebookException
        ? Math.Clamp(facebookException.StatusCode ?? (int)HttpStatusCode.BadGateway, 400, 599)
        : (int)HttpStatusCode.InternalServerError;

      context.Response.StatusCode = statusCode;
      context.Response.ContentType = "application/json";

      if (ex is FacebookApiException withRetryAfter && withRetryAfter.RetryAfter.HasValue)
      {
        context.Response.Headers.RetryAfter = Math.Ceiling(withRetryAfter.RetryAfter.Value.TotalSeconds).ToString();
      }

      var payload = JsonSerializer.Serialize(
        ex is FacebookApiException facebookError
          ? ApiResponseFactory.Failure(
            "Lỗi Facebook",
            facebookError.ErrorCode,
            facebookError.Message)
          : ApiResponseFactory.Failure(
            "Có lỗi xảy ra",
            "internal_server_error",
            "An unexpected error occurred."),
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

      await context.Response.WriteAsync(payload);
    }
  }
}
