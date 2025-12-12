using Komet.MCP.DynamicsAX2012.Core.Models;
using Komet.MCP.DynamicsAX2012.Server.Services;
using Komet.MCP.DynamicsAX2012.Server.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Configure AX settings from environment or defaults
builder.Services.Configure<AXConfiguration>(config =>
{
    config.EndpointUrl = Environment.GetEnvironmentVariable("AX_ENDPOINT_URL")
        ?? "net.tcp://it-test-erp3cu.brasseler.biz:8201/DynamicsAx/Services/MCPServices";
    config.Upn = Environment.GetEnvironmentVariable("AX_UPN")
        ?? "Adminsysmgmt@brasseler.biz";
    config.DefaultCompany = Environment.GetEnvironmentVariable("AX_DEFAULT_COMPANY")
        ?? "GBL";
    config.TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("AX_TIMEOUT_SECONDS"), out var timeout)
        ? timeout : 30;
});

// Register AIF/WCF service
builder.Services.AddSingleton<AXConnectionService>();

// Configure BC Proxy settings from environment or defaults
builder.Services.Configure<BCProxyConfiguration>(config =>
{
    config.BaseUrl = Environment.GetEnvironmentVariable("BC_PROXY_URL")
        ?? "http://localhost:5100";
    config.TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("BC_PROXY_TIMEOUT"), out var timeout)
        ? timeout : 30;
});

// Register BC Proxy service
builder.Services.AddSingleton<BCProxyService>();

// Configure Backend selection from environment (AIF, BC, or AUTO)
builder.Services.Configure<AXBackendConfiguration>(config =>
{
    var backendEnv = Environment.GetEnvironmentVariable("AX_BACKEND")?.ToUpperInvariant() ?? "AIF";
    config.Mode = backendEnv switch
    {
        "BC" => AXBackendMode.BC,
        "AUTO" => AXBackendMode.AUTO,
        _ => AXBackendMode.AIF
    };
});

// Register unified backend service
builder.Services.AddSingleton<AXBackendService>();

// Register MCP Server with tools
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(CustomerTools).Assembly);

var app = builder.Build();

// Log startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Komet.MCP.DynamicsAX2012 Server starting...");
logger.LogInformation("Endpoint: {Endpoint}", Environment.GetEnvironmentVariable("AX_ENDPOINT_URL") ?? "net.tcp://it-test-erp3cu.brasseler.biz:8201/DynamicsAx/Services/MCPServices");
logger.LogInformation("Default Company: {Company}", Environment.GetEnvironmentVariable("AX_DEFAULT_COMPANY") ?? "GBL");
logger.LogInformation("BC Proxy URL: {BCProxyUrl}", Environment.GetEnvironmentVariable("BC_PROXY_URL") ?? "http://localhost:5100");
logger.LogInformation("Backend Mode: {BackendMode}", Environment.GetEnvironmentVariable("AX_BACKEND") ?? "AIF");

await app.RunAsync();
