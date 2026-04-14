using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
builder.Logging.ClearProviders();

// Register configurable options (overridable via env vars)
builder.Services.Configure<DebuggerOptions>(opts =>
{
    if (int.TryParse(Environment.GetEnvironmentVariable("VSDEBUGMCP_EXPRESSION_TIMEOUT_MS"), out var et))
        opts.ExpressionTimeoutMs = et;
    if (int.TryParse(Environment.GetEnvironmentVariable("VSDEBUGMCP_COM_TIMEOUT_MS"), out var ct))
        opts.ComOperationTimeoutMs = ct;
    if (int.TryParse(Environment.GetEnvironmentVariable("VSDEBUGMCP_MAX_VARIABLE_DEPTH"), out var md))
        opts.MaxVariableDepth = md;
    if (int.TryParse(Environment.GetEnvironmentVariable("VSDEBUGMCP_MAX_OUTPUT_LINES"), out var ml))
        opts.MaxOutputLines = ml;
    if (int.TryParse(Environment.GetEnvironmentVariable("VSDEBUGMCP_RESTART_DELAY_MS"), out var rd))
        opts.RestartDelayMs = rd;
    if (int.TryParse(Environment.GetEnvironmentVariable("VSDEBUGMCP_STEP_CONTEXT_LINES"), out var sc))
        opts.StepContextLines = sc;
    if (int.TryParse(Environment.GetEnvironmentVariable("VSDEBUGMCP_STEP_MAX_LOCALS"), out var sl))
        opts.StepMaxLocals = sl;
});

// Register core services
builder.Services.AddSingleton<IRotHelper, RotHelper>();
builder.Services.AddSingleton<ComThread>();
builder.Services.AddSingleton(sp =>
    new DteConnector(
        sp.GetRequiredService<ComThread>(),
        sp.GetRequiredService<IRotHelper>(),
        sp.GetRequiredService<ILogger<DteConnector>>(),
        vsPid));

// Register debug services
builder.Services.AddSingleton<ISessionDebugService, SessionDebugService>();
builder.Services.AddSingleton<IBreakpointDebugService, BreakpointDebugService>();
builder.Services.AddSingleton<IExecutionDebugService, ExecutionDebugService>();
builder.Services.AddSingleton<IInspectionDebugService, InspectionDebugService>();
builder.Services.AddSingleton<IProcessDebugService, ProcessDebugService>();
builder.Services.AddSingleton<IExceptionDebugService, ExceptionDebugService>();
builder.Services.AddSingleton<INavigationDebugService, NavigationDebugService>();

// Configure MCP server with stdio transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<VsDebugMcp.Tools.SessionTools>()
    .WithTools<VsDebugMcp.Tools.ExecutionTools>()
    .WithTools<VsDebugMcp.Tools.BreakpointTools>()
    .WithTools<VsDebugMcp.Tools.InspectionTools>()
    .WithTools<VsDebugMcp.Tools.ProcessTools>()
    .WithTools<VsDebugMcp.Tools.ExceptionTools>()
    .WithTools<VsDebugMcp.Tools.NavigationTools>()
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
