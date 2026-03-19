using EnvDTE;
using Microsoft.Extensions.Logging;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class NavigationDebugService : INavigationDebugService
{
    private readonly DteConnector _connector;
    private readonly ILogger<NavigationDebugService> _logger;

    public NavigationDebugService(DteConnector connector, ILogger<NavigationDebugService> logger)
    {
        _connector = connector;
        _logger = logger;
    }

    public async Task<string> ExecuteImmediateCommandAsync(string command)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "execute immediate command");
            if (check is not null) return check;

            try
            {
                dte.Debugger.ExecuteStatement(command);
                return $"Executed: {command}";
            }
            catch (Exception ex)
            {
                return $"Failed to execute '{command}': {ex.Message}";
            }
        });
    }

    public async Task<string> NavigateToSourceAsync(string filePath, int line)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                dte.ItemOperations.OpenFile(filePath);
                var doc = dte.ActiveDocument;
                if (doc is null)
                    return $"Could not open file: {filePath}";

                var sel = (TextSelection)doc.Selection;
                sel.GotoLine(line, false);

                return $"Navigated to {filePath}:{line}";
            }
            catch (Exception ex)
            {
                return $"Failed to navigate to {filePath}:{line}: {ex.Message}";
            }
        });
    }
}
