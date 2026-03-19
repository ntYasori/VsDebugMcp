namespace VsDebugMcp.Debugger;

/// <summary>
/// Process attachment and detachment operations.
/// </summary>
public interface IProcessDebugService
{
    Task<string> AttachToProcessAsync(int? pid = null, string? processName = null);
    Task<string> DetachFromProcessAsync();
    Task<string> ListProcessesAsync(string? filter = null);
}
