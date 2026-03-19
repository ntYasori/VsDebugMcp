# VsDebugMcp Architecture

## Overview

VsDebugMcp is a Model Context Protocol (MCP) server that exposes Visual Studio's debugger as a set of tools and resources for Claude Code. It bridges the gap between an AI assistant and a full-featured IDE debugger.

## High-Level Component Diagram

```
+-------------------+          stdio           +--------------------+
|                   |  (JSON-RPC over stdin/   |                    |
|   Claude Code     |   stdout)                |   VsDebugMcp.exe   |
|   (MCP Client)    | <---------------------> |   (MCP Server)      |
|                   |                          |                    |
+-------------------+                          +--------+-----------+
                                                        |
                                                        | COM / EnvDTE
                                                        | (STA Thread)
                                                        |
                                               +--------v-----------+
                                               |                    |
                                               |   devenv.exe       |
                                               |   (Visual Studio)  |
                                               |                    |
                                               +--------------------+
```

## Communication Flow

```
User Request (e.g., "set breakpoint at line 42")
    |
    v
Claude Code interprets intent and calls MCP tool
    |
    v
JSON-RPC request over stdin --> VsDebugMcp.exe
    |
    v
MCP Server routes to tool handler (e.g., BreakpointTools.AddBreakpoint)
    |
    v
Tool calls IBreakpointDebugService.AddBreakpointAsync()
    |
    v
Service marshals to ComThread (STA) via DteConnector
    |
    v
ComThread executes EnvDTE call on DTE.Debugger
    |
    v
Visual Studio processes the command
    |
    v
Result flows back: COM --> ComThread --> Service --> Tool --> JSON-RPC --> Claude Code
```

## Key Components

### Program.cs (Entry Point)

Configures the .NET Generic Host with:
- `DebuggerOptions` -- configurable timeouts and limits (overridable via environment variables).
- `IRotHelper` / `RotHelper` -- Running Object Table enumeration (injectable for testability).
- `ComThread` -- singleton STA thread for COM interop with configurable timeout.
- `DteConnector` -- manages the connection to a Visual Studio instance.
- Four debug services implementing the Interface Segregation Principle:
  - `ISessionDebugService` -- session lifecycle, state, and configurations.
  - `IBreakpointDebugService` -- breakpoint management.
  - `IExecutionDebugService` -- stepping and execution control.
  - `IInspectionDebugService` -- variable inspection, expression evaluation, call stack, threads, watches, and output.
- MCP server with stdio transport, tools, and resources discovered from the assembly.

Parses `--vs-pid` from command-line arguments for multi-instance targeting.

### COM Automation Approach

#### Running Object Table (ROT)

Visual Studio registers its DTE (Development Tools Environment) COM object in the Windows Running Object Table. The ROT is a system-wide registry of running COM objects identified by monikers.

`RotHelper` (implementing `IRotHelper`) enumerates ROT entries matching the pattern `!VisualStudio.DTE.<version>:<pid>`, extracting the version and process ID. This allows:
- **Auto-discovery:** Find any running VS instance (picks the newest version).
- **PID targeting:** Connect to a specific instance when multiple are running.

#### DTE Interface (EnvDTE)

The `EnvDTE.DTE` interface is Visual Studio's top-level automation object. Through it, VsDebugMcp accesses:
- `DTE.Debugger` -- debug session control, stepping, breakpoints.
- `DTE.Debugger.Breakpoints` -- breakpoint management.
- `DTE.Debugger.CurrentStackFrame` -- current execution context.
- `DTE.Debugger.GetExpression()` -- expression evaluation.

The project references the EnvDTE and EnvDTE80 type libraries via COM references in the .csproj.

#### DteConnector

Wraps the DTE object with:
- **Lazy connection:** Connects on first use via `ConnectAsync()`.
- **Health checking:** `EnsureConnectedAsync()` verifies the COM link is alive before each operation. If VS was closed and reopened, it reconnects automatically.
- **Typed access:** `ExecuteOnDteAsync<T>()` provides strongly-typed DTE access with automatic marshaling to the STA thread.
- **Logging:** Logs connection, reconnection, and health check events.

### Thread Model

