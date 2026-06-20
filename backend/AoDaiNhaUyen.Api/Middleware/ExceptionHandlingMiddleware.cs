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
      else if (ex is ZernioApiException zernioApiException)
      {
        logger.LogWarning(
          zernioApiException,
          "Zernio API error while processing request. Code={ErrorCode} Status={StatusCode}",
          zernioApiException.ErrorCode,
          zernioApiException.StatusCode);
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

      var statusCode = ex switch
      {
        FacebookApiException facebookException => Math.Clamp(facebookException.StatusCode ?? (int)HttpStatusCode.BadGateway, 400, 599),
        ZernioApiException zernioException => Math.Clamp(zernioException.StatusCode ?? (int)HttpStatusCode.BadGateway, 400, 599),
        _ => (int)HttpStatusCode.InternalServerError
      };

      context.Response.StatusCode = statusCode;
      context.Response.ContentType = "application/json";

      var retryAfter = ex switch
      {
        FacebookApiException facebookRetry when facebookRetry.RetryAfter.HasValue => facebookRetry.RetryAfter,
        ZernioApiException zernioRetry when zernioRetry.RetryAfter.HasValue => zernioRetry.RetryAfter,
        _ => null
      };
      if (retryAfter.HasValue)
      {
        context.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.Value.TotalSeconds).ToString();
      }

      var payload = JsonSerializer.Serialize(
        ex switch
        {
          FacebookApiException facebookError => ApiResponseFactory.Failure(
            "Lỗi Facebook",
            facebookError.ErrorCode,
            facebookError.Message),
          ZernioApiException zernioError => ApiResponseFactory.Failure(
            "Lỗi Zernio",
            zernioError.ErrorCode,
            zernioError.Message),
          _ => ApiResponseFactory.Failure(
            "Có lỗi xảy ra",
            "internal_server_error",
            "An unexpected error occurred.")
        },
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

      await context.Response.WriteAsync(payload);
    }
  }
}
