using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VsDebugMcp;
using VsDebugMcp.Debugger;
using VsDebugMcp.Interop;

// Parse --vs-pid from command line
int? vsPid = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--vs-pid" && int.TryParse(args[i + 1], out var pid))
    {
        vsPid = pid;
        break;
    }
}

var builder = Host.CreateApplicationBuilder(args);

// Register core services
builder.Services.AddSingleton<ComThread>();
builder.Services.AddSingleton(sp => new DteConnector(sp.GetRequiredService<ComThread>(), vsPid));
builder.Services.AddSingleton<IVsDebuggerService>(sp =>
    new VsDebuggerService(
        sp.GetRequiredService<DteConnector>(),
        sp.GetRequiredService<ComThread>()));

// Configure MCP server with stdio transport
// Use generic registration to be trim-compatible
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<VsDebugMcp.Tools.SessionTools>()
    .WithTools<VsDebugMcp.Tools.ExecutionTools>()
    .WithTools<VsDebugMcp.Tools.BreakpointTools>()
    .WithTools<VsDebugMcp.Tools.InspectionTools>()
    .WithResources<VsDebugMcp.Resources.DebugResources>();

var app = builder.Build();

// Ensure COM connection is established
var connector = app.Services.GetRequiredService<DteConnector>();
await connector.ConnectAsync();

await app.RunAsync();
