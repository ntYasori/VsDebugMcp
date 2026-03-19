using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class InspectionDebugService : IInspectionDebugService
{
    private readonly DteConnector _connector;
    private readonly ILogger<InspectionDebugService> _logger;
    private readonly DebuggerOptions _options;
    private readonly List<string> _watchExpressions = new();
    private readonly object _watchLock = new();

    public InspectionDebugService(
        DteConnector connector,
        ILogger<InspectionDebugService> logger,
        IOptions<DebuggerOptions> options)
    {
        _connector = connector;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> GetVariablesAsync(int? depth = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "inspect variables");
            if (check is not null) return check;

            var frame = dte.Debugger.CurrentStackFrame;
            if (frame is null)
                return "No current stack frame available.";

            var sb = new StringBuilder();
            var maxDepth = Math.Min(depth ?? 1, _options.MaxVariableDepth);

            sb.AppendLine("**Locals:**");
            DebuggerHelpers.FormatExpressions(frame.Locals, sb, 0, maxDepth);

            sb.AppendLine("\n**Arguments:**");
            DebuggerHelpers.FormatExpressions(frame.Arguments, sb, 0, maxDepth);

            return sb.ToString();
        });
    }

    public async Task<string> EvaluateExpressionAsync(string expression)
    {
        try
        {
            return await _connector.ExecuteOnDteAsync(dte =>
            {
                var check = DebuggerHelpers.RequireBreakMode(dte, "evaluate");
                if (check is not null) return check;

                var result = dte.Debugger.GetExpression(expression, false, _options.ExpressionTimeoutMs);

                if (!result.IsValidValue)
                    return $"Error evaluating '{expression}': {result.Value}";

                return $"{expression} = {result.Value} ({result.Type})";
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating expression '{Expression}'", expression);
            return $"Error evaluating '{expression}': {ex.Message}. Tip: Use 'evaluate_multiple' tool to evaluate several expressions in a single call — it is more reliable and efficient.";
        }
    }

    public async Task<string> EvaluateMultipleExpressionsAsync(string[] expressions)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "evaluate");
            if (check is not null) return check;

            var sb = new StringBuilder();
            foreach (var expression in expressions)
            {
                var result = dte.Debugger.GetExpression(expression, false, _options.ExpressionTimeoutMs);
                if (!result.IsValidValue)
                    sb.AppendLine($"{expression} => Error: {result.Value}");
                else
                    sb.AppendLine($"{expression} = {result.Value} ({result.Type})");
            }
            return sb.ToString().TrimEnd();
        });
    }

    public async Task<string> GetCallStackAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get call stack");
            if (check is not null) return check;

            var thread = dte.Debugger.CurrentThread;
            if (thread is null)
                return "No current thread available.";

            var sb = new StringBuilder();
            int index = 0;

            foreach (StackFrame frame in thread.StackFrames)
            {
                var module = frame.Module;
                var functionName = frame.FunctionName;

                if (string.IsNullOrEmpty(module) || module == "[External Code]")
                    sb.AppendLine($"  #{index}  [External Code]");
                else
                    sb.AppendLine($"  #{index}  {functionName} - {module}");

                index++;
            }

            return $"Call Stack ({index} frame{(index != 1 ? "s" : "")}):\n{sb.ToString().TrimEnd()}";
        });
    }

    public async Task<string> GetCurrentLocationAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get location");
            if (check is not null) return check;

            var frame = dte.Debugger.CurrentStackFrame;
            if (frame is null)
                return "No current stack frame available.";

            var sb = new StringBuilder();
            sb.AppendLine($"Function: {frame.FunctionName}");
            sb.AppendLine($"Module: {frame.Module}");

            try
            {
                var doc = dte.ActiveDocument;
                if (doc is not null)
                {
                    sb.AppendLine($"File: {doc.FullName}");
                    if (doc.Selection is TextSelection sel)
                        sb.AppendLine($"Line: {sel.CurrentLine}");
                }
            }
            catch (COMException ex)
            {
                _logger.LogDebug(ex, "Document info not available for current location");
            }

            sb.AppendLine($"Language: {frame.Language}");

            return sb.ToString().TrimEnd();
        });
    }

    public async Task<string> GetExceptionInfoAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get exception info");
            if (check is not null) return check;

            var exResult = dte.Debugger.GetExpression("$exception", false, _options.ExpressionTimeoutMs);
            if (!exResult.IsValidValue)
                return "No exception in current context.";

            var sb = new StringBuilder();
            sb.AppendLine($"Exception: {exResult.Type}");
            sb.AppendLine($"Value: {exResult.Value}");

            var msgResult = dte.Debugger.GetExpression("$exception.Message", false, _options.ExpressionTimeoutMs);
            if (msgResult.IsValidValue)
                sb.AppendLine($"Message: {msgResult.Value}");

            var stackResult = dte.Debugger.GetExpression("$exception.StackTrace", false, _options.ExpressionTimeoutMs);
            if (stackResult.IsValidValue)
                sb.AppendLine($"StackTrace: {stackResult.Value}");

            var innerResult = dte.Debugger.GetExpression("$exception.InnerException", false, _options.ExpressionTimeoutMs);
            if (innerResult.IsValidValue && innerResult.Value != "null")
                sb.AppendLine($"InnerException: {innerResult.Value}");

            return sb.ToString().TrimEnd();
        });
    }

    public async Task<string> GetThreadsAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get threads");
            if (check is not null) return check;

            var sb = new StringBuilder();
            var currentThreadId = dte.Debugger.CurrentThread?.ID ?? -1;
            int count = 0;

            foreach (EnvDTE.Thread thread in dte.Debugger.CurrentProgram.Threads)
            {
                count++;
                var marker = thread.ID == currentThreadId ? " (current)" : "";
                var name = string.IsNullOrEmpty(thread.Name) ? $"Thread {thread.ID}" : thread.Name;

                sb.Append($"  #{thread.ID}  {name}{marker}");

                try
                {
                    if (thread.IsFrozen)
                        sb.Append(" [frozen]");
                }
                catch (COMException ex)
                {
                    _logger.LogDebug(ex, "Could not check frozen state for thread {ThreadId}", thread.ID);
                }

                sb.AppendLine();
            }

            return $"{count} thread(s):\n{sb.ToString().TrimEnd()}";
        });
    }

    public async Task<string> SwitchStackFrameAsync(int frameIndex)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "switch stack frame");
            if (check is not null) return check;

            var thread = dte.Debugger.CurrentThread;
            if (thread is null)
                return "No current thread available.";

            int index = 0;
            foreach (StackFrame frame in thread.StackFrames)
            {
                if (index == frameIndex)
                {
                    dte.Debugger.CurrentStackFrame = frame;
                    return $"Switched to frame #{frameIndex}: {frame.FunctionName} in {frame.Module}";
                }
                index++;
            }

            return $"Frame index {frameIndex} out of range (0-{index - 1}).";
        });
    }

    public async Task<string> GetOutputAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                var dte2 = (DTE2)dte;
                var outputWindow = dte2.ToolWindows.OutputWindow;
                EnvDTE.OutputWindowPane? debugPane = null;

                foreach (EnvDTE.OutputWindowPane pane in outputWindow.OutputWindowPanes)
                {
                    if (pane.Name == "Debug")
                    {
                        debugPane = pane;
                        break;
                    }
                }

                if (debugPane is null)
                    return "Debug output pane not found.";

                var textDoc = debugPane.TextDocument;
                var editPoint = textDoc.StartPoint.CreateEditPoint();
                var text = editPoint.GetText(textDoc.EndPoint);

                if (string.IsNullOrWhiteSpace(text))
                    return "Debug output is empty.";

                var lines = text.Split('\n');
                if (lines.Length > _options.MaxOutputLines)
                {
                    text = $"... ({lines.Length - _options.MaxOutputLines} lines truncated)\n" +
                           string.Join('\n', lines[^_options.MaxOutputLines..]);
                }

                return $"Debug Output ({lines.Length} lines):\n{text.TrimEnd()}";
            }
            catch (COMException ex)
            {
                _logger.LogDebug(ex, "Could not read debug output");
                return "Could not read debug output window.";
            }
        });
    }

    // ── Watch management ────────────────────────────────────────────

    public Task<string> AddWatchAsync(string expression)
    {
        lock (_watchLock)
        {
            if (_watchExpressions.Contains(expression))
                return Task.FromResult($"Watch '{expression}' already exists.");

            _watchExpressions.Add(expression);
            return Task.FromResult($"Watch added: {expression} ({_watchExpressions.Count} total)");
        }
    }

    public Task<string> RemoveWatchAsync(string expression)
    {
        lock (_watchLock)
        {
            if (!_watchExpressions.Remove(expression))
                return Task.FromResult($"Watch '{expression}' not found.");

            return Task.FromResult($"Watch removed: {expression} ({_watchExpressions.Count} remaining)");
        }
    }

    public async Task<string> ListWatchesAsync()
    {
        List<string> snapshot;
        lock (_watchLock)
        {
            if (_watchExpressions.Count == 0)
                return "No watch expressions set. Use add_watch to add one.";
            snapshot = new List<string>(_watchExpressions);
        }

        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{snapshot.Count} watch(es) (not in break mode, values unavailable):");
                foreach (var expr in snapshot)
                    sb.AppendLine($"  - {expr}");
                return sb.ToString().TrimEnd();
            }

            var resultSb = new StringBuilder();
            resultSb.AppendLine($"{snapshot.Count} watch(es):");
            foreach (var expr in snapshot)
            {
                var result = dte.Debugger.GetExpression(expr, false, _options.ExpressionTimeoutMs);
                if (!result.IsValidValue)
                    resultSb.AppendLine($"  {expr} => Error: {result.Value}");
                else
                    resultSb.AppendLine($"  {expr} = {result.Value} ({result.Type})");
            }
            return resultSb.ToString().TrimEnd();
        });
    }

    // ── Advanced inspection ─────────────────────────────────────

    public async Task<string> GetLoadedModulesAsync(string? filter = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get loaded modules");
            if (check is not null) return check;

            try
            {
                dynamic process = dte.Debugger.CurrentProcess;
                if (process is null)
                    return "No current process available.";

                var sb = new StringBuilder();
                int count = 0;

                foreach (dynamic module in process.Modules)
                {
                    string name;
                    string path;
                    try
                    {
                        name = module.Name;
                        path = module.Path;
                    }
                    catch { continue; }

                    if (filter is not null &&
                        !name.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                        !path.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    count++;
                    sb.AppendLine($"  {name}");
                    sb.AppendLine($"    Path: {path}");

                    try { sb.AppendLine($"    Order: {module.Order}"); } catch { }
                }

                return count == 0
                    ? (filter is not null ? $"No modules matching '{filter}'." : "No modules loaded.")
                    : $"{count} module(s){(filter is not null ? $" matching '{filter}'" : "")}:\n{sb.ToString().TrimEnd()}";
            }
            catch (Exception ex)
            {
                return $"Failed to get loaded modules: {ex.Message}";
            }
        });
    }

    public async Task<string> SearchVariablesAsync(string? namePattern = null, string? valuePattern = null, int maxDepth = 3)
    {
        if (namePattern is null && valuePattern is null)
            return "Either 'namePattern' or 'valuePattern' must be provided.";

        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "search variables");
            if (check is not null) return check;

            var frame = dte.Debugger.CurrentStackFrame;
            if (frame is null)
                return "No current stack frame available.";

            var sb = new StringBuilder();
            var nameRegex = namePattern is not null ? new Regex(namePattern, RegexOptions.IgnoreCase) : null;
            var valueRegex = valuePattern is not null ? new Regex(valuePattern, RegexOptions.IgnoreCase) : null;
            int matches = 0;
            var safeMaxDepth = Math.Min(maxDepth, _options.MaxVariableDepth);

            SearchExpressions(frame.Locals, sb, nameRegex, valueRegex, "", safeMaxDepth, 0, ref matches);
            SearchExpressions(frame.Arguments, sb, nameRegex, valueRegex, "", safeMaxDepth, 0, ref matches);

            return matches == 0
                ? "No matching variables found."
                : $"{matches} match(es):\n{sb.ToString().TrimEnd()}";
        });
    }

    public async Task<string> GetAutosAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get autos");
            if (check is not null) return check;

            var frame = dte.Debugger.CurrentStackFrame;
            if (frame is null)
                return "No current stack frame available.";

            var sb = new StringBuilder();
            sb.AppendLine("**Locals (current frame):**");
            foreach (Expression expr in frame.Locals)
            {
                sb.AppendLine($"  {expr.Name} = {expr.Value} ({expr.Type})");
            }

            // Try to get $ReturnValue
            var retResult = dte.Debugger.GetExpression("$ReturnValue", false, _options.ExpressionTimeoutMs);
            if (retResult.IsValidValue && retResult.Value != "undefined")
            {
                sb.AppendLine($"\n**Return Value:**");
                sb.AppendLine($"  $ReturnValue = {retResult.Value} ({retResult.Type})");
            }

            return sb.ToString().TrimEnd();
        });
    }

    public async Task<string> GetReturnValueAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get return value");
            if (check is not null) return check;

            var result = dte.Debugger.GetExpression("$ReturnValue", false, _options.ExpressionTimeoutMs);
            if (!result.IsValidValue)
                return "No return value available. Use this after stepping over or out of a function call.";

            return $"$ReturnValue = {result.Value} ({result.Type})";
        });
    }

    private static void SearchExpressions(
        Expressions expressions, StringBuilder sb,
        Regex? nameRegex, Regex? valueRegex,
        string prefix, int maxDepth, int currentDepth, ref int matches)
    {
        foreach (Expression expr in expressions)
        {
            var fullName = string.IsNullOrEmpty(prefix) ? expr.Name : $"{prefix}.{expr.Name}";
            var nameMatch = nameRegex is null || nameRegex.IsMatch(fullName);
            var valueMatch = valueRegex is null || valueRegex.IsMatch(expr.Value);

            if (nameMatch && valueMatch)
            {
                sb.AppendLine($"  {fullName} = {expr.Value} ({expr.Type})");
                matches++;
            }

            if (currentDepth < maxDepth && expr.DataMembers.Count > 0)
            {
                SearchExpressions(expr.DataMembers, sb, nameRegex, valueRegex,
                    fullName, maxDepth, currentDepth + 1, ref matches);
            }
        }
    }
}
