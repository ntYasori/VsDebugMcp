namespace VsDebugMcp.Debugger;

/// <summary>
/// Variable inspection, expression evaluation, call stack, threads, watches, and output.
/// </summary>
public interface IInspectionDebugService
{
    Task<string> GetVariablesAsync(int? depth = null);
    Task<string> EvaluateExpressionAsync(string expression);
    Task<string> EvaluateMultipleExpressionsAsync(string[] expressions);
    Task<string> GetCallStackAsync();
    Task<string> GetCurrentLocationAsync();
    Task<string> GetExceptionInfoAsync();
    Task<string> GetThreadsAsync();
    Task<string> SwitchStackFrameAsync(int frameIndex);
    Task<string> GetOutputAsync();
    Task<string> AddWatchAsync(string expression);
    Task<string> RemoveWatchAsync(string expression);
    Task<string> ListWatchesAsync();
}
