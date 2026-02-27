using ModelContextProtocol.Server;
using System.ComponentModel;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class BreakpointTools
{
    [McpServerTool(Name = "add_breakpoint"), Description("Add a breakpoint at the specified file and line number. Optionally set a condition.")]
    public static async Task<string> AddBreakpoint(
        IVsDebuggerService debugger,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) where the breakpoint should be set.")] int line,
        [Description("Optional condition expression for a conditional breakpoint.")] string? condition = null)
    {
        return await debugger.AddBreakpointAsync(filePath, line, condition);
    }

    [McpServerTool(Name = "remove_breakpoint"), Description("Remove a breakpoint at the specified file and line number.")]
    public static async Task<string> RemoveBreakpoint(
        IVsDebuggerService debugger,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) of the breakpoint to remove.")] int line)
    {
        return await debugger.RemoveBreakpointAsync(filePath, line);
    }

    [McpServerTool(Name = "clear_all_breakpoints"), Description("Remove all breakpoints from the current debugging session.")]
    public static async Task<string> ClearAllBreakpoints(IVsDebuggerService debugger)
    {
        return await debugger.ClearAllBreakpointsAsync();
    }

    [McpServerTool(Name = "list_breakpoints"), Description("List all breakpoints currently set in the debugging session.")]
    public static async Task<string> ListBreakpoints(IVsDebuggerService debugger)
    {
        return await debugger.ListBreakpointsAsync();
    }
}
