namespace VsDebugMcp.Debugger;

/// <summary>
/// Immutable record representing the current state of a debug session.
/// </summary>
public sealed record DebugState
{
    public bool IsDebugging { get; init; }
    public string Mode { get; init; } = "Design"; // Design, Run, Break
    public string? CurrentFile { get; init; }
    public int? CurrentLine { get; init; }
    public string? CurrentFunction { get; init; }
    public string? SolutionName { get; init; }
    public string? ActiveProjectName { get; init; }
    public int BreakpointCount { get; init; }

    public static DebugState NotDebugging => new()
    {
        IsDebugging = false,
        Mode = "Design"
    };
}
