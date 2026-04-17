using System.Security.Claims;
using System.Text;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Application.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Repositories;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AoDaiNhaUyen.Api.Configuration;

public static class ServiceRegistration
{
  public static IServiceCollection AddBackendServices(this IServiceCollection services, IConfiguration configuration)
  {
    var configuredConnection = configuration.GetConnectionString("DefaultConnection");
    var envConnection =
      Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    var connectionString = !string.IsNullOrWhiteSpace(envConnection)
      ? envConnection
      : configuredConnection;

    if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "CHANGE_ME")
    {
      throw new InvalidOperationException("Database connection string was not configured.");
    }

    services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
    services
      .AddOptions<GoogleOAuthSettings>()
      .Bind(configuration.GetSection("GoogleOAuth"))
      .ValidateDataAnnotations()
      .Validate(
        settings => Uri.TryCreate(settings.RedirectUri, UriKind.Absolute, out _),
        "GoogleOAuth:RedirectUri must be a valid absolute URI.")
      .ValidateOnStart();
    services.Configure<CookieSettings>(configuration.GetSection("CookieSettings"));

    var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
    if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    {
      throw new InvalidOperationException("JwtSettings:SecretKey was not configured.");
    }

    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    services.AddHttpClient();

    services
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwtSettings.Issuer,
          ValidAudience = jwtSettings.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
          NameClaimType = ClaimTypes.Name,
          RoleClaimType = ClaimTypes.Role,
          ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
          OnMessageReceived = context =>
          {
            var cookieName = configuration.GetValue<string>("CookieSettings:AccessTokenCookieName") ?? "access_token";
            if (context.Request.Cookies.TryGetValue(cookieName, out var accessToken))
            {
              context.Token = accessToken;
            }

            return Task.CompletedTask;
          }
        };
      });

    services.AddScoped<ICategoryRepository, CategoryRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();

    services.AddScoped<ICatalogService, CatalogService>();
    services.AddScoped<ISeedDataService, SeedDataService>();
    services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
    services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
    services.AddScoped<IAuthService, AuthService>();
    services.Configure<GoogleCloudOptions>(configuration.GetSection("GoogleCloud"));
    services.AddHttpClient<IAiTryOnService, VertexAiTryOnService>(httpClient =>
    {
      httpClient.Timeout = Timeout.InfiniteTimeSpan;
    });

    return services;
  }
}
