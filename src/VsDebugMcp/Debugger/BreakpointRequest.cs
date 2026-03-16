namespace VsDebugMcp.Debugger;

public sealed record BreakpointRequest(string FilePath, int Line, string? Condition = null);
