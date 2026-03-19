using ModelContextProtocol.Server;
using System.ComponentModel;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class ExceptionTools
{
    [McpServerTool(Name = "manage_exception_settings"), Description("Configure how the debugger handles a specific exception type. Set whether to break when the exception is thrown ('always'), only when it's user-unhandled ('user-unhandled'), or never ('never'). Use the full exception type name (e.g. 'System.NullReferenceException').")]
    public static async Task<string> ManageExceptionSettings(
        IExceptionDebugService exception,
        [Description("Full exception type name (e.g. 'System.NullReferenceException', 'System.IO.IOException').")] string exceptionType,
        [Description("Break mode: 'always' (break when thrown), 'user-unhandled' (break only if not caught by user code), or 'never' (don't break).")] string breakMode)
    {
        return await exception.ManageExceptionSettingsAsync(exceptionType, breakMode);
    }

    [McpServerTool(Name = "list_exception_settings"), Description("List exception settings showing which exceptions are configured to break. Optionally filter by namespace or type name.")]
    public static async Task<string> ListExceptionSettings(
        IExceptionDebugService exception,
        [Description("Optional filter to search by exception type name or namespace (e.g. 'System.IO', 'NullReference').")] string? filter = null)
    {
        return await exception.ListExceptionSettingsAsync(filter);
    }

    [McpServerTool(Name = "get_exception_chain"), Description("Get the complete exception chain including all inner exceptions. Shows type, message, and stack trace for each exception in the chain. Only works when stopped at an exception.")]
    public static async Task<string> GetExceptionChain(IExceptionDebugService exception)
    {
        return await exception.GetExceptionChainAsync();
    }
}
