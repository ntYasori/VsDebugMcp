using System.Text;
using EnvDTE;
using Microsoft.Extensions.Logging;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class ProcessDebugService : IProcessDebugService
{
    private readonly DteConnector _connector;
    private readonly ILogger<ProcessDebugService> _logger;

    public ProcessDebugService(DteConnector connector, ILogger<ProcessDebugService> logger)
    {
        _connector = connector;
        _logger = logger;
    }

    public async Task<string> AttachToProcessAsync(int? pid = null, string? processName = null)
    {
        if (pid is null && processName is null)
            return "Either 'pid' or 'processName' must be provided.";

        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var matches = new List<Process>();

            foreach (Process proc in dte.Debugger.LocalProcesses)
            {
                if (pid.HasValue && proc.ProcessID == pid.Value)
                {
                    matches.Add(proc);
                    break;
                }
                else if (processName is not null &&
                         proc.Name.Contains(processName, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(proc);
                }
            }

            if (matches.Count == 0)
            {
                var criteria = pid.HasValue ? $"PID {pid}" : $"name '{processName}'";
                return $"No process found matching {criteria}. Use list_processes to see available processes.";
            }

            if (matches.Count > 1)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Multiple processes match '{processName}'. Specify a PID to attach:");
                foreach (var p in matches)
                    sb.AppendLine($"  PID {p.ProcessID}: {p.Name}");
                return sb.ToString().TrimEnd();
            }

            var target = matches[0];
            try
            {
                target.Attach();
                _logger.LogInformation("Attached to process {ProcessName} (PID {Pid})", target.Name, target.ProcessID);
                return $"Attached to process '{target.Name}' (PID {target.ProcessID}).";
            }
            catch (Exception ex)
            {
                return $"Failed to attach to process PID {target.ProcessID}: {ex.Message}";
            }
        });
    }

    public async Task<string> DetachFromProcessAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            if (dte.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
                return "Not currently debugging any process.";

            try
            {
                dte.Debugger.DetachAll();
                _logger.LogInformation("Detached from all processes");
                return "Detached from all processes.";
            }
            catch (Exception ex)
            {
                return $"Failed to detach: {ex.Message}";
            }
        });
    }

    public async Task<string> ListProcessesAsync(string? filter = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var sb = new StringBuilder();
            int count = 0;
            int total = 0;

            foreach (Process proc in dte.Debugger.LocalProcesses)
            {
                total++;
                if (filter is not null &&
                    !proc.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                count++;
                sb.AppendLine($"  PID {proc.ProcessID}: {proc.Name}");
            }

            if (count == 0)
            {
                return filter is not null
                    ? $"No processes matching '{filter}' (out of {total} total)."
                    : "No processes found.";
            }

            var header = filter is not null
                ? $"{count} process(es) matching '{filter}':"
                : $"{count} process(es):";
            return $"{header}\n{sb.ToString().TrimEnd()}";
        });
    }
}
