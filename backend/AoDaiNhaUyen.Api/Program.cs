using AoDaiNhaUyen.Api.Configuration;
using AoDaiNhaUyen.Api.Middleware;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Services;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Services;
using DotNetEnv;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;

var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var fusionCacheSettings = builder.Configuration.GetSection("FusionCache");
builder.Services.Configure<FusionCacheSettings>(fusionCacheSettings);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var redisInstanceName = builder.Configuration["RedisCacheSettings:InstanceName"] ?? "AoDaiNhaUyen:";
var l1CacheSize = fusionCacheSettings.GetValue<int>("L1CacheSize", 5000);

IConnectionMultiplexer? redisConnection = null;
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
  try
  {
    redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddStackExchangeRedisCache(options =>
    {
      options.Configuration = redisConnectionString;
      options.InstanceName = redisInstanceName;
    });

    builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Warning: Redis unavailable; falling back to L1-only cache. {ex.Message}");
    builder.Services.AddDistributedMemoryCache();
  }
}
else
{
  builder.Services.AddDistributedMemoryCache();
}

var isRedisEnabled = redisConnection is not null;

builder.Services.AddSingleton<IFusionCache>(sp =>
{
  var memoryCache = new MemoryCache(new MemoryCacheOptions
  {
    SizeLimit = l1CacheSize,
    CompactionPercentage = 0.1
  });

  var cache = new FusionCache(
    Options.Create(new FusionCacheOptions { CacheName = "AoDaiNhaUyen" }),
    memoryCache);

  cache.SetupSerializer(new FusionCacheSystemTextJsonSerializer());

  var options = sp.GetRequiredService<IOptions<FusionCacheSettings>>().Value;
  cache.DefaultEntryOptions.Duration = options.L2CacheDuration;
  cache.DefaultEntryOptions.MemoryCacheDuration = options.L1CacheDuration;

  if (options.EnableL2Cache && isRedisEnabled)
  {
    cache.SetupDistributedCache(sp.GetRequiredService<IDistributedCache>());
  }

  if (options.EnableBackplane && isRedisEnabled)
  {
    cache.SetupBackplane(new RedisBackplane(new RedisBackplaneOptions
    {
      Configuration = redisConnectionString
    }));
  }

  var logger = sp.GetRequiredService<ILogger<Program>>();
  logger.LogInformation(
    "FusionCache initialized. L1Size={Size}, L2={L2}, Backplane={Backplane}",
    l1CacheSize,
    options.EnableL2Cache && isRedisEnabled,
    options.EnableBackplane && isRedisEnabled);

  return cache;
});

builder.Services.AddScoped<ICacheKeyService, CacheKeyService>();
builder.Services.AddScoped<IFusionCacheService>(sp => new FusionCacheService(
  sp.GetRequiredService<IFusionCache>(),
  sp.GetRequiredService<ILogger<FusionCacheService>>(),
  sp.GetRequiredService<IOptions<FusionCacheSettings>>(),
  sp.GetService<IConnectionMultiplexer>(),
  redisInstanceName));
builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddScoped<IAdminChatPersistence, AdminChatPersistence>();
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

app.UseMiddleware<SensitiveResponseCacheMiddleware>();
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
