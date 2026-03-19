using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using VsDebugMcp.Debugger;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class SessionTools
{
    [McpServerTool(Name = "start_debugging"), Description("Start debugging the current project in Visual Studio. Optionally specify a build configuration (e.g. 'Debug', 'Release'). If no configuration is specified and the client supports elicitation, you will be prompted to choose one.")]
    public static async Task<string> StartDebugging(
        McpServer server,
        ISessionDebugService session,
        [Description("Build configuration name (e.g. 'Debug', 'Release'). If omitted, may prompt for selection.")] string? configuration = null,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null && server?.ClientCapabilities?.Elicitation is not null)
        {
            try
            {
                var configNames = await session.GetConfigurationNamesAsync();
                if (configNames.Length > 1)
                {
                    var oneOf = configNames.Select(name =>
                        new ElicitRequestParams.EnumSchemaOption { Const = name, Title = name }).ToArray();

                    var result = await server.ElicitAsync(new ElicitRequestParams
                    {
                        Message = "Select the build configuration to debug:",
                        RequestedSchema = new ElicitRequestParams.RequestSchema
                        {
                            Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                            {
                                ["configuration"] = new ElicitRequestParams.TitledSingleSelectEnumSchema
                                {
                                    Description = "Build configuration",
                                    OneOf = oneOf,
                                }
                            }
                        }
                    }, cancellationToken);

                    if (result.Action == "accept" && result.Content?.TryGetValue("configuration", out var configValue) == true)
                        configuration = configValue.GetString();
                }
            }
            catch { /* Elicitation not available, continue with default */ }
        }

        return await session.StartDebuggingAsync(configuration);
    }

    [McpServerTool(Name = "stop_debugging"), Description("Stop the current debugging session in Visual Studio.")]
    public static async Task<string> StopDebugging(ISessionDebugService session)
    {
        return await session.StopDebuggingAsync();
    }

    [McpServerTool(Name = "restart_debugging"), Description("Restart the current debugging session (stop and start again).")]
    public static async Task<string> RestartDebugging(ISessionDebugService session)
    {
        return await session.RestartDebuggingAsync();
    }

    [McpServerTool(Name = "edit_and_continue"), Description("Apply code changes while debugging (Edit and Continue). Allows modifying code during a debug session without restarting. The debugger must be in break mode. Not all changes are supported (e.g. adding new classes or changing method signatures may require restart).")]
    public static async Task<string> EditAndContinue(
        McpServer server,
        ISessionDebugService session,
        CancellationToken cancellationToken = default)
    {
        if (server?.ClientCapabilities?.Elicitation is not null)
        {
            try
            {
                var result = await server.ElicitAsync(new ElicitRequestParams
                {
                    Message = "Apply code changes to the running process? This will modify the executing code using Edit and Continue.",
                    RequestedSchema = new ElicitRequestParams.RequestSchema
                    {
                        Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                        {
                            ["confirm"] = new ElicitRequestParams.BooleanSchema
                            {
                                Description = "Apply changes?",
                                Default = true
                            }
                        }
                    }
                }, cancellationToken);

                if (result.Action != "accept" || result.Content?["confirm"].ValueKind != JsonValueKind.True)
                    return "Edit and Continue cancelled by user.";
            }
            catch { /* Elicitation not available, proceed */ }
        }

        return await session.ApplyCodeChangesAsync();
    }

    [McpServerTool(Name = "get_debug_state"), Description("Get the current state of the debugger, including mode (Design/Run/Break), current file, line, function, solution name, and breakpoint count. Use this to check if debugging is active and what state the debugger is in before performing operations.")]
    public static async Task<string> GetDebugState(ISessionDebugService session)
    {
        var state = await session.GetDebugStateAsync();
        var sb = new StringBuilder();
        sb.AppendLine($"Mode: {state.Mode}");
        sb.AppendLine($"IsDebugging: {state.IsDebugging}");
        if (state.SolutionName is not null)
            sb.AppendLine($"Solution: {state.SolutionName}");
        if (state.ActiveProjectName is not null)
            sb.AppendLine($"Project: {state.ActiveProjectName}");
        sb.AppendLine($"Breakpoints: {state.BreakpointCount}");
        if (state.CurrentFunction is not null)
            sb.AppendLine($"Function: {state.CurrentFunction}");
        if (state.CurrentFile is not null)
            sb.AppendLine($"File: {state.CurrentFile}");
        if (state.CurrentLine is not null)
            sb.AppendLine($"Line: {state.CurrentLine}");
        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "list_configurations"), Description("List all available build configurations for the current solution (e.g. Debug, Release). Shows which configuration is currently active.")]
    public static async Task<string> ListConfigurations(ISessionDebugService session)
    {
        return await session.ListConfigurationsAsync();
    }

    [McpServerTool(Name = "list_vs_instances"), Description("List all running Visual Studio instances with their PID, version, solution name, and connection status. Use this to discover available VS instances before switching.")]
    public static async Task<string> ListVsInstances(DteConnector connector)
    {
        var instances = await connector.ListInstancesAsync();
        if (instances.Count == 0)
            return "No running Visual Studio instances found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {instances.Count} Visual Studio instance(s):");
        sb.AppendLine();
        foreach (var inst in instances)
        {
            var current = inst.IsCurrent ? " (connected)" : "";
            sb.AppendLine($"  PID: {inst.ProcessId}{current}");
            sb.AppendLine($"  Version: {inst.Version}");
            if (inst.SolutionName is not null)
                sb.AppendLine($"  Solution: {inst.SolutionName}");
            if (inst.WindowTitle is not null)
                sb.AppendLine($"  Window: {inst.WindowTitle}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "switch_vs_instance"), Description("Switch to a different running Visual Studio instance. If PID is omitted and the client supports elicitation, presents a selection dialog. Use list_vs_instances first to see available instances.")]
    public static async Task<string> SwitchVsInstance(
        McpServer server,
        DteConnector connector,
        [Description("Process ID of the target Visual Studio instance. Use list_vs_instances to find PIDs. If omitted, shows a selection dialog.")] int? pid = null,
        CancellationToken cancellationToken = default)
    {
        if (pid is null)
        {
            var instances = await connector.ListInstancesAsync();
            if (instances.Count == 0)
                return "No running Visual Studio instances found.";
            if (instances.Count == 1)
            {
                pid = instances[0].ProcessId;
            }
            else if (server?.ClientCapabilities?.Elicitation is not null)
            {
                try
                {
                    var options = instances.Select(i =>
                    {
                        var label = i.SolutionName ?? $"PID {i.ProcessId}";
                        if (i.IsCurrent) label += " (current)";
                        return new ElicitRequestParams.EnumSchemaOption
                        {
                            Const = i.ProcessId.ToString(),
                            Title = $"{label} \u2014 VS {i.Version} (PID {i.ProcessId})"
                        };
                    }).ToArray();

                    var result = await server.ElicitAsync(new ElicitRequestParams
                    {
                        Message = "Select the Visual Studio instance to connect to:",
                        RequestedSchema = new ElicitRequestParams.RequestSchema
                        {
                            Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                            {
                                ["instance"] = new ElicitRequestParams.TitledSingleSelectEnumSchema
                                {
                                    Description = "Visual Studio instance",
                                    OneOf = options,
                                }
                            }
                        }
                    }, cancellationToken);

                    if (result.Action == "accept"
                        && result.Content?.TryGetValue("instance", out var val) == true
                        && int.TryParse(val.GetString(), out var selectedPid))
                    {
                        pid = selectedPid;
                    }
                    else
                    {
                        return "Instance selection cancelled.";
                    }
                }
                catch { /* Elicitation not available, fall through */ }
            }

            if (pid is null)
                return "Multiple VS instances found. Specify a PID or use a client that supports elicitation.\n\n"
                     + await ListVsInstances(connector);
        }

        await connector.SwitchAsync(pid.Value);
        var info = (await connector.ListInstancesAsync())
            .FirstOrDefault(i => i.ProcessId == pid.Value);
        var name = info?.SolutionName ?? $"PID {pid.Value}";
        return $"Switched to Visual Studio instance: {name} (PID {pid.Value}, VS {info?.Version})";
    }
}
