using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AoDaiNhaUyen.Infrastructure.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
  public AppDbContext CreateDbContext(string[] args)
  {
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    var apiPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "AoDaiNhaUyen.Api"));

    var configuration = new ConfigurationBuilder()
      .SetBasePath(apiPath)
      .AddJsonFile("appsettings.json", optional: true)
      .AddJsonFile($"appsettings.{environment}.json", optional: true)
      .AddEnvironmentVariables()
      .Build();

    var connectionString =
      Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
      Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING") ??
      configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "CHANGE_ME")
    {
      throw new InvalidOperationException("Database connection string was not configured.");
    }

    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
    optionsBuilder.UseNpgsql(connectionString);

    return new AppDbContext(optionsBuilder.Options);
  }
}
