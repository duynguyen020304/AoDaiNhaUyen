namespace AoDaiNhaUyen.Api.Middleware;

public sealed class SensitiveResponseCacheMiddleware(RequestDelegate next)
{
  private static readonly PathString[] SensitiveApiPrefixes =
  [
    new("/api/admin"),
    new("/api/auth"),
    new("/api/users"),
    new("/api/user"),
    new("/api/cart"),
    new("/api/checkout"),
    new("/api/promo"),
  ];

  public async Task InvokeAsync(HttpContext context)
  {
    if (IsSensitiveApiRequest(context.Request.Path))
    {
      context.Response.OnStarting(() =>
      {
        context.Response.Headers.CacheControl = "private, no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
        return Task.CompletedTask;
      });
    }

    await next(context);
  }

  private static bool IsSensitiveApiRequest(PathString path)
  {
    foreach (var prefix in SensitiveApiPrefixes)
    {
      if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }
}
