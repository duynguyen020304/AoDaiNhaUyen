using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// All logs go to stderr to keep stdout clean for JSON-RPC messages
builder.Logging.AddConsole(options =>
  options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
  .AddMcpServer()
  .WithStdioServerTransport()
  .WithToolsFromAssembly();

await builder.Build().RunAsync();
