using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Api.Configuration;

public static class ServiceRegistration
{
  public static IServiceCollection AddBackendServices(this IServiceCollection services, IConfiguration configuration)
  {
    var configuredConnection = configuration.GetConnectionString("DefaultConnection");
    var envConnection =
      Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
      Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING");

    var connectionString = !string.IsNullOrWhiteSpace(envConnection)
      ? envConnection
      : configuredConnection;

    if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "CHANGE_ME")
    {
      throw new InvalidOperationException("Database connection string was not configured.");
    }

    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

    services.AddScoped<ICategoryRepository, CategoryRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();

    services.AddScoped<ICatalogService, CatalogService>();
    services.AddScoped<ISeedDataService, SeedDataService>();

    return services;
  }
}
