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

      var payload = JsonSerializer.Serialize(new
      {
        error = new
        {
          code = "internal_server_error",
          message = "An unexpected error occurred."
        }
      });

      await context.Response.WriteAsync(payload);
    }
  }
}
