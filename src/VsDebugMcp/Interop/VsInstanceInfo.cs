namespace VsDebugMcp.Interop;

public sealed record VsInstanceInfo(
    int ProcessId,
    string Version,
    string? SolutionName,
    string? WindowTitle,
    bool IsCurrent);
