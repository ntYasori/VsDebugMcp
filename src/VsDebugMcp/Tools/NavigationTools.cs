using ModelContextProtocol.Server;
using System.ComponentModel;
using VsDebugMcp.Debugger;

namespace VsDebugMcp.Tools;

[McpServerToolType]
public sealed class NavigationTools
{
    [McpServerTool(Name = "execute_immediate_command"), Description("Execute a command in the debugger's Immediate Window context. Allows evaluating expressions with side effects — modifying variables, calling methods, etc. Unlike evaluate_expression, this can change program state. Requires break mode.")]
    public static async Task<string> ExecuteImmediateCommand(
        INavigationDebugService navigation,
        [Description("The command to execute (e.g. 'myVar = 42', 'obj.Reset()', 'System.Console.WriteLine(\"debug\")').")] string command)
    {
        return await navigation.ExecuteImmediateCommandAsync(command);
    }

    [McpServerTool(Name = "navigate_to_source"), Description("Open a source file in Visual Studio and navigate to a specific line. Useful after analysis to direct the developer's attention to relevant code.")]
    public static async Task<string> NavigateToSource(
        INavigationDebugService navigation,
        [Description("Full path to the source file.")] string filePath,
        [Description("Line number (1-based) to navigate to.")] int line)
    {
        return await navigation.NavigateToSourceAsync(filePath, line);
    }
}
