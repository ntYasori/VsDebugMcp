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

    // Execution control
    Task<string> StepOverAsync();
    Task<string> StepIntoAsync();
    Task<string> StepOutAsync();
    Task<string> ContinueExecutionAsync();

    // Breakpoints
    Task<string> AddBreakpointAsync(string filePath, int line, string? condition = null);
    Task<string> RemoveBreakpointAsync(string filePath, int line);
    Task<string> ClearAllBreakpointsAsync();
    Task<string> ListBreakpointsAsync();

    // Inspection
    Task<string> GetVariablesAsync(int? depth = null);
    Task<string> EvaluateExpressionAsync(string expression);
    Task<string> GetCallStackAsync();

    // State
    Task<DebugState> GetDebugStateAsync();
    bool IsConnected { get; }
}
