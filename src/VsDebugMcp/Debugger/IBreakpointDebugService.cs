namespace VsDebugMcp.Debugger;

/// <summary>
/// Breakpoint management: add, remove, toggle, list, clear, and batch operations.
/// </summary>
public interface IBreakpointDebugService
{
    Task<string> AddBreakpointAsync(string filePath, int line, string? condition = null);
    Task<string> AddBreakpointsBatchAsync(BreakpointRequest[] breakpoints);
    Task<string> RemoveBreakpointAsync(string filePath, int line);
    Task<string> ClearAllBreakpointsAsync();
    Task<string> ListBreakpointsAsync();
    Task<string> ToggleBreakpointAsync(string filePath, int line);
}
