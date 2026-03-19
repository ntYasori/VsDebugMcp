namespace VsDebugMcp;

/// <summary>
/// Configurable options for debugger operations.
/// Values can be overridden via environment variables prefixed with VSDEBUGMCP_.
/// </summary>
public class DebuggerOptions
{
    /// <summary>Timeout in milliseconds for expression evaluation via DTE.</summary>
    public int ExpressionTimeoutMs { get; set; } = 500;

    /// <summary>Timeout in milliseconds for COM operations on the STA thread.</summary>
    public int ComOperationTimeoutMs { get; set; } = 10_000;

    /// <summary>Maximum allowed depth when expanding variables to prevent huge output.</summary>
    public int MaxVariableDepth { get; set; } = 5;

    /// <summary>Maximum number of output lines returned from the Debug Output window.</summary>
    public int MaxOutputLines { get; set; } = 100;

    /// <summary>Delay in milliseconds between stop and start during restart debugging.</summary>
    public int RestartDelayMs { get; set; } = 500;
}