```
+---------------------------+       +---------------------------+
|   MCP Server Threads      |       |   COM STA Thread          |
|   (ThreadPool / async)    |       |   ("VsDebugMcp-STA")      |
|                           |       |                           |
|   Tool handlers run here  |       |   All EnvDTE calls run    |
|   on async threadpool     | ----> |   here via ComThread      |
|   threads.                |       |   work queue.             |
|                           |       |                           |
|   Cannot call COM         |       |   Single-threaded.        |
|   directly (wrong         |       |   Processes one work      |
|   apartment).             |       |   item at a time.         |
+---------------------------+       +---------------------------+
```

**Why STA?** COM objects like EnvDTE require Single-Threaded Apartment (STA) access. The .NET ThreadPool uses MTA threads. `ComThread` bridges this gap by maintaining a dedicated STA thread with a `BlockingCollection<Action>` work queue.

**Timeout behavior:** Each COM operation has a configurable timeout (default 10s). When a timeout occurs, the caller receives a `TimeoutException`, but the actual COM work may still be executing on the STA thread (e.g., VS is showing a modal dialog). Subsequent operations queue behind it until the STA thread is free.

**Flow:**
1. An MCP tool handler (on a ThreadPool thread) calls a debug service (e.g., `IBreakpointDebugService`).
2. The service calls `DteConnector.ExecuteOnDteAsync()`.
3. `DteConnector` posts the work to `ComThread` via `RunAsync<T>()`.
4. The STA thread executes the COM call and completes the `TaskCompletionSource`.
5. The async continuation resumes on the ThreadPool.

### Debug Services

Services are organized by the Interface Segregation Principle -- each tool class depends only on the interface it needs:

| Interface | Implementation | Purpose |
|---|---|---|
| `ISessionDebugService` | `SessionDebugService` | Session lifecycle, state, configurations |
| `IBreakpointDebugService` | `BreakpointDebugService` | Breakpoint management |
| `IExecutionDebugService` | `ExecutionDebugService` | Stepping and execution control |
| `IInspectionDebugService` | `InspectionDebugService` | Variables, expressions, call stack, threads, watches, output |

Shared logic (break mode validation, location formatting, expression tree formatting) lives in `DebuggerHelpers`.

### MCP Protocol Integration

#### Tools

Tools are organized by category into static classes decorated with `[McpServerToolType]`:

| Class | Tools | Purpose |
|---|---|---|
| `SessionTools` | start_debugging, stop_debugging, restart_debugging, edit_and_continue, get_debug_state, list_configurations, list_vs_instances, switch_vs_instance | Debug session lifecycle and VS instance management |
| `ExecutionTools` | step_over, step_into, step_out, continue_execution, set_next_statement, run_to_cursor | Code stepping and execution |
| `BreakpointTools` | add_breakpoint, add_breakpoints_batch, remove_breakpoint, toggle_breakpoint, clear_all_breakpoints, list_breakpoints | Breakpoint management |
| `InspectionTools` | get_variables_values, evaluate_expression, evaluate_multiple, get_current_location, get_exception_info, get_call_stack, get_threads, switch_stack_frame, get_output, add_watch, remove_watch, list_watches | Variable and expression inspection |

Tools receive their specific debug service interface via dependency injection (the MCP SDK resolves constructor/method parameters from the DI container).

#### Resources

Resources are static content served via MCP resource URIs:

| URI | Content |
|---|---|
| `debug://instructions` | General debugging workflow guide |
| `debug://troubleshoot/csharp` | C# debugging tips |
| `debug://troubleshoot/cpp` | C++ debugging tips |
| `debug://troubleshoot/fsharp` | F# debugging tips |
| `debug://troubleshoot/vb` | VB.NET debugging tips |

Resource content is stored as embedded markdown files in the assembly (`Resources/Content/*.md`) and served by `DebugResources` using `Assembly.GetManifestResourceStream()`.

### Configurable Options

`DebuggerOptions` provides configurable parameters, overridable via environment variables:

| Option | Default | Env Var | Purpose |
|---|---|---|---|
| `ExpressionTimeoutMs` | 500 | `VSDEBUGMCP_EXPRESSION_TIMEOUT_MS` | Timeout for expression evaluation |
| `ComOperationTimeoutMs` | 10000 | `VSDEBUGMCP_COM_TIMEOUT_MS` | Timeout for COM operations on STA thread |
| `MaxVariableDepth` | 5 | `VSDEBUGMCP_MAX_VARIABLE_DEPTH` | Max depth for variable expansion |
| `MaxOutputLines` | 100 | `VSDEBUGMCP_MAX_OUTPUT_LINES` | Max lines from Debug Output window |
| `RestartDelayMs` | 500 | `VSDEBUGMCP_RESTART_DELAY_MS` | Delay between stop and start during restart |

