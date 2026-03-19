namespace VsDebugMcp.Debugger;

/// <summary>
/// Exception settings management and exception chain inspection.
/// </summary>
public interface IExceptionDebugService
{
    Task<string> ManageExceptionSettingsAsync(string exceptionType, string breakMode);
    Task<string> ListExceptionSettingsAsync(string? filter = null);
    Task<string> GetExceptionChainAsync();
}
