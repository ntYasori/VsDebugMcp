using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class VsDebuggerService : IVsDebuggerService
{
    /// <summary>Timeout in ms for expression evaluation via DTE.</summary>
    private const int ExpressionTimeoutMs = 500;

    /// <summary>Maximum allowed depth when expanding variables to prevent huge output.</summary>
    private const int MaxVariableDepth = 5;

    private readonly DteConnector _connector;
    private readonly ILogger<VsDebuggerService> _logger;
    private readonly List<string> _watchExpressions = new();
    private readonly object _watchLock = new();

    public VsDebuggerService(DteConnector connector, ILogger<VsDebuggerService> logger)
    {
        _connector = connector;
        _logger = logger;
    }

    public bool IsConnected => _connector.IsConnected;

    private static string? RequireBreakMode(DTE dte, string operation) =>
        dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode
            ? $"Cannot {operation}: debugger is not in break mode."
            : null;

    // ── Session control ─────────────────────────────────────────────

    public async Task<string> StartDebuggingAsync(string? configuration = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode)
                return "Already debugging. Use restart_debugging to restart, or stop_debugging first.";

            if (configuration is not null)
            {
                try
                {
                    dte.Solution.SolutionBuild.SolutionConfigurations
                        .Item(configuration).Activate();
                }
                catch (COMException ex)
                {
                    _logger.LogWarning(ex, "Configuration '{Configuration}' not found", configuration);
                    return $"Configuration '{configuration}' not found. Use list_configurations to see available options.";
                }
            }

            dte.Debugger.Go(false);
            return $"Debugging started for solution '{dte.Solution.FullName}'.";
        });
    }

    public async Task<string> StopDebuggingAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
                return "Not currently debugging.";

            dte.Debugger.Stop(false);
            return "Debugging stopped.";
        });
    }

    public async Task<string> RestartDebuggingAsync()
    {
        var wasDebugging = await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode)
            {
                dte.Debugger.Stop(true);
                return true;
            }
            return false;
        });

        if (wasDebugging)
            await Task.Delay(500);

        return await _connector.ExecuteOnDteAsync(dte =>
        {
            dte.Debugger.Go(false);
            return "Debugging restarted.";
        });
    }

    public async Task<string> ApplyCodeChangesAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "apply code changes");
            if (check is not null) return check;

            try
            {
                dte.ExecuteCommand("Debug.ApplyCodeChanges");
                return "Edit and Continue: code changes applied successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply code changes");
                return $"Failed to apply code changes: {ex.Message}. " +
                       "Ensure Edit and Continue is enabled in VS settings and changes are compatible.";
            }
        });
    }

    // ── Execution control ───────────────────────────────────────────

    public async Task<string> StepOverAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "step");
            if (check is not null) return check;

            dte.Debugger.StepOver(false);
            return FormatCurrentLocation(dte);
        });
    }

    public async Task<string> StepIntoAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "step");
            if (check is not null) return check;

            dte.Debugger.StepInto(false);
            return FormatCurrentLocation(dte);
        });
    }

    public async Task<string> StepOutAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "step");
            if (check is not null) return check;

            dte.Debugger.StepOut(false);
            return FormatCurrentLocation(dte);
        });
    }

    public async Task<string> ContinueExecutionAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
                return "Not currently debugging. Use start_debugging first.";

            dte.Debugger.Go(false);
            return "Continuing execution...";
        });
    }

    public async Task<string> SetNextStatementAsync(int line)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "set next statement");
            if (check is not null) return check;

            try
            {
                var doc = dte.ActiveDocument;
                if (doc is null)
                    return "No active document.";

                var sel = (TextSelection)doc.Selection;
                sel.GotoLine(line, false);
                dte.Debugger.SetNextStatement();

                return $"Next statement set to line {line} in {doc.FullName}.";
            }
            catch (Exception ex)
            {
                return $"Failed to set next statement to line {line}: {ex.Message}";
            }
        });
    }

    public async Task<string> RunToCursorAsync(string filePath, int line)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "run to cursor");
            if (check is not null) return check;

            try
            {
                dte.ItemOperations.OpenFile(filePath);
                var doc = dte.ActiveDocument;
                if (doc is null)
                    return $"Could not open file: {filePath}";

                var sel = (TextSelection)doc.Selection;
                sel.GotoLine(line, false);

                dte.Debugger.RunToCursor(false);

                return $"Running to {filePath}:{line}...";
            }
            catch (Exception ex)
            {
                return $"Failed to run to cursor at {filePath}:{line}: {ex.Message}";
            }
        });
    }

    // ── Breakpoints ─────────────────────────────────────────────────

    public async Task<string> AddBreakpointAsync(string filePath, int line, string? condition = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                dte.Debugger.Breakpoints.Add(
                    File: filePath,
                    Line: line,
                    Condition: condition ?? "",
                    ConditionType: dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue);

                return $"Breakpoint added at {filePath}:{line}" +
                       (condition is not null ? $" (condition: {condition})" : "");
            }
            catch (Exception ex)
            {
                return $"Failed to add breakpoint: {ex.Message}";
            }
        });
    }

    public async Task<string> AddBreakpointsBatchAsync(BreakpointRequest[] breakpoints)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var sb = new StringBuilder();
            int success = 0;

            foreach (var bp in breakpoints)
            {
                try
                {
                    dte.Debugger.Breakpoints.Add(
                        File: bp.FilePath,
                        Line: bp.Line,
                        Condition: bp.Condition ?? "",
                        ConditionType: dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue);
                    success++;
                    sb.Append($"  + {bp.FilePath}:{bp.Line}");
                    if (bp.Condition is not null) sb.Append($" (condition: {bp.Condition})");
                    sb.AppendLine();
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  FAIL {bp.FilePath}:{bp.Line} - {ex.Message}");
                }
            }

            return $"{success}/{breakpoints.Length} breakpoint(s) added:\n{sb.ToString().TrimEnd()}";
        });
    }

    public async Task<string> RemoveBreakpointAsync(string filePath, int line)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var normalizedPath = Path.GetFullPath(filePath);
            foreach (Breakpoint bp in dte.Debugger.Breakpoints)
            {
                if (string.Equals(Path.GetFullPath(bp.File), normalizedPath, StringComparison.OrdinalIgnoreCase)
                    && bp.FileLine == line)
                {
                    bp.Delete();
                    return $"Breakpoint removed at {filePath}:{line}.";
                }
            }
            return $"No breakpoint found at {filePath}:{line}.";
        });
    }

    public async Task<string> ClearAllBreakpointsAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            int count = dte.Debugger.Breakpoints.Count;
            for (int i = count; i >= 1; i--)
            {
                dte.Debugger.Breakpoints.Item(i).Delete();
            }
            return $"Cleared {count} breakpoint(s).";
        });
    }

    public async Task<string> ListBreakpointsAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (Breakpoint bp in dte.Debugger.Breakpoints)
            {
                count++;
                sb.Append($"- {bp.File}:{bp.FileLine}");
                if (!string.IsNullOrEmpty(bp.Condition))
                    sb.Append($" (condition: {bp.Condition})");
                if (!bp.Enabled)
                    sb.Append(" [disabled]");
                sb.AppendLine();
            }

            return count == 0
                ? "No breakpoints set."
                : $"{count} breakpoint(s):\n{sb}";
        });
    }

    // ── Inspection ──────────────────────────────────────────────────

    public async Task<string> GetVariablesAsync(int? depth = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "inspect variables");
            if (check is not null) return check;

            var frame = dte.Debugger.CurrentStackFrame;
            if (frame is null)
                return "No current stack frame available.";

            var sb = new StringBuilder();
            var maxDepth = Math.Min(depth ?? 1, MaxVariableDepth);

            sb.AppendLine("**Locals:**");
            FormatExpressions(frame.Locals, sb, 0, maxDepth);

            sb.AppendLine("\n**Arguments:**");
            FormatExpressions(frame.Arguments, sb, 0, maxDepth);

            return sb.ToString();
        });
    }

    public async Task<string> EvaluateExpressionAsync(string expression)
    {
        try
        {
            return await _connector.ExecuteOnDteAsync(dte =>
            {
                var check = RequireBreakMode(dte, "evaluate");
                if (check is not null) return check;

                var result = dte.Debugger.GetExpression(expression, false, ExpressionTimeoutMs);

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
            var check = RequireBreakMode(dte, "evaluate");
            if (check is not null) return check;

            var sb = new StringBuilder();
            foreach (var expression in expressions)
            {
                var result = dte.Debugger.GetExpression(expression, false, ExpressionTimeoutMs);
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
            var check = RequireBreakMode(dte, "get call stack");
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
            var check = RequireBreakMode(dte, "get location");
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
                var result = dte.Debugger.GetExpression(expr, false, ExpressionTimeoutMs);
                if (!result.IsValidValue)
                    resultSb.AppendLine($"  {expr} => Error: {result.Value}");
                else
                    resultSb.AppendLine($"  {expr} = {result.Value} ({result.Type})");
            }
            return resultSb.ToString().TrimEnd();
        });
    }

    // ── State ───────────────────────────────────────────────────────

    public async Task<DebugState> GetDebugStateAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var mode = dte.Debugger.CurrentMode switch
            {
                dbgDebugMode.dbgDesignMode => "Design",
                dbgDebugMode.dbgRunMode => "Run",
                dbgDebugMode.dbgBreakMode => "Break",
                _ => "Unknown"
            };

            string? currentFile = null;
            int? currentLine = null;
            string? currentFunction = null;

            if (mode == "Break")
            {
                try
                {
                    var frame = dte.Debugger.CurrentStackFrame;
                    currentFunction = frame?.FunctionName;
                    var doc = dte.ActiveDocument;
                    currentFile = doc?.FullName;
                    if (doc?.Selection is TextSelection sel)
                        currentLine = sel.CurrentLine;
                }
                catch (COMException ex)
                {
                    _logger.LogDebug(ex, "Could not retrieve full debug state details");
                }
            }

            return new DebugState
            {
                IsDebugging = mode != "Design",
                Mode = mode,
                CurrentFile = currentFile,
                CurrentLine = currentLine,
                CurrentFunction = currentFunction,
                SolutionName = Path.GetFileNameWithoutExtension(dte.Solution.FullName),
                ActiveProjectName = GetActiveProjectName(dte),
                BreakpointCount = dte.Debugger.Breakpoints.Count
            };
        });
    }

    public async Task<string> ListConfigurationsAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var configs = dte.Solution.SolutionBuild.SolutionConfigurations;
            if (configs.Count == 0)
                return "No configurations found.";

            var active = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
            var sb = new StringBuilder();
            sb.AppendLine($"{configs.Count} configuration(s):");
            foreach (SolutionConfiguration config in configs)
            {
                var marker = config.Name == active ? " (active)" : "";
                sb.AppendLine($"  - {config.Name}{marker}");
            }
            return sb.ToString().TrimEnd();
        });
    }

    public async Task<string[]> GetConfigurationNamesAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var configs = dte.Solution.SolutionBuild.SolutionConfigurations;
            var result = new List<string>();
            foreach (SolutionConfiguration config in configs)
                result.Add(config.Name);
            return result.ToArray();
        });
    }

    public async Task<string> ToggleBreakpointAsync(string filePath, int line)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var normalizedPath = Path.GetFullPath(filePath);
            foreach (Breakpoint bp in dte.Debugger.Breakpoints)
            {
                if (string.Equals(Path.GetFullPath(bp.File), normalizedPath, StringComparison.OrdinalIgnoreCase)
                    && bp.FileLine == line)
                {
                    bp.Enabled = !bp.Enabled;
                    return $"Breakpoint at {filePath}:{line} is now {(bp.Enabled ? "enabled" : "disabled")}.";
                }
            }
            return $"No breakpoint found at {filePath}:{line}.";
        });
    }

    public async Task<string> GetExceptionInfoAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "get exception info");
            if (check is not null) return check;

            var exResult = dte.Debugger.GetExpression("$exception", false, ExpressionTimeoutMs);
            if (!exResult.IsValidValue)
                return "No exception in current context.";

            var sb = new StringBuilder();
            sb.AppendLine($"Exception: {exResult.Type}");
            sb.AppendLine($"Value: {exResult.Value}");

            var msgResult = dte.Debugger.GetExpression("$exception.Message", false, ExpressionTimeoutMs);
            if (msgResult.IsValidValue)
                sb.AppendLine($"Message: {msgResult.Value}");

            var stackResult = dte.Debugger.GetExpression("$exception.StackTrace", false, ExpressionTimeoutMs);
            if (stackResult.IsValidValue)
                sb.AppendLine($"StackTrace: {stackResult.Value}");

            var innerResult = dte.Debugger.GetExpression("$exception.InnerException", false, ExpressionTimeoutMs);
            if (innerResult.IsValidValue && innerResult.Value != "null")
                sb.AppendLine($"InnerException: {innerResult.Value}");

            return sb.ToString().TrimEnd();
        });
    }

    public async Task<string> GetThreadsAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "get threads");
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
                catch (COMException) { }

                sb.AppendLine();
            }

            return $"{count} thread(s):\n{sb.ToString().TrimEnd()}";
        });
    }

    public async Task<string> SwitchStackFrameAsync(int frameIndex)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = RequireBreakMode(dte, "switch stack frame");
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

                // Limit output to last 100 lines to avoid huge responses
                var lines = text.Split('\n');
                if (lines.Length > 100)
                {
                    text = $"... ({lines.Length - 100} lines truncated)\n" +
                           string.Join('\n', lines[^100..]);
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

    // ── Private helpers ─────────────────────────────────────────────

    private static string FormatCurrentLocation(DTE dte)
    {
        try
        {
            var frame = dte.Debugger.CurrentStackFrame;
            var doc = dte.ActiveDocument;
            var line = (doc?.Selection is TextSelection sel) ? sel.CurrentLine : 0;
            return $"Now at: {doc?.FullName ?? "unknown"}:{line} in {frame?.FunctionName ?? "unknown"}";
        }
        catch (COMException)
        {
            return "Step completed.";
        }
    }

    private static void FormatExpressions(Expressions expressions, StringBuilder sb, int indent, int maxDepth)
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

    private string? GetActiveProjectName(DTE dte)
    {
        try
        {
            var startupProjects = (Array)dte.Solution.SolutionBuild.StartupProjects;
            return startupProjects.Length > 0 ? startupProjects.GetValue(0)?.ToString() : null;
        }
        catch (COMException ex)
        {
            _logger.LogDebug(ex, "Could not get active project name");
            return null;
        }
    }
}
