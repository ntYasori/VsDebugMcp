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
}
