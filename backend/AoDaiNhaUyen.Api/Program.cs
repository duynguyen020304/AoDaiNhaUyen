using AoDaiNhaUyen.Api.Configuration;
using AoDaiNhaUyen.Api.Middleware;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Services;
using DotNetEnv;
using Microsoft.Extensions.FileProviders;

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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseStaticFiles();

var uploadStoragePathResolver = app.Services.GetRequiredService<IUploadStoragePathResolver>();
Directory.CreateDirectory(uploadStoragePathResolver.UploadRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadStoragePathResolver.UploadRootPath),
    RequestPath = "/upload"
});

app.UseAuthentication();
app.UseAuthorization();
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
