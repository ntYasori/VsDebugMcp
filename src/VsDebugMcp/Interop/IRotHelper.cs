namespace VsDebugMcp.Interop;

/// <summary>
/// Abstraction for Running Object Table operations, enabling testability.
/// </summary>
public interface IRotHelper
{
    List<DteInstance> GetRunningDteInstances();
    object? GetDteByPid(int pid);
    DteInstance? GetFirstDteInstance();
}
