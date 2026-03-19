namespace VsDebugMcp.Debugger;

/// <summary>
/// Execution control: stepping, continue, set next statement, and run to cursor.
/// </summary>
public interface IExecutionDebugService
{
    Task<string> StepOverAsync();
    Task<string> StepIntoAsync();
    Task<string> StepOutAsync();
    Task<string> ContinueExecutionAsync();
    Task<string> SetNextStatementAsync(int line);
    Task<string> RunToCursorAsync(string filePath, int line);
}
