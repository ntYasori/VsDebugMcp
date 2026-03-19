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
        IBreakpointDebugService breakpoints,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) where the breakpoint should be set.")] int line,
        [Description("Optional condition expression for a conditional breakpoint.")] string? condition = null)
    {
        return await breakpoints.AddBreakpointAsync(filePath, line, condition);
    }

    [McpServerTool(Name = "add_breakpoints_batch"), Description("Add multiple breakpoints at once in a single operation. More efficient than calling add_breakpoint multiple times. Each breakpoint can have its own optional condition.")]
    public static async Task<string> AddBreakpointsBatch(
        IBreakpointDebugService breakpoints,
        [Description("Array of breakpoints to add. Each item must have 'FilePath' (string) and 'Line' (int), and optionally 'Condition' (string).")] BreakpointRequest[] bps)
    {
        return await breakpoints.AddBreakpointsBatchAsync(bps);
    }

    [McpServerTool(Name = "remove_breakpoint"), Description("Remove a breakpoint at the specified file and line number.")]
    public static async Task<string> RemoveBreakpoint(
        IBreakpointDebugService breakpoints,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) of the breakpoint to remove.")] int line)
    {
        return await breakpoints.RemoveBreakpointAsync(filePath, line);
    }

    [McpServerTool(Name = "toggle_breakpoint"), Description("Enable or disable a breakpoint without removing it. Useful to temporarily skip a breakpoint.")]
    public static async Task<string> ToggleBreakpoint(
        IBreakpointDebugService breakpoints,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) of the breakpoint to toggle.")] int line)
    {
        return await breakpoints.ToggleBreakpointAsync(filePath, line);
    }

    [McpServerTool(Name = "clear_all_breakpoints"), Description("Remove ALL breakpoints from the current debugging session. This action cannot be undone.")]
    public static async Task<string> ClearAllBreakpoints(
        McpServer server,
        IBreakpointDebugService breakpoints,
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

        return await breakpoints.ClearAllBreakpointsAsync();
    }

    [McpServerTool(Name = "list_breakpoints"), Description("List all breakpoints currently set in the debugging session.")]
    public static async Task<string> ListBreakpoints(IBreakpointDebugService breakpoints)
    {
        return await breakpoints.ListBreakpointsAsync();
    }
}
