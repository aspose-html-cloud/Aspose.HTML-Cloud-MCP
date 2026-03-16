using Aspose.HTML.Cloud.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ── Aspose.HTML Cloud MCP Server ─────────────────────────────────────────────
// A stdio-based Model Context Protocol server that exposes Aspose.HTML Cloud
// document conversion as MCP tools. Designed to be launched by MCP clients
// such as Claude Desktop, Cursor, or VS Code.

var builder = Host.CreateApplicationBuilder(args);

// Suppress all framework logs — must not write to stdout (corrupts JSON-RPC stream)
builder.Logging.ClearProviders();
builder.Logging.AddFilter("Microsoft", LogLevel.None);
builder.Logging.AddFilter("System", LogLevel.None);
builder.Logging.AddFilter("Aspose.HTML.Cloud.Mcp", LogLevel.Information);
// stderr is safe — visible in host app logs
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<HttpClient>(_ => new HttpClient
{
    Timeout = TimeSpan.FromMinutes(5)
});
builder.Services.AddSingleton<AsposeHtmlAuth>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
        {
            Name    = "aspose-html-cloud",
            Version = "1.0.0"
        };
    })
    .WithStdioServerTransport()
    .WithTools<ConversionTools>();

await builder.Build().RunAsync();
