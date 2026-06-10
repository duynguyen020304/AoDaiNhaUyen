using Amazon;
using Amazon.S3;
using System.Security.Claims;
using System.Text;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Application.Services;
using AoDaiNhaUyen.Domain.Constants;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Repositories;
using AoDaiNhaUyen.Infrastructure.Services;
using AoDaiNhaUyen.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;

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

    services
      .AddOptions<JwtSettings>()
      .Bind(configuration.GetSection("JwtSettings"))
      .Validate(settings => ValidateJwtSettings(settings, out _), "JwtSettings không hợp lệ.")
      .ValidateOnStart();
    services
      .AddOptions<EmailSettings>()
      .Bind(configuration.GetSection("EmailSettings"))
      .ValidateDataAnnotations()
      .Validate(
        settings => Uri.TryCreate(settings.ApiBaseUrl, UriKind.Absolute, out _),
        "EmailSettings:ApiBaseUrl must be a valid absolute URI.")
      .Validate(
        settings => Uri.TryCreate(settings.FrontendBaseUrl, UriKind.Absolute, out _),
        "EmailSettings:FrontendBaseUrl must be a valid absolute URI.")
      .ValidateOnStart();
    services
      .AddOptions<GoogleOAuthSettings>()
      .Bind(configuration.GetSection("GoogleOAuth"))
      .ValidateDataAnnotations()
      .Validate(
        settings => Uri.TryCreate(settings.RedirectUri, UriKind.Absolute, out _),
        "GoogleOAuth:RedirectUri must be a valid absolute URI.")
      .ValidateOnStart();
    services
      .AddOptions<ZaloOAuthSettings>()
      .Bind(configuration.GetSection("ZaloOAuth"))
      .ValidateDataAnnotations()
      .Validate(
        settings => Uri.TryCreate(settings.RedirectUri, UriKind.Absolute, out _),
        "ZaloOAuth:RedirectUri must be a valid absolute URI.")
      .ValidateOnStart();
    services.Configure<CookieSettings>(configuration.GetSection("CookieSettings"));

    var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
    if (!ValidateJwtSettings(jwtSettings, out var jwtError))
    {
      throw new InvalidOperationException(jwtError);
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

    services.AddAuthorizationBuilder()
      .AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole(RoleNames.Admin))
      .AddPolicy("RequireCustomerRole", policy =>
        policy.RequireRole(RoleNames.Customer))
      .AddPolicy("RequireAdminOrCustomer", policy =>
        policy.RequireRole(RoleNames.Admin, RoleNames.Customer));

    services.AddScoped<ICategoryRepository, CategoryRepository>();
    services.AddScoped<ICartRepository, CartRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IUserProfileRepository, UserProfileRepository>();
    services.AddScoped<ICommentRepository, CommentRepository>();

    services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();
    services.AddScoped<IBlogPostRepository, BlogPostRepository>();
    services.AddScoped<ICatalogService, CatalogService>();
    services.AddScoped<IBlogCategoryService, BlogCategoryService>();
    services.AddScoped<IBlogPostService, BlogPostService>();
    services.AddScoped<ICommentService, CommentService>();
    services.AddScoped<IAdminProductService, AdminProductService>();
    services.AddScoped<IAdminUserService, AdminUserService>();
    services.AddScoped<IAdminRoleService, AdminRoleService>();
    services.AddScoped<IAdminCategoryService, AdminCategoryService>();
    services.AddScoped<ICartService, CartService>();
    services.AddScoped<ICheckoutService, CheckoutService>();
    services.AddScoped<IPromoService, PromoService>();
    services.AddScoped<IStockService, StockService>();
    services.AddScoped<IOrderAttributionService, OrderAttributionService>();
    services.AddScoped<ICustomerEventService, CustomerEventService>();
    services.AddScoped<IEmailQueueService, EmailQueueService>();
    services.AddScoped<IEmailTemplateService, EmailTemplateService>();
    services.AddScoped<ISubscriberService, SubscriberService>();
    services.AddScoped<IMarketingConsentService, MarketingConsentService>();
    services.AddScoped<IPromoCostService, PromoCostService>();
    services.AddHostedService<BackgroundEmailWorker>();
    services.AddScoped<IAdminEmailTemplateService, AdminEmailTemplateService>();
    services.AddScoped<IAdminSubscriberService, AdminSubscriberService>();
    services.AddScoped<IAdminEmailJobService, AdminEmailJobService>();
    services.AddScoped<IAdminMarketingStatsService, AdminMarketingStatsService>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<ISeedDataService, SeedDataService>();



    // S3 Storage

    services.Configure<S3StorageSettings>(configuration.GetSection(S3StorageSettings.SectionName));

    services.AddSingleton<IAmazonS3>(sp =>

    {

      var s3Settings = sp.GetRequiredService<IOptions<S3StorageSettings>>().Value;

      var config = new AmazonS3Config { ForcePathStyle = s3Settings.UsePathStyle };



      if (!string.IsNullOrWhiteSpace(s3Settings.ServiceUrl))

        config.ServiceURL = s3Settings.ServiceUrl;

      else if (!string.IsNullOrWhiteSpace(s3Settings.Region))

        config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3Settings.Region);



      if (!string.IsNullOrWhiteSpace(s3Settings.AccessKey) && !string.IsNullOrWhiteSpace(s3Settings.SecretKey))

        return new AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, config);



      return new AmazonS3Client(config);

    });

    services.AddScoped<IStorageService, S3StorageService>();
    services.AddScoped<IAdminMediaService, AdminMediaService>();
    services.AddScoped<IAdminDashboardService, AdminDashboardService>();
    services.AddScoped<IAdminOrderService, AdminOrderService>();
    services.AddScoped<IAdminInventoryService, AdminInventoryService>();
    services.AddScoped<IAdminToolRiskService, AdminToolRiskService>();
    services.AddScoped<IAdminReviewService, AdminReviewService>();
    services.AddScoped<IAdminPromoService, AdminPromoService>();
    services.AddScoped<IPromptRedactionService, PromptRedactionService>();
    services.AddScoped<ILlmAuditService, LlmAuditService>();
    services.AddScoped<ISafetyGate, SafetyGate>();
    services.AddSingleton<IAutoModeStore, AutoModeStore>();
    services.AddSingleton<IPendingActionStore, PendingActionStore>();
    services.AddSingleton<IConversationStore, ConversationStore>();
    services.AddScoped<IAdminAgentService, AdminAgentService>();
    services.AddHttpClient<IAdminLlmProvider, VertexAiAdminProvider>(httpClient =>
    {
      httpClient.Timeout = Timeout.InfiniteTimeSpan;
    });
    services.AddScoped<IImageVisibilityService, ImageVisibilityService>();
    services.AddScoped<IBlogImageVisibilityService, BlogImageVisibilityService>();
    services.AddSingleton<IImageUploadValidator, ImageUploadValidator>();
    services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
    services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<IEmailService, SmtpEmailService>();
    services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
    services.AddScoped<IZaloOAuthService, ZaloOAuthService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ICatalogStylingService, CatalogStylingService>();
    services.AddScoped<ICatalogTryOnService, CatalogTryOnService>();
    services.AddHttpClient<IIntentClassifier, IntentClassifier>(httpClient =>
    {
      httpClient.Timeout = Timeout.InfiniteTimeSpan;
    });
    services.AddScoped<IThreadMemoryService, ThreadMemoryService>();
    services.AddScoped<IStylistFallbackTextService, StylistFallbackTextService>();
    services.AddScoped<StylistChatService>();
    services.AddSingleton<IStylistChatService, ConcurrencyLimitedStylistChatService>();
    services.AddHttpClient<IStylistResponseComposer, VertexAiStylistResponseComposer>(httpClient =>
    {
      httpClient.Timeout = Timeout.InfiniteTimeSpan;
    });
    services
      .AddOptions<ChatConcurrencyOptions>()
      .Bind(configuration.GetSection(ChatConcurrencyOptions.SectionName))
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.Configure<GoogleCloudOptions>(configuration.GetSection("GoogleCloud"));
    services
      .AddOptions<AiTryOnConcurrencyOptions>()
      .Bind(configuration.GetSection(AiTryOnConcurrencyOptions.SectionName))
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services
      .AddOptions<ImageValidationOptions>()
      .Bind(configuration.GetSection(ImageValidationOptions.SectionName))
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddScoped<ICachedImageValidationService, CachedImageValidationService>();
    services.AddHttpClient<IImageValidationService, VertexAiImageValidationService>(httpClient =>
    {
      httpClient.Timeout = Timeout.InfiniteTimeSpan;
    });
    services.AddHttpClient<VertexAiTryOnService>(httpClient =>
    {
      httpClient.Timeout = Timeout.InfiniteTimeSpan;
    });
    services.AddSingleton<IAiTryOnService>(serviceProvider =>
      new ConcurrencyLimitedAiTryOnService(
        () => serviceProvider.GetRequiredService<VertexAiTryOnService>(),
        serviceProvider.GetRequiredService<IOptions<AiTryOnConcurrencyOptions>>(),
        serviceProvider.GetRequiredService<ILogger<ConcurrencyLimitedAiTryOnService>>()));

    return services;
  }

  private static bool ValidateJwtSettings(JwtSettings settings, out string? error)
  {
    if (string.IsNullOrWhiteSpace(settings.SecretKey))
    {
      error = "JwtSettings:SecretKey was not configured.";
      return false;
    }

    var secret = settings.SecretKey.Trim();
    if (secret.Length < 64 || Regex.IsMatch(secret, "CHANGE_ME|DEV_ONLY", RegexOptions.IgnoreCase))
    {
      error = "JwtSettings:SecretKey must be a strong non-placeholder secret with at least 64 characters.";
      return false;
    }

    if (string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience))
    {
      error = "JwtSettings:Issuer and Audience are required.";
      return false;
    }

    if (settings.ExpiryMinutes is < 5 or > 120)
    {
      error = "JwtSettings:ExpiryMinutes must be between 5 and 120.";
      return false;
    }

    if (settings.RefreshTokenExpiryDays is < 1 or > 60)
    {
      error = "JwtSettings:RefreshTokenExpiryDays must be between 1 and 60.";
      return false;
    }

    error = null;
    return true;
  }
}
