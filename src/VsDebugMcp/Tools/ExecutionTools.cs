using ModelContextProtocol.Server;
using System.ComponentModel;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class ExecutionTools
{
    [McpServerTool(Name = "step_over"), Description("Step over the current line of code (execute it without stepping into called functions).")]
    public static async Task<string> StepOver(IVsDebuggerService debugger)
    {
        return await debugger.StepOverAsync();
    }

    [McpServerTool(Name = "step_into"), Description("Step into the current line of code (enter the called function).")]
    public static async Task<string> StepInto(IVsDebuggerService debugger)
    {
        return await debugger.StepIntoAsync();
    }

    [McpServerTool(Name = "step_out"), Description("Step out of the current function (continue until the current function returns).")]
    public static async Task<string> StepOut(IVsDebuggerService debugger)
    {
        return await debugger.StepOutAsync();
    }

    [McpServerTool(Name = "continue_execution"), Description("Continue execution until the next breakpoint or program end.")]
    public static async Task<string> ContinueExecution(IVsDebuggerService debugger)
    {
        return await debugger.ContinueExecutionAsync();
    }
}
