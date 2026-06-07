using Amazon;
using Amazon.S3;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// All logs go to stderr to keep stdout clean for JSON-RPC messages
builder.Logging.AddConsole(options =>
  options.LogToStandardErrorThreshold = LogLevel.Trace);

// Load .env
var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
if (File.Exists(envPath))
  Env.Load(envPath);

// Connection string (env var → appsettings.json → throw)
var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
              ?? builder.Configuration["ConnectionStrings:DefaultConnection"];
if (string.IsNullOrWhiteSpace(connStr) || connStr == "CHANGE_ME")
  throw new InvalidOperationException(
    "ConnectionStrings__DefaultConnection chưa được cấu hình. " +
    "Đặt trong .env hoặc appsettings.json.");

var services = builder.Services;

// ---------------------------------------------------------------------------
// DbContext
// ---------------------------------------------------------------------------
services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connStr));

// ---------------------------------------------------------------------------
// Configuration options
// ---------------------------------------------------------------------------
services.Configure<GoogleCloudOptions>(
  builder.Configuration.GetSection("GoogleCloud"));
services.Configure<S3StorageSettings>(
  builder.Configuration.GetSection(S3StorageSettings.SectionName));

// ---------------------------------------------------------------------------
// S3 Storage
// ---------------------------------------------------------------------------
services.AddSingleton<IAmazonS3>(sp =>
{
  var s3Settings = sp.GetRequiredService<IOptions<S3StorageSettings>>().Value;
  var config = new AmazonS3Config
  {
    ForcePathStyle = s3Settings.UsePathStyle
  };
  if (!string.IsNullOrWhiteSpace(s3Settings.ServiceUrl))
    config.ServiceURL = s3Settings.ServiceUrl;
  else if (!string.IsNullOrWhiteSpace(s3Settings.Region))
    config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3Settings.Region);

  if (!string.IsNullOrWhiteSpace(s3Settings.AccessKey)
      && !string.IsNullOrWhiteSpace(s3Settings.SecretKey))
    return new AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, config);

  return new AmazonS3Client(config);
});

// ---------------------------------------------------------------------------
// Storage & Image visibility
// ---------------------------------------------------------------------------
services.AddScoped<IStorageService, S3StorageService>();
services.AddScoped<IImageVisibilityService, ImageVisibilityService>();

// ---------------------------------------------------------------------------
// Admin services (used by MCP tools)
// ---------------------------------------------------------------------------
services.AddScoped<IAdminDashboardService, AdminDashboardService>();
services.AddScoped<IAdminProductService, AdminProductService>();
services.AddScoped<IAdminCategoryService, AdminCategoryService>();
services.AddScoped<IAdminUserService, AdminUserService>();
services.AddScoped<IAdminRoleService, AdminRoleService>();

// ---------------------------------------------------------------------------
// MCP server
// ---------------------------------------------------------------------------
services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
