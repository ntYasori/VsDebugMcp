using ModelContextProtocol.Server;
using System.ComponentModel;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class InspectionTools
{
    [McpServerTool(Name = "get_variables_values"), Description("Get the values of all local variables and arguments in the current stack frame.")]
    public static async Task<string> GetVariablesValues(
        IVsDebuggerService debugger,
        [Description("Maximum depth for expanding complex objects (default: 1).")] int? depth = null)
    {
        return await debugger.GetVariablesAsync(depth);
    }

    [McpServerTool(Name = "evaluate_expression"), Description("Evaluate an expression in the context of the current stack frame.")]
    public static async Task<string> EvaluateExpression(
        IVsDebuggerService debugger,
        [Description("The expression to evaluate (e.g. 'myVariable.Count', 'x + y', 'DateTime.Now').")] string expression)
    {
        return await debugger.EvaluateExpressionAsync(expression);
    }

    [McpServerTool(Name = "get_call_stack"), Description("Get the current call stack showing all stack frames of the active thread.")]
    public static async Task<string> GetCallStack(IVsDebuggerService debugger)
    {
        return await debugger.GetCallStackAsync();
    }

    [McpServerTool(Name = "evaluate_multiple"), Description("Evaluate multiple expressions at once in the current stack frame. More efficient than calling evaluate_expression multiple times.")]
    public static async Task<string> EvaluateMultiple(
        IVsDebuggerService debugger,
        [Description("Array of expressions to evaluate (e.g. ['myVar', 'x + y', 'obj.Property']).")] string[] expressions)
    {
        return await debugger.EvaluateMultipleExpressionsAsync(expressions);
    }

    [McpServerTool(Name = "get_current_location"), Description("Get the current execution location including file path, line number, function name, and module.")]
    public static async Task<string> GetCurrentLocation(IVsDebuggerService debugger)
    {
        return await debugger.GetCurrentLocationAsync();
    }

    [McpServerTool(Name = "manage_watch"), Description("Manage persistent watch expressions. Add/remove watches that persist across debug steps. Use 'list' to evaluate all watches at once.")]
    public static async Task<string> ManageWatch(
        IVsDebuggerService debugger,
        [Description("Action to perform: 'add', 'remove', or 'list'.")] string action,
        [Description("The expression to watch (required for 'add' and 'remove').")] string? expression = null)
    {
        return action.ToLowerInvariant() switch
        {
            "add" when expression is not null => await debugger.AddWatchAsync(expression),
            "remove" when expression is not null => await debugger.RemoveWatchAsync(expression),
            "list" => await debugger.ListWatchesAsync(),
            "add" or "remove" => "Expression is required for 'add' and 'remove' actions.",
            _ => "Invalid action. Use 'add', 'remove', or 'list'."
        };
    }
}
