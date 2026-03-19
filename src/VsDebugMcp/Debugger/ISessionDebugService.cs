namespace VsDebugMcp.Debugger;

/// <summary>
/// Debug session lifecycle: start, stop, restart, edit-and-continue, state, and configurations.
/// </summary>
public interface ISessionDebugService
{
    Task<string> StartDebuggingAsync(string? configuration = null);
    Task<string> StopDebuggingAsync();
    Task<string> RestartDebuggingAsync();
    Task<string> ApplyCodeChangesAsync();
    Task<DebugState> GetDebugStateAsync();
    Task<string> ListConfigurationsAsync();
    Task<string[]> GetConfigurationNamesAsync();
    bool IsConnected { get; }
}
