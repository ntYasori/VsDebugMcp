namespace VsDebugMcp.Debugger;

/// <summary>
/// Immediate command execution and source navigation.
/// </summary>
public interface INavigationDebugService
{
    Task<string> ExecuteImmediateCommandAsync(string command);
    Task<string> NavigateToSourceAsync(string filePath, int line);
}
