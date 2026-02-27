using System.Text;
using EnvDTE;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class VsDebuggerService : IVsDebuggerService
{
    private readonly DteConnector _connector;
    private readonly ComThread _comThread;

    public VsDebuggerService(DteConnector connector, ComThread comThread)
    {
        _connector = connector;
        _comThread = comThread;
    }

    public bool IsConnected => _connector.IsConnected;

    public async Task<string> StartDebuggingAsync(string? configuration = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode)
                return "Already debugging. Use restart_debugging to restart, or stop_debugging first.";

            if (configuration is not null)
            {
                // Try to set active configuration
                try
                {
                    dte.Solution.SolutionBuild.SolutionConfigurations
                        .Item(configuration).Activate();
                }
                catch
                {
                    return $"Configuration '{configuration}' not found. Available configurations can be checked in VS.";
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
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode)
            {
                dte.Debugger.Stop(true); // Wait for stop
                System.Threading.Thread.Sleep(500);
            }

            dte.Debugger.Go(false);
            return "Debugging restarted.";
        });
    }

    public async Task<string> StepOverAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return "Cannot step: debugger is not in break mode. Set a breakpoint and wait for it to be hit.";

            dte.Debugger.StepOver(false);
            return FormatCurrentLocation(dte);
        });
    }

    public async Task<string> StepIntoAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return "Cannot step: debugger is not in break mode.";

            dte.Debugger.StepInto(false);
            return FormatCurrentLocation(dte);
        });
    }

    public async Task<string> StepOutAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return "Cannot step: debugger is not in break mode.";

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

    public async Task<string> AddBreakpointAsync(string filePath, int line, string? condition = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                var bp = dte.Debugger.Breakpoints.Add(
                    File: filePath,
                    Line: line,
                    Condition: condition ?? "",
                    ConditionType: condition is not null
                        ? dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue
                        : dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue);

                return $"Breakpoint added at {filePath}:{line}" +
                       (condition is not null ? $" (condition: {condition})" : "");
            }
            catch (Exception ex)
            {
                return $"Failed to add breakpoint: {ex.Message}";
            }
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
            // Delete in reverse to avoid index issues
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

    public async Task<string> GetVariablesAsync(int? depth = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return "Cannot inspect variables: debugger is not in break mode.";

            var frame = dte.Debugger.CurrentStackFrame;
            if (frame is null)
                return "No current stack frame available.";

            var sb = new StringBuilder();
            var maxDepth = depth ?? 1;

            sb.AppendLine("**Locals:**");
            FormatExpressions(frame.Locals, sb, 0, maxDepth);

            sb.AppendLine("\n**Arguments:**");
            FormatExpressions(frame.Arguments, sb, 0, maxDepth);

            return sb.ToString();
        });
    }

    public async Task<string> EvaluateExpressionAsync(string expression)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return "Cannot evaluate: debugger is not in break mode.";

            var result = dte.Debugger.GetExpression(expression, false, 500);

            if (!result.IsValidValue)
                return $"Error evaluating '{expression}': {result.Value}";

            return $"{expression} = {result.Value} ({result.Type})";
        });
    }

    public async Task<string> GetCallStackAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return "Cannot get call stack: debugger is not in break mode.";

            var thread = dte.Debugger.CurrentThread;
            if (thread is null)
                return "No current thread available.";

            var frames = thread.StackFrames;
            var sb = new StringBuilder();
            int count = 0;

            foreach (StackFrame frame in frames)
                count++;

            sb.AppendLine($"Call Stack ({count} frame{(count != 1 ? "s" : "")}):");

            int index = 0;
            foreach (StackFrame frame in frames)
            {
                var module = frame.Module;
                var functionName = frame.FunctionName;

                if (string.IsNullOrEmpty(module) || module == "[External Code]")
                {
                    sb.AppendLine($"  #{index}  [External Code]");
                }
                else
                {
                    sb.AppendLine($"  #{index}  {functionName} - {module}");
                }

                index++;
            }

            return sb.ToString().TrimEnd();
        });
    }

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
                    // Try to get current source location
                    var doc = dte.ActiveDocument;
                    currentFile = doc?.FullName;
                    if (doc?.Selection is TextSelection sel)
                        currentLine = sel.CurrentLine;
                }
                catch { /* Not always available */ }
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

    private static string FormatCurrentLocation(DTE dte)
    {
        try
        {
            var frame = dte.Debugger.CurrentStackFrame;
            var doc = dte.ActiveDocument;
            var line = (doc?.Selection is TextSelection sel) ? sel.CurrentLine : 0;
            return $"Now at: {doc?.FullName ?? "unknown"}:{line} in {frame?.FunctionName ?? "unknown"}";
        }
        catch
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

    private static string? GetActiveProjectName(DTE dte)
    {
        try
        {
            var startupProjects = (Array)dte.Solution.SolutionBuild.StartupProjects;
            return startupProjects.Length > 0 ? startupProjects.GetValue(0)?.ToString() : null;
        }
        catch
        {
            return null;
        }
    }
}
