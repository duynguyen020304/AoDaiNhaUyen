using Amazon;
using Amazon.S3;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using AoDaiNhaUyen.Mcp.Auth;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
if (File.Exists(envPath))
  Env.Load(envPath);

var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
              ?? builder.Configuration["ConnectionStrings:DefaultConnection"];
if (string.IsNullOrWhiteSpace(connStr) || connStr == "CHANGE_ME")
  throw new InvalidOperationException(
    "ConnectionStrings__DefaultConnection chưa được cấu hình. " +
    "Đặt trong .env hoặc appsettings.json.");

var services = builder.Services;

services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connStr));

services.Configure<GoogleCloudOptions>(builder.Configuration.GetSection("GoogleCloud"));
services.Configure<S3StorageSettings>(builder.Configuration.GetSection(S3StorageSettings.SectionName));
services.Configure<McpAuthOptions>(builder.Configuration.GetSection(McpAuthOptions.SectionName));

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

services.AddScoped<IStorageService, S3StorageService>();
services.AddScoped<IImageVisibilityService, ImageVisibilityService>();
services.AddScoped<IAdminDashboardService, AdminDashboardService>();
services.AddScoped<IAdminProductService, AdminProductService>();
services.AddScoped<IAdminCategoryService, AdminCategoryService>();
services.AddScoped<IAdminUserService, AdminUserService>();
services.AddScoped<IAdminRoleService, AdminRoleService>();

services.AddAuthentication(McpPolicies.Scheme)
  .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(McpPolicies.Scheme, _ => { });
services.AddAuthorization(options =>
{
  static void RequireScope(AuthorizationPolicyBuilder policy, string scope) =>
    policy.AddAuthenticationSchemes(McpPolicies.Scheme)
      .RequireAuthenticatedUser()
      .RequireClaim(McpPolicies.ScopeClaim, scope);

  options.AddPolicy(McpPolicies.Read, policy => RequireScope(policy, McpPolicies.ReadScope));
  options.AddPolicy(McpPolicies.Write, policy => RequireScope(policy, McpPolicies.WriteScope));
  options.AddPolicy(McpPolicies.Users, policy => RequireScope(policy, McpPolicies.UsersScope));
  options.AddPolicy(McpPolicies.Roles, policy => RequireScope(policy, McpPolicies.RolesScope));
});

services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
  options.AddFixedWindowLimiter("mcp", limiterOptions =>
  {
    limiterOptions.PermitLimit = 120;
    limiterOptions.Window = TimeSpan.FromMinutes(1);
    limiterOptions.QueueLimit = 0;
  });
});

services.AddMcpServer()
  .WithHttpTransport(options => options.Stateless = true)
  .AddAuthorizationFilters()
  .WithRequestFilters(filters =>
  {
    filters.AddCallToolFilter(next => async (context, cancellationToken) =>
    {
      var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("McpAudit");
      var keyId = context.User?.Identity?.Name ?? "anonymous";
      var started = TimeProvider.System.GetTimestamp();
      try
      {
        var result = await next(context, cancellationToken);
        logger?.LogInformation(
          "MCP tool call by {KeyId} completed in {ElapsedMs} ms",
          keyId,
          TimeProvider.System.GetElapsedTime(started).TotalMilliseconds);
        return result;
      }
      catch (Exception ex)
      {
        logger?.LogWarning(
          ex,
          "MCP tool call by {KeyId} failed in {ElapsedMs} ms",
          keyId,
          TimeProvider.System.GetElapsedTime(started).TotalMilliseconds);
        throw;
      }
    });
  })
  .WithToolsFromAssembly();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
  app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapMcp().RequireAuthorization(McpPolicies.Read).RequireRateLimiting("mcp");

await app.RunAsync();
