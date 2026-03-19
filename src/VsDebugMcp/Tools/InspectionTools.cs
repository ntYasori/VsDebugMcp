using ModelContextProtocol.Server;
using System.ComponentModel;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class InspectionTools
{
    [McpServerTool(Name = "get_variables_values"), Description("Get the values of all local variables and arguments in the current stack frame.")]
    public static async Task<string> GetVariablesValues(
        IInspectionDebugService inspection,
        [Description("Maximum depth for expanding complex objects (default: 1, max: 5).")] int? depth = null)
    {
        return await inspection.GetVariablesAsync(depth);
    }

    [McpServerTool(Name = "evaluate_expression"), Description("Evaluate a SINGLE expression in the context of the current stack frame (e.g. 'myVariable.Count', 'x + y', 'DateTime.Now'). IMPORTANT: If you need to evaluate more than one expression, use the 'evaluate_multiple' tool instead — it evaluates all expressions in a single call, avoiding errors from parallel calls.")]
    public static async Task<string> EvaluateExpression(
        IInspectionDebugService inspection,
        [Description("The expression to evaluate (e.g. 'myVariable.Count', 'x + y', 'DateTime.Now').")] string expression)
    {
        return await inspection.EvaluateExpressionAsync(expression);
    }

    [McpServerTool(Name = "get_call_stack"), Description("Get the current call stack showing all stack frames of the active thread.")]
    public static async Task<string> GetCallStack(IInspectionDebugService inspection)
    {
        return await inspection.GetCallStackAsync();
    }

    [McpServerTool(Name = "evaluate_multiple"), Description("Evaluate multiple expressions at once in the current stack frame. ALWAYS prefer this tool over calling evaluate_expression multiple times — it is more reliable (avoids errors from parallel calls) and more efficient (single round-trip). Errors in individual expressions do not affect the others.")]
    public static async Task<string> EvaluateMultiple(
        IInspectionDebugService inspection,
        [Description("Array of expressions to evaluate (e.g. ['myVar', 'x + y', 'obj.Property']).")] string[] expressions)
    {
        return await inspection.EvaluateMultipleExpressionsAsync(expressions);
    }

    [McpServerTool(Name = "get_current_location"), Description("Get the current execution location including file path, line number, function name, and module.")]
    public static async Task<string> GetCurrentLocation(IInspectionDebugService inspection)
    {
        return await inspection.GetCurrentLocationAsync();
    }

    [McpServerTool(Name = "get_exception_info"), Description("Get details about the current exception being debugged, including type, message, stack trace, and inner exception. Only works when stopped at an exception or in a catch block.")]
    public static async Task<string> GetExceptionInfo(IInspectionDebugService inspection)
    {
        return await inspection.GetExceptionInfoAsync();
    }

    [McpServerTool(Name = "get_threads"), Description("List all threads in the current debug process, showing thread ID, name, and state. Marks the current active thread.")]
    public static async Task<string> GetThreads(IInspectionDebugService inspection)
    {
        return await inspection.GetThreadsAsync();
    }

    [McpServerTool(Name = "switch_stack_frame"), Description("Switch to a different stack frame to inspect variables and state at that level. Use get_call_stack first to see available frames and their indices.")]
    public static async Task<string> SwitchStackFrame(
        IInspectionDebugService inspection,
        [Description("Zero-based index of the stack frame to switch to (0 = top/current frame).")] int frameIndex)
    {
        return await inspection.SwitchStackFrameAsync(frameIndex);
    }

    [McpServerTool(Name = "get_output"), Description("Read the Visual Studio Debug Output window contents. Shows debug logs, console output, and diagnostic messages. Limited to the last 100 lines.")]
    public static async Task<string> GetOutput(IInspectionDebugService inspection)
    {
        return await inspection.GetOutputAsync();
    }

    [McpServerTool(Name = "add_watch"), Description("Add a persistent watch expression that will be evaluated on each debug step. Use list_watches to see current values.")]
    public static async Task<string> AddWatch(
        IInspectionDebugService inspection,
        [Description("The expression to watch (e.g. 'myVar.Count', 'x + y').")] string expression)
    {
        return await inspection.AddWatchAsync(expression);
    }

    [McpServerTool(Name = "remove_watch"), Description("Remove a previously added watch expression.")]
    public static async Task<string> RemoveWatch(
        IInspectionDebugService inspection,
        [Description("The watch expression to remove.")] string expression)
    {
        return await inspection.RemoveWatchAsync(expression);
    }

    [McpServerTool(Name = "list_watches"), Description("List all watch expressions and their current values. If in break mode, evaluates each expression and shows the result.")]
    public static async Task<string> ListWatches(IInspectionDebugService inspection)
    {
        return await inspection.ListWatchesAsync();
    }
}
