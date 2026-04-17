using AoDaiNhaUyen.Api.Configuration;
using AoDaiNhaUyen.Api.Middleware;
using AoDaiNhaUyen.Application.Interfaces;
using DotNetEnv;

var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddBackendServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

if (app.Configuration.GetValue<bool>("RunMigrationsAndSeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var seedDataService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
    await seedDataService.SeedAllAsync();
}

app.Run();
