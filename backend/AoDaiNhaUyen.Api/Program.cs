using AoDaiNhaUyen.Api.Configuration;
using AoDaiNhaUyen.Api.Middleware;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Services;
using DotNetEnv;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;

var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
  options.AddPolicy("Frontend", policy =>
  {
    var origins = GetFrontendOrigins(builder.Configuration);

    policy
      .WithOrigins(origins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
    });
});
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IUploadStoragePathResolver>(
  _ => new UploadStoragePathResolver(Path.Combine(builder.Environment.ContentRootPath, "upload")));
builder.Services.AddBackendServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// TODO: Remove /upload static file serving once all consumers migrated to S3.
// Chat uploads use S3 (private/chat/), product images use S3, curated try-on assets use S3.
// Some legacy paths may still reference local /upload/ — keep until full audit confirms safe removal.
var uploadStoragePathResolver = app.Services.GetRequiredService<IUploadStoragePathResolver>();
Directory.CreateDirectory(uploadStoragePathResolver.UploadRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadStoragePathResolver.UploadRootPath),
    RequestPath = "/upload"
});

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    await next(context);
    if (context.Response.StatusCode == 403)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var roles = string.Join(",", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));
        logger.LogWarning("[AuthZ] {UserId} (roles: {Roles}) denied on {Path} {Method}",
            userId, roles, context.Request.Path, context.Request.Method);
    }
});
app.MapControllers();

if (app.Configuration.GetValue<bool>("RunMigrationsAndSeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var seedDataService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
    await seedDataService.SeedAllAsync();
}

app.Run();

static string[] GetFrontendOrigins(IConfiguration configuration)
{
  var configuredOrigins = configuration.GetSection("FrontendOrigins").Get<string[]>();
  if (configuredOrigins is { Length: > 0 })
  {
    return NormalizeOrigins(configuredOrigins);
  }

  var rawOrigins = configuration["FrontendOrigins"];
  if (!string.IsNullOrWhiteSpace(rawOrigins))
  {
    var parsedOrigins = rawOrigins
      .Trim()
      .TrimStart('[')
      .TrimEnd(']')
      .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (parsedOrigins.Length > 0)
    {
      return NormalizeOrigins(parsedOrigins);
    }
  }

  return
  [
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5174",
    "https://aodainhauyen.io.vn",
    "https://backup.aodainhauyen.io.vn"
  ];
}

static string[] NormalizeOrigins(IEnumerable<string> origins)
{
  return origins
    .Select(origin => origin.Trim().Trim('"', '\'').TrimEnd('/'))
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
}
