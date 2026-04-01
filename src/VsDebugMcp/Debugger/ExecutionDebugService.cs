using EnvDTE;
using Microsoft.Extensions.Logging;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class ExecutionDebugService : IExecutionDebugService
{
    private readonly DteConnector _connector;
    private readonly ILogger<ExecutionDebugService> _logger;

    public ExecutionDebugService(DteConnector connector, ILogger<ExecutionDebugService> logger)
    {
        _connector = connector;
        _logger = logger;
    }

    public async Task<string> StepOverAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "step");
            if (check is not null) return check;

            dte.Debugger.StepOver(true);
            return DebuggerHelpers.FormatCurrentLocation(dte);
        });
    }

    public async Task<string> StepIntoAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "step");
            if (check is not null) return check;

            dte.Debugger.StepInto(true);
            return DebuggerHelpers.FormatCurrentLocation(dte);
        });
    }

    public async Task<string> StepOutAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "step");
            if (check is not null) return check;

            dte.Debugger.StepOut(true);
            return DebuggerHelpers.FormatCurrentLocation(dte);
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
            var check = DebuggerHelpers.RequireBreakMode(dte, "set next statement");
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
            var check = DebuggerHelpers.RequireBreakMode(dte, "run to cursor");
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
}
