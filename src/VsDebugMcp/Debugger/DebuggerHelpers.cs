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
    /// Formats a source code snippet around the current line with line numbers and an arrow marker.
    /// Returns null if source is not available (external code, no active document).
    /// </summary>
    internal static string? FormatSourceSnippet(DTE dte, int contextLines)
    {
        try
        {
            var doc = dte.ActiveDocument;
            if (doc is null) return null;

            var line = (doc.Selection is TextSelection sel) ? sel.CurrentLine : 0;
            if (line <= 0) return null;

            var textDoc = (TextDocument)doc.Object("TextDocument");
            var totalLines = textDoc.EndPoint.Line;
            var startLine = Math.Max(1, line - contextLines);
            var endLine = Math.Min(totalLines + 1, line + contextLines + 1);

            var editPoint = textDoc.StartPoint.CreateEditPoint();
            var text = editPoint.GetLines(startLine, endLine);
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var lineCount = endLine - startLine;

            var sb = new StringBuilder();
            var gutterWidth = (endLine - 1).ToString().Length;
            for (int i = 0; i < Math.Min(lines.Length, lineCount); i++)
            {
                var lineNum = startLine + i;
                var marker = lineNum == line ? "\u2192" : " ";
                sb.AppendLine($"{marker} {lineNum.ToString().PadLeft(gutterWidth)}| {lines[i]}");
            }
            return sb.ToString().TrimEnd();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    /// <summary>
    /// Formats a rich step result with location, source snippet, and local variables.
    /// </summary>
    internal static string FormatStepResult(DTE dte, DebuggerOptions options)
    {
        var sb = new StringBuilder();

        try
        {
            var frame = dte.Debugger.CurrentStackFrame;
            var doc = dte.ActiveDocument;
            var line = (doc?.Selection is TextSelection sel) ? sel.CurrentLine : 0;
            var filePath = doc?.FullName ?? "unknown";
            var funcName = frame?.FunctionName ?? "unknown";

            sb.AppendLine($"Stepped to: {filePath}:{line} in {funcName}");

            // Source context
            var snippet = FormatSourceSnippet(dte, options.StepContextLines);
            if (snippet is not null)
            {
                sb.AppendLine();
                sb.AppendLine(snippet);
            }

            // Locals summary
            if (frame is not null && options.StepMaxLocals > 0)
            {
                try
                {
                    var locals = frame.Locals;
                    if (locals.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Locals:");
                        int shown = 0;
                        foreach (Expression expr in locals)
                        {
                            if (shown >= options.StepMaxLocals)
                            {
                                sb.AppendLine($"  ... and {locals.Count - shown} more");
                                break;
                            }
                            sb.AppendLine($"  {expr.Name} = {expr.Value} ({expr.Type})");
                            shown++;
                        }
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Locals not available — skip
                }
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return "Step completed.";
        }

        return sb.ToString().TrimEnd();
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
