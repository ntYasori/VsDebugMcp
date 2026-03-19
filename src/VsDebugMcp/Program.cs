using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        sp.GetRequiredService<ILogger<VsDebuggerService>>()));

// Configure MCP server with stdio transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<VsDebugMcp.Tools.SessionTools>()
    .WithTools<VsDebugMcp.Tools.ExecutionTools>()
    .WithTools<VsDebugMcp.Tools.BreakpointTools>()
    .WithTools<VsDebugMcp.Tools.InspectionTools>()
    .WithResources<VsDebugMcp.Resources.DebugResources>();

var app = builder.Build();

// Try to establish COM connection; if no VS is running, start anyway
// so the user can connect later via list_vs_instances + switch_vs_instance
var connector = app.Services.GetRequiredService<DteConnector>();
try
{
    await connector.ConnectAsync();
}
catch (InvalidOperationException)
{
    // VS not running or not accessible — MCP server starts anyway.
    // Tools will report connection errors; user can switch/connect later.
}

await app.RunAsync();
