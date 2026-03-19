using System.Text;
using EnvDTE;

namespace VsDebugMcp.Debugger;

/// <summary>
/// Shared helper methods used across debug service implementations.
/// </summary>
internal static class DebuggerHelpers
{
    /// <summary>
    /// Returns an error message if the debugger is not in break mode, null otherwise.
    /// </summary>
    internal static string? RequireBreakMode(DTE dte, string operation) =>
        dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode
            ? $"Cannot {operation}: debugger is not in break mode."
            : null;

    /// <summary>
    /// Formats the current execution location after a step operation.
    /// </summary>
    internal static string FormatCurrentLocation(DTE dte)
    {
        try
        {
            var frame = dte.Debugger.CurrentStackFrame;
            var doc = dte.ActiveDocument;
            var line = (doc?.Selection is TextSelection sel) ? sel.CurrentLine : 0;
            return $"Now at: {doc?.FullName ?? "unknown"}:{line} in {frame?.FunctionName ?? "unknown"}";
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return "Step completed.";
        }
    }

    /// <summary>
    /// Recursively formats expression trees (variables, arguments) into a StringBuilder.
    /// </summary>
    internal static void FormatExpressions(Expressions expressions, StringBuilder sb, int indent, int maxDepth)
    {
        var prefix = new string(' ', indent * 2);
        foreach (Expression expr in expressions)
        {
            sb.AppendLine($"{prefix}- {expr.Name} = {expr.Value} ({expr.Type})");
            if (indent < maxDepth && expr.DataMembers.Count > 0)
            {
                FormatExpressions(expr.DataMembers, sb, indent + 1, maxDepth);
            }
        }
    }
}
