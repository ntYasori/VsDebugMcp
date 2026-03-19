using System.Text;
using EnvDTE;
using Microsoft.Extensions.Logging;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class BreakpointDebugService : IBreakpointDebugService
{
    private readonly DteConnector _connector;
    private readonly ILogger<BreakpointDebugService> _logger;

    public BreakpointDebugService(DteConnector connector, ILogger<BreakpointDebugService> logger)
    {
        _connector = connector;
        _logger = logger;
    }

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

    // ── Advanced breakpoints ─────────────────────────────────────

    public async Task<string> AddTracepointAsync(string filePath, int line, string message, bool continueExecution = true)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                var bps = dte.Debugger.Breakpoints.Add(File: filePath, Line: line);
                if (bps.Count == 0)
                    return $"Failed to create tracepoint at {filePath}:{line}.";

                dynamic bp = bps.Item(1);
                bp.Message = message;
                bp.BreakWhenHit = !continueExecution;

                var action = continueExecution ? "logs without breaking" : "logs and breaks";
                return $"Tracepoint added at {filePath}:{line} — {action}.\n  Message: {message}";
            }
            catch (Exception ex)
            {
                return $"Failed to add tracepoint: {ex.Message}";
            }
        });
    }

    public async Task<string> SetHitCountBreakpointAsync(string filePath, int line, int hitCount, string hitCountType)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                var bps = dte.Debugger.Breakpoints.Add(File: filePath, Line: line);
                if (bps.Count == 0)
                    return $"Failed to create breakpoint at {filePath}:{line}.";

                dynamic bp = bps.Item(1);

                // EnvDTE80 dbgHitCountType values: 1=Equal, 2=GreaterOrEqual, 3=Multiple
                int hitCountTypeValue = hitCountType.ToLowerInvariant() switch
                {
                    "equal" => 1,
                    "greaterorequal" => 2,
                    "multiple" => 3,
                    _ => throw new ArgumentException($"Invalid hitCountType: '{hitCountType}'. Use 'equal', 'greaterOrEqual', or 'multiple'.")
                };

                bp.HitCountType = hitCountTypeValue;
                bp.HitCountTarget = hitCount;

                return $"Hit count breakpoint added at {filePath}:{line} — breaks when hit count is {hitCountType} {hitCount}.";
            }
            catch (ArgumentException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                return $"Failed to set hit count breakpoint: {ex.Message}";
            }
        });
    }

    public async Task<string> AddDataBreakpointAsync(string expression)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                dte.ExecuteCommand("Debug.NewDataBreakpoint", expression);
                return $"Data breakpoint set for '{expression}'.\n" +
                       "Note: Data breakpoints are supported in C++ native code and .NET Core 3.0+ for certain scenarios.";
            }
            catch (Exception ex)
            {
                return $"Failed to add data breakpoint for '{expression}': {ex.Message}. " +
                       "Data breakpoints may not be supported for this debug target.";
            }
        });
    }
}
