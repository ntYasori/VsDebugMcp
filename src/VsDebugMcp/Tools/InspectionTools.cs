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
}
