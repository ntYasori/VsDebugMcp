using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class SessionTools
{
    [McpServerTool(Name = "start_debugging"), Description("Start debugging the current project in Visual Studio. Optionally specify a build configuration (e.g. 'Debug', 'Release'). If no configuration is specified and the client supports elicitation, you will be prompted to choose one.")]
    public static async Task<string> StartDebugging(
        McpServer server,
        IVsDebuggerService debugger,
        [Description("Build configuration name (e.g. 'Debug', 'Release'). If omitted, may prompt for selection.")] string? configuration = null,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null && server?.ClientCapabilities?.Elicitation is not null)
        {
            try
            {
                var configNames = await debugger.GetConfigurationNamesAsync();
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

        return await debugger.StartDebuggingAsync(configuration);
    }

    [McpServerTool(Name = "stop_debugging"), Description("Stop the current debugging session in Visual Studio.")]
    public static async Task<string> StopDebugging(IVsDebuggerService debugger)
    {
        return await debugger.StopDebuggingAsync();
    }

    [McpServerTool(Name = "restart_debugging"), Description("Restart the current debugging session (stop and start again).")]
    public static async Task<string> RestartDebugging(IVsDebuggerService debugger)
    {
        return await debugger.RestartDebuggingAsync();
    }

    [McpServerTool(Name = "edit_and_continue"), Description("Apply code changes while debugging (Edit and Continue). Allows modifying code during a debug session without restarting. The debugger must be in break mode. Not all changes are supported (e.g. adding new classes or changing method signatures may require restart).")]
    public static async Task<string> EditAndContinue(
        McpServer server,
        IVsDebuggerService debugger,
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

        return await debugger.ApplyCodeChangesAsync();
    }

    [McpServerTool(Name = "get_debug_state"), Description("Get the current state of the debugger, including mode (Design/Run/Break), current file, line, function, solution name, and breakpoint count. Use this to check if debugging is active and what state the debugger is in before performing operations.")]
    public static async Task<string> GetDebugState(IVsDebuggerService debugger)
    {
        var state = await debugger.GetDebugStateAsync();
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
    public static async Task<string> ListConfigurations(IVsDebuggerService debugger)
    {
        return await debugger.ListConfigurationsAsync();
    }
}
