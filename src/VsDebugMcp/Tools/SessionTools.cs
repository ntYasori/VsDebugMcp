using ModelContextProtocol.Server;
using System.ComponentModel;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class SessionTools
{
    [McpServerTool(Name = "start_debugging"), Description("Start debugging the current project in Visual Studio. Optionally specify a build configuration (e.g. 'Debug', 'Release').")]
    public static async Task<string> StartDebugging(
        IVsDebuggerService debugger,
        [Description("Build configuration name (e.g. 'Debug', 'Release'). Defaults to active configuration.")] string? configuration = null)
    {
        return await debugger.StartDebuggingAsync(configuration);
    }

    [McpServerTool(Name = "stop_debugging"), Description("Stop the current debugging session in Visual Studio.")]
    public static async Task<string> StopDebugging(IVsDebuggerService debugger)
    {
        return await debugger.StopDebuggingAsync();
    }

    [McpServerTool(Name = "restart_debugging"), Description("Restart the current debugging session (stop and start again).")]
    public static async Task<string> RestartDebugging(IVsDebuggerService debugger)
    {
        return await debugger.RestartDebuggingAsync();
    }
}
