using AoDaiNhaUyen.Api.Authentication;
using AoDaiNhaUyen.Api.Hermes;
using AoDaiNhaUyen.Api.Responses;
using Microsoft.AspNetCore.Authentication;

namespace AoDaiNhaUyen.Api.Middleware;

public sealed class HermesApiDescriptionMiddleware(RequestDelegate next)
{
  public async Task InvokeAsync(HttpContext context, HermesAdminApiDescriptionRegistry registry)
  {
    if (!IsHermesDescriptionRequest(context))
    {
      await next(context);
      return;
    }

    var authResult = await context.AuthenticateAsync(HermesAdminAuthOptions.SchemeName);
    if (!authResult.Succeeded || authResult.Principal is null)
    {
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      await context.Response.WriteAsJsonAsync(ApiResponseFactory.Failure(
        "Hermes admin key không hợp lệ.",
        "unauthorized",
        "Thiếu hoặc sai X-Hermes-Admin-Key."));
      return;
    }

    context.User = authResult.Principal;
    var description = registry.Find(context.Request.Method, context.Request.Path);
    if (description is null)
    {
      context.Response.StatusCode = StatusCodes.Status404NotFound;
      await context.Response.WriteAsJsonAsync(ApiResponseFactory.Failure(
        "Không tìm thấy mô tả API cho Hermes.",
        "hermes_description_not_found",
        string.Join("; ", registry.KnownRoutes())));
      return;
    }

    await context.Response.WriteAsJsonAsync(ApiResponseFactory.Success(description, "Mô tả API cho Hermes."));
  }

  private static bool IsHermesDescriptionRequest(HttpContext context) =>
    context.Request.Path.StartsWithSegments("/api/admin") &&
    string.Equals(context.Request.Headers["X-Hermes-Describe"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase);
}