### Error Handling Strategy

1. **COM Exceptions:** All COM calls are wrapped in try/catch. `COMException` typically means VS is busy, closed, or in an invalid state. The `DteConnector` attempts reconnection on the next call and logs all connection events.

2. **State Validation:** Debug services check the debugger mode (Design/Run/Break) before executing commands via `DebuggerHelpers.RequireBreakMode()`. Stepping commands require Break mode; start requires Design mode.

3. **Tool-Level Errors:** Tool handlers return error messages as strings (not exceptions) so Claude Code receives a useful description of what went wrong.

4. **Connection Loss:** If VS is closed, subsequent calls fail with a descriptive error. VsDebugMcp does not crash -- it reports the error and attempts to reconnect when VS is available again.

5. **Timeouts:** COM operations time out after a configurable period. The caller receives a `TimeoutException` with a descriptive message.

### Security Considerations

- **Local only.** VsDebugMcp communicates via stdio (stdin/stdout) with Claude Code and via COM with a local Visual Studio instance. There is no network exposure.
- **Same-user requirement.** COM ROT access is restricted to the same Windows user session and integrity level.
- **Expression evaluation.** The `evaluate_expression` tool can execute arbitrary code in the debuggee's context. This is inherent to debugger expression evaluation -- it is no different from using the Immediate Window in Visual Studio directly.
- **No credential handling.** VsDebugMcp does not store or transmit credentials. Authentication is handled by Windows COM security.
- **Process lifetime.** VsDebugMcp runs as a child process of Claude Code and terminates when the stdio pipe is closed.

## Project Structure

```
VsDebugMcp/
  VsDebugMcp.sln
  src/
    VsDebugMcp/
      Program.cs                          -- Entry point and DI configuration
      ComThread.cs                        -- STA thread for COM marshaling (with timeout)
      DebuggerOptions.cs                  -- Configurable options (timeouts, limits)
      VsDebugMcp.csproj                   -- Project file with COM references
      Debugger/
        ISessionDebugService.cs           -- Session lifecycle interface
        IBreakpointDebugService.cs        -- Breakpoint management interface
        IExecutionDebugService.cs         -- Execution control interface
        IInspectionDebugService.cs        -- Inspection and watch interface
        SessionDebugService.cs            -- Session implementation
        BreakpointDebugService.cs         -- Breakpoint implementation
        ExecutionDebugService.cs          -- Execution implementation
        InspectionDebugService.cs         -- Inspection implementation
        DebuggerHelpers.cs                -- Shared helpers (break mode, formatting)
        DebugState.cs                     -- Immutable debug state record
        BreakpointRequest.cs              -- Record for batch breakpoint operations
      Interop/
        IRotHelper.cs                     -- ROT abstraction (injectable)
        RotHelper.cs                      -- Running Object Table enumeration
        DteConnector.cs                   -- DTE connection management (with logging)
        NativeMethods.cs                  -- P/Invoke for ROT and COM APIs
        VsInstanceInfo.cs                 -- VS instance info record
      Tools/
        SessionTools.cs                   -- start/stop/restart, debug state, configurations (with elicitation)
        ExecutionTools.cs                 -- step over/into/out, continue, set next statement, run to cursor
        BreakpointTools.cs                -- add/remove/toggle/list/batch breakpoints (with elicitation)
        InspectionTools.cs                -- variables, expressions, call stack, threads, exceptions, output, watches
      Resources/
        DebugResources.cs                 -- MCP resource type definitions
        Content/
          debug_instructions.md           -- General debugging guide
          troubleshooting_csharp.md       -- C# tips
          troubleshooting_cpp.md          -- C++ tips
          troubleshooting_fsharp.md       -- F# tips
          troubleshooting_vb.md           -- VB.NET tips
  tests/
    VsDebugMcp.UnitTests/                 -- Unit tests (mockable interfaces)
    VsDebugMcp.IntegrationTests/          -- Integration tests (requires VS)
  docs/
    SETUP.md                              -- Installation and configuration
    ARCHITECTURE.md                       -- This document
```
