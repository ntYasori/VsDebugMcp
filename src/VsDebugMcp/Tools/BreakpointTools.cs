using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class BreakpointTools
{
    [McpServerTool(Name = "add_breakpoint"), Description("Add a breakpoint at the specified file and line number. Optionally set a condition expression.")]
    public static async Task<string> AddBreakpoint(
        IVsDebuggerService debugger,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) where the breakpoint should be set.")] int line,
        [Description("Optional condition expression for a conditional breakpoint.")] string? condition = null)
    {
        return await debugger.AddBreakpointAsync(filePath, line, condition);
    }

    [McpServerTool(Name = "add_breakpoints_batch"), Description("Add multiple breakpoints at once in a single operation. More efficient than calling add_breakpoint multiple times. Each breakpoint can have its own optional condition.")]
    public static async Task<string> AddBreakpointsBatch(
        IVsDebuggerService debugger,
        [Description("Array of breakpoints to add. Each item must have 'FilePath' (string) and 'Line' (int), and optionally 'Condition' (string).")] BreakpointRequest[] breakpoints)
    {
        return await debugger.AddBreakpointsBatchAsync(breakpoints);
    }

    [McpServerTool(Name = "remove_breakpoint"), Description("Remove a breakpoint at the specified file and line number.")]
    public static async Task<string> RemoveBreakpoint(
        IVsDebuggerService debugger,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) of the breakpoint to remove.")] int line)
    {
        return await debugger.RemoveBreakpointAsync(filePath, line);
    }

    [McpServerTool(Name = "toggle_breakpoint"), Description("Enable or disable a breakpoint without removing it. Useful to temporarily skip a breakpoint.")]
    public static async Task<string> ToggleBreakpoint(
        IVsDebuggerService debugger,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) of the breakpoint to toggle.")] int line)
    {
        return await debugger.ToggleBreakpointAsync(filePath, line);
    }

    [McpServerTool(Name = "clear_all_breakpoints"), Description("Remove ALL breakpoints from the current debugging session. This action cannot be undone.")]
    public static async Task<string> ClearAllBreakpoints(
        McpServer server,
        IVsDebuggerService debugger,
        CancellationToken cancellationToken = default)
    {
        if (server?.ClientCapabilities?.Elicitation is not null)
        {
            try
            {
                var result = await server.ElicitAsync(new ElicitRequestParams
                {
                    Message = "This will remove ALL breakpoints. Are you sure?",
                    RequestedSchema = new ElicitRequestParams.RequestSchema
                    {
                        Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                        {
                            ["confirm"] = new ElicitRequestParams.BooleanSchema
                            {
                                Description = "Remove all breakpoints?",
                                Default = false
                            }
                        }
                    }
                }, cancellationToken);

                if (result.Action != "accept" || result.Content?["confirm"].ValueKind != JsonValueKind.True)
                    return "Clear all breakpoints cancelled by user.";
            }
            catch { /* Elicitation not available, proceed */ }
        }

        return await debugger.ClearAllBreakpointsAsync();
    }

    [McpServerTool(Name = "list_breakpoints"), Description("List all breakpoints currently set in the debugging session.")]
    public static async Task<string> ListBreakpoints(IVsDebuggerService debugger)
    {
        return await debugger.ListBreakpointsAsync();
    }
}
