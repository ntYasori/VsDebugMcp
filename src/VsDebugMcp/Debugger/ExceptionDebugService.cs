using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VsDebugMcp.Interop;

namespace VsDebugMcp.Debugger;

public sealed class ExceptionDebugService : IExceptionDebugService
{
    private readonly DteConnector _connector;
    private readonly ILogger<ExceptionDebugService> _logger;
    private readonly DebuggerOptions _options;

    public ExceptionDebugService(
        DteConnector connector,
        ILogger<ExceptionDebugService> logger,
        IOptions<DebuggerOptions> options)
    {
        _connector = connector;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> ManageExceptionSettingsAsync(string exceptionType, string breakMode)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                dynamic debugger = dte.Debugger;
                var exceptionGroups = debugger.ExceptionGroups;

                foreach (dynamic group in exceptionGroups)
                {
                    try
                    {
                        dynamic item = group.Item(exceptionType);
                        if (item is null) continue;

                        var (breakWhenThrown, breakWhenUserUnhandled) = ParseBreakMode(breakMode);
                        group.SetBreakWhenThrown(breakWhenThrown, item);
                        group.SetBreakWhenUserUnhandled(breakWhenUserUnhandled, item);

                        _logger.LogInformation("Exception settings updated: {Type} -> {Mode}", exceptionType, breakMode);
                        return $"Exception '{exceptionType}' set to '{breakMode}'.";
                    }
                    catch (COMException)
                    {
                        // This group doesn't contain the exception type
                    }
                }

                return $"Exception type '{exceptionType}' not found in any exception group. " +
                       "Ensure the full type name is correct (e.g., 'System.NullReferenceException').";
            }
            catch (InvalidCastException)
            {
                return "Exception settings management requires Visual Studio with Debugger2 support.";
            }
            catch (ArgumentException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                return $"Failed to manage exception settings: {ex.Message}";
            }
        });
    }

    public async Task<string> ListExceptionSettingsAsync(string? filter = null)
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            try
            {
                dynamic debugger = dte.Debugger;
                var exceptionGroups = debugger.ExceptionGroups;

                var sb = new StringBuilder();
                int totalCount = 0;

                foreach (dynamic group in exceptionGroups)
                {
                    string groupName;
                    try { groupName = group.Name; }
                    catch { continue; }

                    var groupEntries = new StringBuilder();
                    int groupCount = 0;

                    try
                    {
                        foreach (dynamic setting in group)
                        {
                            string name;
                            try { name = setting.Name; }
                            catch { continue; }

                            if (filter is not null &&
                                !name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                continue;

                            bool breakWhenThrown;
                            try { breakWhenThrown = setting.BreakWhenThrown; }
                            catch { breakWhenThrown = false; }

                            var mode = breakWhenThrown ? "always" : "user-unhandled";
                            groupEntries.AppendLine($"    {name} [{mode}]");
                            groupCount++;
                            totalCount++;
                        }
                    }
                    catch (COMException)
                    {
                        // Some groups may not support enumeration
                    }

                    if (groupCount > 0)
                    {
                        sb.AppendLine($"  {groupName} ({groupCount}):");
                        sb.Append(groupEntries);
                    }
                }

                if (totalCount == 0)
                {
                    return filter is not null
                        ? $"No exception settings matching '{filter}'."
                        : "No exception settings found or unable to enumerate.";
                }

                return $"Exception settings{(filter is not null ? $" matching '{filter}'" : "")} ({totalCount}):\n{sb.ToString().TrimEnd()}";
            }
            catch (InvalidCastException)
            {
                return "Exception settings listing requires Visual Studio with Debugger2 support.";
            }
            catch (Exception ex)
            {
                return $"Failed to list exception settings: {ex.Message}";
            }
        });
    }

    public async Task<string> GetExceptionChainAsync()
    {
        return await _connector.ExecuteOnDteAsync(dte =>
        {
            var check = DebuggerHelpers.RequireBreakMode(dte, "get exception chain");
            if (check is not null) return check;

            var exResult = dte.Debugger.GetExpression("$exception", false, _options.ExpressionTimeoutMs);
            if (!exResult.IsValidValue)
                return "No exception in current context.";

            var sb = new StringBuilder();
            int depth = 0;
            const int maxDepth = 10;
            string currentPrefix = "$exception";

            while (depth < maxDepth)
            {
                var typeResult = dte.Debugger.GetExpression($"{currentPrefix}.GetType().FullName", false, _options.ExpressionTimeoutMs);
                var msgResult = dte.Debugger.GetExpression($"{currentPrefix}.Message", false, _options.ExpressionTimeoutMs);
                var stackResult = dte.Debugger.GetExpression($"{currentPrefix}.StackTrace", false, _options.ExpressionTimeoutMs);

                var typeName = typeResult.IsValidValue ? typeResult.Value.Trim('"') : "Unknown";
                var message = msgResult.IsValidValue ? msgResult.Value : "N/A";
                var stackTrace = stackResult.IsValidValue ? stackResult.Value : "N/A";

                var indent = new string(' ', depth * 2);
                sb.AppendLine($"{indent}[{depth}] {typeName}");
                sb.AppendLine($"{indent}    Message: {message}");
                if (stackTrace != "null" && stackTrace != "N/A")
                    sb.AppendLine($"{indent}    StackTrace: {stackTrace}");

                // Check for inner exception
                var innerResult = dte.Debugger.GetExpression($"{currentPrefix}.InnerException", false, _options.ExpressionTimeoutMs);
                if (!innerResult.IsValidValue || innerResult.Value == "null")
                    break;

                currentPrefix += ".InnerException";
                depth++;
            }

            if (depth >= maxDepth)
                sb.AppendLine($"... (chain truncated at depth {maxDepth})");

            return $"Exception chain ({depth + 1} exception{(depth > 0 ? "s" : "")}):\n{sb.ToString().TrimEnd()}";
        });
    }

    private static (bool breakWhenThrown, bool breakWhenUserUnhandled) ParseBreakMode(string breakMode)
    {
        return breakMode.ToLowerInvariant() switch
        {
            "always" => (true, true),
            "user-unhandled" => (false, true),
            "never" => (false, false),
            _ => throw new ArgumentException($"Invalid break mode: '{breakMode}'. Use 'always', 'user-unhandled', or 'never'.")
        };
    }
}
