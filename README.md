# VsDebugMcp

**MCP server that gives AI assistants full control of the Visual Studio debugger.**

[![GitHub release](https://img.shields.io/github/v/release/ViniciusGomes-eSolution/VsDebugMcp)](https://github.com/ViniciusGomes-eSolution/VsDebugMcp/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](https://github.com/ViniciusGomes-eSolution/VsDebugMcp)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/github/license/ViniciusGomes-eSolution/VsDebugMcp)](LICENSE)

---

VsDebugMcp is a [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that connects AI assistants -- such as Claude Code -- directly to the Visual Studio debugger. It exposes **47 tools** across 7 categories, giving the AI the ability to start and stop debug sessions, set breakpoints, step through code, inspect variables, evaluate expressions, attach to processes, manage exceptions, and more.

Under the hood, VsDebugMcp uses COM automation (EnvDTE) to communicate with Visual Studio through the Windows Running Object Table (ROT). A dedicated STA thread handles all COM marshaling, ensuring thread-safe interaction with the IDE. The result is that your AI assistant can operate the debugger with the same precision a developer would using the Visual Studio UI -- but programmatically and at conversation speed.

The server ships as a **single self-contained Windows executable**. No .NET runtime installation is required. Download the exe, register it with your MCP client, and start debugging with AI.

## Quick Start

**1. Download** the latest `VsDebugMcp.exe` from [Releases](https://github.com/ViniciusGomes-eSolution/VsDebugMcp/releases/latest).

**2. Register** with Claude Code:

```bash
claude mcp add vs-debugger -- "C:\path\to\VsDebugMcp.exe"
```

**3. Open** a solution in Visual Studio 2022 or 2026.

**4. Ask Claude** to debug:

> "Set a breakpoint at line 42 in Program.cs and start debugging"

Claude will use the MCP tools to control Visual Studio's debugger directly.

## Features

| Category | Tools | Capabilities |
|---|---|---|
| **Session Management** | 8 | Start, stop, restart debugging; Edit and Continue; query debug state; list build configurations; discover and switch between VS instances |
| **Execution Control** | 6 | Step over, step into, step out; continue execution; set next statement; run to cursor |
| **Breakpoints** | 9 | Add, remove, toggle, list breakpoints; batch add; conditional breakpoints; tracepoints with message formatting; hit count breakpoints; data breakpoints |
| **Inspection** | 16 | Get locals, autos, return values; evaluate single or multiple expressions; call stack and thread inspection; stack frame navigation; watch management; search variables; loaded modules; debug output |
| **Process Management** | 3 | Attach to and detach from running processes; list available processes with filtering |
| **Exception Handling** | 3 | Configure break behavior per exception type; list exception settings; get full exception chains with inner exceptions |
| **Navigation** | 2 | Execute Immediate Window commands (with side effects); navigate to source file and line |

Plus **5 MCP resources** providing debugging instructions and language-specific troubleshooting guides for C#, C++, F#, and VB.NET.

## Installation

### Option 1: Claude Code CLI

```bash
claude mcp add vs-debugger -- "C:\path\to\VsDebugMcp.exe"
```

### Option 2: Project Configuration (.mcp.json)

Create a `.mcp.json` file in your project root:

```json
{
  "mcpServers": {
    "vs-debugger": {
      "command": "C:\\path\\to\\VsDebugMcp.exe",
      "args": [],
      "transport": "stdio"
    }
  }
}
```

### Option 3: User Settings (settings.json)

Add to your Claude Code settings (`~/.claude/settings.json`):

```json
{
  "mcpServers": {
    "vs-debugger": {
      "command": "C:\\path\\to\\VsDebugMcp.exe",
      "transport": "stdio"
    }
  }
}
```

## Multi-Instance Support

When multiple Visual Studio instances are running, use the `--vs-pid` flag to target a specific one:

```bash
claude mcp add vs-debugger -- "C:\path\to\VsDebugMcp.exe" --vs-pid 12345
```

Or in `.mcp.json`:

```json
{
  "mcpServers": {
    "vs-debugger": {
      "command": "C:\\path\\to\\VsDebugMcp.exe",
      "args": ["--vs-pid", "12345"],
      "transport": "stdio"
    }
  }
}
```

To find the Visual Studio process ID:

```powershell
Get-Process devenv | Select-Object Id, MainWindowTitle
```

You can also switch instances at runtime using the `list_vs_instances` and `switch_vs_instance` tools -- no restart required.

## Configuration

All options are configurable via environment variables:

| Environment Variable | Default | Description |
|---|---|---|
| `VSDEBUGMCP_EXPRESSION_TIMEOUT_MS` | 500 | Timeout for expression evaluation (milliseconds) |
| `VSDEBUGMCP_COM_TIMEOUT_MS` | 10000 | Timeout for COM operations on the STA thread (milliseconds) |
| `VSDEBUGMCP_MAX_VARIABLE_DEPTH` | 5 | Maximum depth for expanding nested objects |
| `VSDEBUGMCP_MAX_OUTPUT_LINES` | 100 | Maximum lines returned from the Debug Output window |
| `VSDEBUGMCP_RESTART_DELAY_MS` | 500 | Delay between stop and start during a restart operation |
| `VSDEBUGMCP_STEP_CONTEXT_LINES` | 3 | Number of surrounding source lines returned after a step |
| `VSDEBUGMCP_STEP_MAX_LOCALS` | 15 | Maximum local variables returned after a step |

## Tool Reference

<details>
<summary>All 47 tools with descriptions</summary>

### Session Management (8 tools)

| Tool | Description |
|---|---|
| `start_debugging` | Start debugging the current project. Optionally specify a build configuration. Supports interactive configuration selection via elicitation. |
| `stop_debugging` | Stop the current debugging session. |
| `restart_debugging` | Restart the current debugging session (stop and start again). |
| `edit_and_continue` | Apply code changes while debugging without restarting. Requires break mode. Asks for confirmation via elicitation. |
| `get_debug_state` | Get the current debugger state: mode (Design/Run/Break), current file, line, function, solution name, and breakpoint count. |
| `list_configurations` | List all available build configurations (e.g. Debug, Release) and which is currently active. |
| `list_vs_instances` | List all running Visual Studio instances with PID, version, solution name, and connection status. |
| `switch_vs_instance` | Switch to a different running Visual Studio instance by PID or interactive selection. |

### Execution Control (6 tools)

| Tool | Description |
|---|---|
| `step_over` | Step over the current line (execute without entering called functions). |
| `step_into` | Step into the current line (enter the called function). |
| `step_out` | Step out of the current function (continue until it returns). |
| `continue_execution` | Continue execution until the next breakpoint or program end. |
| `set_next_statement` | Move the execution pointer to a specific line. Allows skipping or re-executing code. |
| `run_to_cursor` | Execute until reaching a specific file and line, like a temporary breakpoint. |

### Breakpoints (9 tools)

| Tool | Description |
|---|---|
| `add_breakpoint` | Add a breakpoint at a file and line. Optionally set a condition expression. |
| `add_breakpoints_batch` | Add multiple breakpoints in a single operation. Each can have its own condition. |
| `remove_breakpoint` | Remove a breakpoint at a file and line. |
| `toggle_breakpoint` | Enable or disable a breakpoint without removing it. |
| `clear_all_breakpoints` | Remove all breakpoints. Asks for confirmation via elicitation. |
| `list_breakpoints` | List all breakpoints currently set in the session. |
| `add_tracepoint` | Add a logging breakpoint that outputs a message when hit. Supports `{variable}`, `$CALLER`, `$CALLSTACK`, `$FUNCTION` placeholders. |
| `set_hit_count_breakpoint` | Add a breakpoint that triggers based on hit count (equal, greaterOrEqual, or multiple). |
| `add_data_breakpoint` | Add a data breakpoint that triggers when a variable's value changes. |

### Inspection (16 tools)

| Tool | Description |
|---|---|
| `get_variables_values` | Get all local variables and arguments in the current stack frame. Configurable expansion depth. |
| `evaluate_expression` | Evaluate a single expression in the current stack frame context. |
| `evaluate_multiple` | Evaluate multiple expressions in one call. More reliable and efficient than multiple single evaluations. |
| `get_current_location` | Get the current execution location: file path, line number, function name, and module. |
| `get_exception_info` | Get current exception details: type, message, stack trace, and inner exception. |
| `get_call_stack` | Get the full call stack of the active thread. |
| `get_threads` | List all threads with ID, name, and state. Marks the active thread. |
| `switch_stack_frame` | Switch to a different stack frame by index to inspect variables at that level. |
| `get_output` | Read the Debug Output window contents (last N lines). |
| `add_watch` | Add a persistent watch expression evaluated on each debug step. |
| `remove_watch` | Remove a previously added watch expression. |
| `list_watches` | List all watch expressions and their current values. |
| `get_loaded_modules` | List loaded modules (DLLs/assemblies) with optional name filtering. |
| `search_variables` | Search locals and arguments by name or value pattern with recursive depth. |
| `get_autos` | Get automatically relevant variables, similar to the VS Autos window. |
| `get_return_value` | Get the return value of the last executed function call (`$ReturnValue`). |

### Process Management (3 tools)

| Tool | Description |
|---|---|
| `attach_to_process` | Attach the debugger to a running process by PID or name. Supports interactive selection via elicitation. |
| `detach_from_process` | Detach the debugger from all attached processes without stopping them. |
| `list_processes` | List running processes available for debugging with optional name filtering. |

### Exception Handling (3 tools)

| Tool | Description |
|---|---|
| `manage_exception_settings` | Configure break behavior for a specific exception type: always, user-unhandled, or never. |
| `list_exception_settings` | List exception settings with optional filtering by namespace or type. |
| `get_exception_chain` | Get the complete exception chain including all inner exceptions with stack traces. |

### Navigation (2 tools)

| Tool | Description |
|---|---|
| `execute_immediate_command` | Execute a command in the Immediate Window context. Can modify variables, call methods, and change program state. |
| `navigate_to_source` | Open a source file in Visual Studio and navigate to a specific line. |

</details>

## Resources

VsDebugMcp provides 5 MCP resources with debugging guidance:

| Resource URI | Description |
|---|---|
| `debug://instructions` | General debugging workflow guide for using the Visual Studio debugger through an AI assistant |
| `debug://troubleshoot/csharp` | C# specific debugging tips and troubleshooting |
| `debug://troubleshoot/cpp` | C++ specific debugging tips and troubleshooting |
| `debug://troubleshoot/fsharp` | F# specific debugging tips and troubleshooting |
| `debug://troubleshoot/vb` | Visual Basic .NET specific debugging tips and troubleshooting |

## How It Works

```
MCP Client (Claude Code, etc.)
    |
    |  JSON-RPC over stdio
    v
VsDebugMcp.exe  (MCP Server)
    |
    |  COM / EnvDTE via dedicated STA thread
    v
devenv.exe  (Visual Studio)
```

1. The MCP client sends a JSON-RPC request over stdin (e.g., "add a breakpoint").
2. VsDebugMcp routes the request to the appropriate tool handler.
3. The tool handler calls a debug service, which marshals the operation to a dedicated STA thread.
4. The STA thread executes the COM call against Visual Studio's DTE (Development Tools Environment) automation object.
5. The result flows back through the same chain to the MCP client.

Visual Studio instances are discovered via the Windows **Running Object Table** (ROT), which is a system-wide registry of running COM objects. VsDebugMcp locates entries matching the pattern `!VisualStudio.DTE.<version>:<pid>` and connects to the target instance. If Visual Studio is closed and reopened, VsDebugMcp reconnects automatically on the next operation.

## Requirements

- **Windows 10 or 11** -- COM automation requires Windows
- **Visual Studio 2022** (17.x) or **Visual Studio 2026** (18.x) -- Community, Professional, or Enterprise
- An **MCP-compatible client** -- such as [Claude Code](https://docs.anthropic.com/en/docs/claude-code), Cursor, or any client supporting the Model Context Protocol

No .NET runtime installation is required. The executable is fully self-contained.

## Building from Source

```bash
git clone https://github.com/ViniciusGomes-eSolution/VsDebugMcp.git
cd VsDebugMcp
dotnet publish src/VsDebugMcp -c Release
```

The output is a single self-contained executable at:

```
src/VsDebugMcp/bin/Release/net8.0-windows/win-x64/publish/VsDebugMcp.exe
```

To run the test suite:

```bash
dotnet test
```

## Documentation

- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** -- Component diagram, threading model, COM automation details, and project structure
- **[SETUP.md](docs/SETUP.md)** -- Detailed installation, configuration, and troubleshooting guide
