namespace VsDebugMcp.Debugger;

/// <summary>
/// Abstraction over Visual Studio's DTE.Debugger for all debug operations.
/// </summary>
public interface IVsDebuggerService
{
    // Session control
    Task<string> StartDebuggingAsync(string? configuration = null);
    Task<string> StopDebuggingAsync();
    Task<string> RestartDebuggingAsync();
    Task<string> ApplyCodeChangesAsync();

    // Execution control
    Task<string> StepOverAsync();
    Task<string> StepIntoAsync();
    Task<string> StepOutAsync();
    Task<string> ContinueExecutionAsync();
    Task<string> SetNextStatementAsync(int line);
    Task<string> RunToCursorAsync(string filePath, int line);

    // Breakpoints
    Task<string> AddBreakpointAsync(string filePath, int line, string? condition = null);
    Task<string> AddBreakpointsBatchAsync(BreakpointRequest[] breakpoints);
    Task<string> RemoveBreakpointAsync(string filePath, int line);
    Task<string> ClearAllBreakpointsAsync();
    Task<string> ListBreakpointsAsync();
    Task<string> ToggleBreakpointAsync(string filePath, int line);

    // Inspection
    Task<string> GetVariablesAsync(int? depth = null);
    Task<string> EvaluateExpressionAsync(string expression);
    Task<string> GetCallStackAsync();
    Task<string> EvaluateMultipleExpressionsAsync(string[] expressions);
    Task<string> GetCurrentLocationAsync();
    Task<string> GetExceptionInfoAsync();
    Task<string> GetThreadsAsync();
    Task<string> SwitchStackFrameAsync(int frameIndex);
    Task<string> GetOutputAsync();

    // Watch management
    Task<string> AddWatchAsync(string expression);
    Task<string> RemoveWatchAsync(string expression);
    Task<string> ListWatchesAsync();

    // State
    Task<DebugState> GetDebugStateAsync();
    Task<string> ListConfigurationsAsync();
    Task<string[]> GetConfigurationNamesAsync();
    bool IsConnected { get; }
}
