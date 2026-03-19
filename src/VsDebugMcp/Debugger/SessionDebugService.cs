using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class SessionDebugService : ISessionDebugService
{
    private readonly DteConnector _connector;
    private readonly ILogger<SessionDebugService> _logger;
    private readonly DebuggerOptions _options;

    public SessionDebugService(DteConnector connector, ILogger<SessionDebugService> logger, IOptions<DebuggerOptions> options)
    {
        _connector = connector;
        _logger = logger;
        _options = options.Value;
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
            await Task.Delay(_options.RestartDelayMs);

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
            var check = DebuggerHelpers.RequireBreakMode(dte, "apply code changes");
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
