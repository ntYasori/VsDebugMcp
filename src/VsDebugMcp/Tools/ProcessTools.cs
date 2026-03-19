using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class ProcessTools
{
    [McpServerTool(Name = "attach_to_process"), Description("Attach the Visual Studio debugger to a running process. Specify either a PID or a process name. If multiple processes match the name and the client supports elicitation, you will be prompted to choose one. Use list_processes to see available processes.")]
    public static async Task<string> AttachToProcess(
        McpServer server,
        IProcessDebugService process,
        [Description("Process ID to attach to. Takes precedence over processName.")] int? pid = null,
        [Description("Process name or partial name to search for (e.g. 'MyApp', 'dotnet').")] string? processName = null,
        CancellationToken cancellationToken = default)
    {
        // If no PID provided and we have a name, check for multiple matches via elicitation
        if (pid is null && processName is not null && server?.ClientCapabilities?.Elicitation is not null)
        {
            try
            {
                // Get the list to check for multiple matches
                var listResult = await process.ListProcessesAsync(processName);
                if (listResult.Contains("Multiple processes match") || listResult.Split('\n').Length > 3)
                {
                    // Parse PIDs from list and offer selection
                    var lines = listResult.Split('\n')
                        .Where(l => l.TrimStart().StartsWith("PID "))
                        .ToArray();

                    if (lines.Length > 1)
                    {
                        var options = lines.Select(line =>
                        {
                            var parts = line.Trim().Split(':', 2);
                            var pidStr = parts[0].Replace("PID ", "").Trim();
                            var name = parts.Length > 1 ? parts[1].Trim() : pidStr;
                            return new ElicitRequestParams.EnumSchemaOption
                            {
                                Const = pidStr,
                                Title = $"{name} (PID {pidStr})"
                            };
                        }).ToArray();

                        var result = await server.ElicitAsync(new ElicitRequestParams
                        {
                            Message = $"Multiple processes match '{processName}'. Select one to attach:",
                            RequestedSchema = new ElicitRequestParams.RequestSchema
                            {
                                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                                {
                                    ["process"] = new ElicitRequestParams.TitledSingleSelectEnumSchema
                                    {
                                        Description = "Process to attach to",
                                        OneOf = options,
                                    }
                                }
                            }
                        }, cancellationToken);

                        if (result.Action == "accept"
                            && result.Content?.TryGetValue("process", out var val) == true
                            && int.TryParse(val.GetString(), out var selectedPid))
                        {
                            pid = selectedPid;
                            processName = null;
                        }
                        else
                        {
                            return "Process selection cancelled.";
                        }
                    }
                }
            }
            catch { /* Elicitation not available, fall through to service */ }
        }

        return await process.AttachToProcessAsync(pid, processName);
    }

    [McpServerTool(Name = "detach_from_process"), Description("Detach the debugger from all attached processes without stopping them. The processes will continue running independently.")]
    public static async Task<string> DetachFromProcess(
        McpServer server,
        IProcessDebugService process,
        CancellationToken cancellationToken = default)
    {
        if (server?.ClientCapabilities?.Elicitation is not null)
        {
            try
            {
                var result = await server.ElicitAsync(new ElicitRequestParams
                {
                    Message = "Detach from all debugged processes? The processes will continue running.",
                    RequestedSchema = new ElicitRequestParams.RequestSchema
                    {
                        Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                        {
                            ["confirm"] = new ElicitRequestParams.BooleanSchema
                            {
                                Description = "Detach from processes?",
                                Default = true
                            }
                        }
                    }
                }, cancellationToken);

                if (result.Action != "accept" || result.Content?["confirm"].ValueKind != JsonValueKind.True)
                    return "Detach cancelled by user.";
            }
            catch { /* Elicitation not available, proceed */ }
        }

        return await process.DetachFromProcessAsync();
    }

    [McpServerTool(Name = "list_processes"), Description("List running processes that can be attached to for debugging. Optionally filter by name.")]
    public static async Task<string> ListProcesses(
        IProcessDebugService process,
        [Description("Optional filter to search processes by name (e.g. 'dotnet', 'MyApp').")] string? filter = null)
    {
        return await process.ListProcessesAsync(filter);
    }
}
