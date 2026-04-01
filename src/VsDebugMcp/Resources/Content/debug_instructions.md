# Visual Studio Debugger - Instructions for Claude Code

This guide covers how to use the VsDebugMcp tools to control Visual Studio's debugger through Claude Code. The server communicates with Visual Studio via COM automation (EnvDTE), giving you full control over debugging sessions, breakpoints, variable inspection, process attachment, exception handling, and more.

## Available Tools (47)

### Session Management (8 tools)

| Tool | Description |
|---|---|
| `start_debugging` | Start debugging (F5) with optional build configuration |
| `stop_debugging` | Stop the current debug session (Shift+F5) |
| `restart_debugging` | Restart debugging (Ctrl+Shift+F5) |
| `edit_and_continue` | Apply code changes while debugging without restarting |
| `get_debug_state` | Get debugger state: mode, file, line, function, breakpoint count |
| `list_configurations` | List available build configurations (Debug, Release, etc.) |
| `list_vs_instances` | List running VS instances with PID, version, and solution |
| `switch_vs_instance` | Switch to a different running VS instance by PID |

### Execution Control (6 tools)

| Tool | Description |
|---|---|
| `step_over` | Step over current line, executing without entering functions (F10) |
| `step_into` | Step into function call on current line (F11) |
| `step_out` | Step out of the current function (Shift+F11) |
| `continue_execution` | Continue until next breakpoint or program end (F5) |
| `set_next_statement` | Move execution pointer to a specific line (skip or re-execute code) |
| `run_to_cursor` | Execute until reaching a specific file and line (temporary breakpoint) |

### Breakpoints (10 tools)

| Tool | Description |
|---|---|
| `add_breakpoint` | Add breakpoint at file/line with optional condition |
| `add_breakpoints_batch` | Add multiple breakpoints at once (preferred over repeated add_breakpoint) |
| `remove_breakpoint` | Remove a breakpoint by file and line |
| `toggle_breakpoint` | Enable/disable a breakpoint without removing it |
| `clear_all_breakpoints` | Remove all breakpoints (cannot be undone) |
| `list_breakpoints` | List all breakpoints with file, line, conditions, and state |
| `add_tracepoint` | Add logging breakpoint with message placeholders |
| `set_hit_count_breakpoint` | Break based on hit count (equal, greaterOrEqual, multiple) |
| `add_data_breakpoint` | Break when a variable's value changes |

### Inspection (16 tools)

| Tool | Description |
|---|---|
| `get_variables_values` | Get locals and arguments in current stack frame (configurable depth) |
| `evaluate_expression` | Evaluate a single expression in current scope |
| `evaluate_multiple` | Evaluate multiple expressions at once (preferred over repeated evaluate_expression) |
| `get_current_location` | Get current file, line, function, and module with source context |
| `get_exception_info` | Get current exception type, message, and stack trace |
| `get_call_stack` | Get the full call stack with all frames |
| `get_threads` | List all threads with ID, name, and state |
| `switch_stack_frame` | Switch to a different stack frame by index |
| `get_output` | Read the VS Debug Output window (last 100 lines) |
| `add_watch` | Add a persistent watch expression |
| `remove_watch` | Remove a watch expression |
| `list_watches` | List all watches with current values |
| `get_loaded_modules` | List loaded DLLs/assemblies with optional name filter |
| `search_variables` | Regex search through variable names and values (recursive) |
| `get_autos` | Get auto-relevant variables: locals plus return value |
| `get_return_value` | Get $ReturnValue after stepping over/out of a function |

### Process Management (3 tools)

| Tool | Description |
|---|---|
| `attach_to_process` | Attach debugger to a running process by PID or name |
| `detach_from_process` | Detach from all processes without stopping them |
| `list_processes` | List processes available for attachment with optional filter |

### Exception Handling (3 tools)

| Tool | Description |
|---|---|
| `manage_exception_settings` | Configure break behavior per exception type (always/user-unhandled/never) |
| `list_exception_settings` | List exception break settings with optional filter |
| `get_exception_chain` | Get full inner exception chain with types, messages, and stack traces |

### Navigation (2 tools)

| Tool | Description |
|---|---|
| `execute_immediate_command` | Run command in Immediate Window (can modify state) |
| `navigate_to_source` | Open file in VS and navigate to a specific line |

## Starting and Stopping Debug Sessions

### Start Debugging

```
Tool: start_debugging
configuration: "Debug"   (optional, defaults to active configuration)
```

This builds the project (if needed) and launches it under the debugger. Equivalent to pressing F5 in Visual Studio. The debugger must be in Design mode (not already debugging). Use `list_configurations` to see available configurations before starting.

### Stop Debugging

```
Tool: stop_debugging
```

Terminates the debuggee process and returns to Design mode. Equivalent to Shift+F5.

### Restart Debugging

```
Tool: restart_debugging
```

Stops the current session and immediately starts a new one. Useful after code changes that require a fresh run.

### Edit and Continue

```
Tool: edit_and_continue
```

Applies code changes to the running process without restarting. The debugger must be in break mode. Not all changes are supported -- adding new classes or changing method signatures typically requires a full restart. This is useful for fixing bugs or adjusting logic while preserving program state.

### Check Debugger State

```
Tool: get_debug_state
```

Returns the current debugger mode (Design/Run/Break), solution name, active project, breakpoint count, current file, line, and function. Always check state before performing operations to ensure the debugger is in the correct mode.

## Multi-Instance Support

When multiple Visual Studio instances are open, you can list and switch between them:

```
Tool: list_vs_instances
```

Returns all running VS instances with PID, version, solution name, and which one is currently connected. Then switch:

```
Tool: switch_vs_instance
pid: 12345
```

Use `list_vs_instances` first to discover available PIDs. This is essential when working across multiple solutions simultaneously.

## Breakpoints

### Setting a Breakpoint

```
Tool: add_breakpoint
filePath: "C:\Projects\MyApp\Program.cs"
line: 42
```

Use full absolute paths. The line number is 1-based, matching what you see in the editor.

### Conditional Breakpoints

```
Tool: add_breakpoint
filePath: "C:\Projects\MyApp\Program.cs"
line: 42
condition: "i > 100"
```

The condition is a C#/C++/VB expression evaluated each time the breakpoint is hit. Execution only pauses when the condition is true. Useful for:
- Breaking only on specific iterations: `index == 99`
- Breaking on specific values: `customer.Name == "Contoso"`
- Breaking on null: `result == null`

### Batch Breakpoints

When you need to set multiple breakpoints, always prefer the batch operation:

```
Tool: add_breakpoints_batch
bps: [
  { "FilePath": "Program.cs", "Line": 10 },
  { "FilePath": "Program.cs", "Line": 25, "Condition": "x > 0" },
  { "FilePath": "Service.cs", "Line": 42 }
]
```

This is more efficient than calling `add_breakpoint` multiple times and completes in a single round-trip.

### Tracepoints (Logging Breakpoints)

Tracepoints log a message when hit and optionally continue execution without breaking:

```
Tool: add_tracepoint
filePath: "C:\Projects\MyApp\Program.cs"
line: 42
message: "Value of x is {x}, caller: $CALLER"
continueExecution: true
```

Supported placeholders in the message:
- `{variableName}` -- inserts the value of a variable
- `$CALLER` -- inserts the calling function name
- `$CALLSTACK` -- inserts the full call stack
- `$FUNCTION` -- inserts the current function name

By default, tracepoints continue execution after logging (`continueExecution: true`). Set to `false` to break after logging the message. Tracepoints are invaluable for non-intrusive logging without modifying source code.

### Hit Count Breakpoints

Break based on how many times a line is reached:

```
Tool: set_hit_count_breakpoint
filePath: "C:\Projects\MyApp\Program.cs"
line: 42
hitCount: 100
hitCountType: "equal"
```

Hit count types:
- `"equal"` -- break when the hit count equals N (e.g., break on the 100th iteration)
- `"greaterOrEqual"` -- break when hit count reaches N or more
- `"multiple"` -- break every Nth hit (e.g., every 10th iteration)

### Data Breakpoints

Break when a variable's value changes, without specifying a line:

```
Tool: add_data_breakpoint
expression: "myObject.Property"
```

Supported in C++ native code and .NET Core 3.0+. This is extremely useful for tracking down where a value gets unexpectedly modified.

### Managing Breakpoints

```
Tool: list_breakpoints        -- see all breakpoints with file, line, conditions, and enabled state
Tool: remove_breakpoint       -- remove a specific breakpoint by file and line
Tool: toggle_breakpoint       -- enable/disable a breakpoint without removing it
Tool: clear_all_breakpoints   -- remove all breakpoints at once
```

Use `toggle_breakpoint` to temporarily disable breakpoints you may need again later rather than removing and re-adding them.

## Stepping Through Code

When the debugger is paused (Break mode), you can step through code. Step operations return source context and local variable values along with the new position, so you get immediate visibility into the program state.

- **Step Over** (`step_over`): Execute the current line and move to the next one. If the line contains a function call, the function runs to completion without pausing inside it.
- **Step Into** (`step_into`): If the current line calls a function, enter that function and pause at its first line. Otherwise, behaves like Step Over.
- **Step Out** (`step_out`): Run the rest of the current function and pause at the line after the call site in the caller.
- **Continue** (`continue_execution`): Resume execution until the next breakpoint is hit or the program ends.

### Advanced Execution Control

- **Set Next Statement** (`set_next_statement`): Move the execution pointer to a specific line within the current method. This lets you skip code or re-execute lines without running them. Only works within the same method scope.

  ```
  Tool: set_next_statement
  line: 35
  ```

- **Run to Cursor** (`run_to_cursor`): Like a temporary breakpoint -- run until reaching a specific line, then stop. Useful for skipping ahead to a point of interest without setting a permanent breakpoint.

  ```
  Tool: run_to_cursor
  filePath: "C:\Projects\MyApp\Program.cs"
  line: 100
  ```

### Typical Stepping Workflow

1. Set a breakpoint at a suspicious line.
2. Start debugging.
3. When the breakpoint hits, inspect variables (the step response includes source context and locals automatically).
4. Step over lines to watch values change.
5. Step into a function if you suspect the bug is inside it.
6. Step out when you've seen enough of a function.
7. After stepping over or out of a function call, use `get_return_value` to see what the function returned.

## Inspecting Variables and Expressions

### Get Local Variables

```
Tool: get_variables_values
depth: 2    (optional, default: 1, max: 5)
```

Returns all local variables and method arguments in the current stack frame. The `depth` parameter controls how deep to expand nested objects:
- `depth: 1` -- shows property names and values for the top level only
- `depth: 2` -- expands one level of nested objects
- Higher values show deeper nesting but produce more output

### Evaluate Expressions

For a single expression:

```
Tool: evaluate_expression
expression: "myList.Count"
```

For multiple expressions, always prefer `evaluate_multiple` -- it is more reliable (avoids errors from parallel calls) and more efficient (single round-trip):

```
Tool: evaluate_multiple
expressions: ["myList.Count", "x + y * 2", "customer.Name", "items.Any()"]
```

Expression examples:
- `myObject.Property` -- access a property
- `x + y * 2` -- arithmetic
- `string.Join(", ", items)` -- call static methods
- `(MyType)baseObject` -- cast expressions
- `$"Name: {person.Name}"` -- interpolated strings

**Note:** Expression evaluation can have side effects. Calling methods that modify state (e.g., `list.Add(item)`) will actually modify the running program. Use `execute_immediate_command` when you intentionally want to modify state.

### Get Auto Variables

```
Tool: get_autos
```

Returns automatically relevant variables, similar to the Autos window in Visual Studio. Includes all locals in the current frame plus the return value of the last function call (if available).

### Get Return Value

```
Tool: get_return_value
```

After stepping over or out of a function call, this retrieves the `$ReturnValue` pseudo-variable. Useful for inspecting what a function returned without assigning it to a variable first.

### Search Variables

```
Tool: search_variables
namePattern: "count"
valuePattern: "null"
maxDepth: 3
```

Recursively searches all local variables and arguments for names or values matching regex patterns. Both `namePattern` and `valuePattern` are optional -- specify one or both. This is powerful for finding specific values deep in nested object graphs.

### Watches

Watches are persistent expressions that get re-evaluated on each debug step:

```
Tool: add_watch
expression: "myList.Count"

Tool: list_watches          -- shows all watches with current values

Tool: remove_watch
expression: "myList.Count"
```

Use watches to track key values across multiple steps without repeatedly calling evaluate. When the debugger is in break mode, `list_watches` evaluates each expression and shows the current result.

### Call Stack and Threads

```
Tool: get_call_stack         -- full call stack with all frames and indices
Tool: get_threads            -- all threads with ID, name, and state
Tool: switch_stack_frame
frameIndex: 2                -- switch to frame index 2 (0 = top/current)
```

Use `get_call_stack` to understand how execution reached the current point. Frame indices from the call stack can be passed to `switch_stack_frame` to inspect variables at different levels of the call hierarchy.

### Current Location

```
Tool: get_current_location
```

Returns the current execution position including file path, line number, function name, and module, along with surrounding source context. Use this after any operation to confirm where execution is.

### Debug Output

```
Tool: get_output
```

Reads the Visual Studio Debug Output window, showing the last 100 lines. Includes debug logs, console output, and diagnostic messages.

### Loaded Modules

```
Tool: get_loaded_modules
filter: "MyApp"              -- optional filter by name or path
```

Lists all loaded DLLs and assemblies in the current debug process. Useful for diagnosing assembly version conflicts or verifying that the correct module is loaded.

## Process Attachment

You can attach the debugger to already-running processes instead of launching through Start Debugging.

### List Available Processes

```
Tool: list_processes
filter: "dotnet"             -- optional name filter
```

### Attach to a Process

By PID (preferred when you know the exact process):

```
Tool: attach_to_process
pid: 12345
```

By name (searches for matching processes):

```
Tool: attach_to_process
processName: "MyApp"
```

If multiple processes match the name, you will be prompted to select one.

### Detach Without Stopping

```
Tool: detach_from_process
```

Detaches the debugger from all attached processes. The processes continue running independently. This is different from `stop_debugging`, which terminates the process.

## Exception Handling

### Configure Exception Break Behavior

```
Tool: manage_exception_settings
exceptionType: "System.NullReferenceException"
breakMode: "always"
```

Break modes:
- `"always"` -- break as soon as the exception is thrown (first-chance), even if it would be caught
- `"user-unhandled"` -- break only if the exception is not caught by user code
- `"never"` -- do not break for this exception type

### List Exception Settings

```
Tool: list_exception_settings
filter: "System.IO"          -- optional filter by namespace or type
```

Shows which exceptions are configured to break and their current mode.

### Get Full Exception Chain

```
Tool: get_exception_chain
```

When stopped at an exception, returns the complete chain of inner exceptions. Each entry shows the type, message, and stack trace. This is more thorough than `get_exception_info`, which shows only the top-level exception.

## Navigation and Immediate Window

### Execute Immediate Commands

```
Tool: execute_immediate_command
command: "myVariable = 42"
```

Runs a command in the Immediate Window context. Unlike `evaluate_expression`, this can modify program state:
- Assign variables: `myVariable = 42`
- Call methods with side effects: `obj.Reset()`
- Write to console: `System.Console.WriteLine("debug")`

Requires break mode.

### Navigate to Source

```
Tool: navigate_to_source
filePath: "C:\Projects\MyApp\Program.cs"
line: 42
```

Opens the specified file in Visual Studio and scrolls to the given line. Useful after analysis to direct the developer's attention to relevant code.

## Common Debugging Workflows

### Finding a Null Reference Exception

1. Use `manage_exception_settings` to break on `System.NullReferenceException` with `breakMode: "always"`.
2. Start debugging. The debugger breaks the instant the exception is thrown.
3. Use `get_exception_info` to read the exception message.
4. Use `get_call_stack` to see the full chain of calls.
5. Use `get_variables_values` to check which variables are null.
6. Use `search_variables` with `valuePattern: "null"` to find all null values in scope.
7. Trace back where the null value should have been assigned.

### Debugging a Loop

1. Use `set_hit_count_breakpoint` with `hitCountType: "equal"` and `hitCount: 50` to break at a specific iteration.
2. Alternatively, use a conditional breakpoint: `add_breakpoint` with `condition: "i == 50"`.
3. Start debugging. The loop runs freely until the condition matches.
4. Use `evaluate_multiple` to check several values at once: `["i", "items[i]", "sum", "items.Count"]`.
5. Step over a few iterations to observe how state changes.
6. Use `add_tracepoint` to log values on every iteration without breaking: `"Iteration {i}: sum={sum}"`.

### Investigating Unexpected Return Values

1. Set a breakpoint at the first line of the method.
2. Step through the method, using `evaluate_multiple` to check key expressions at each decision point.
3. After stepping over or out of the method call, use `get_return_value` to see what was returned.
4. Use `get_autos` for a quick view of locals plus the return value.

### Debugging Async Code

1. Set breakpoints at `await` expressions and inside the async methods.
2. Use `get_threads` to see all active threads and identify which thread is executing your code.
3. Use `get_call_stack` to trace through async frames -- note that async call stacks may show framework infrastructure frames.
4. Use `switch_stack_frame` to inspect variables at different levels of the async chain.
5. Use `add_tracepoint` at async boundaries to log execution flow without disrupting timing: `"Entering {$FUNCTION} on thread {System.Threading.Thread.CurrentThread.ManagedThreadId}"`.

### Exception Investigation

1. Configure the debugger to break on the exception type: `manage_exception_settings` with `breakMode: "always"`.
2. When the exception hits, use `get_exception_info` for the immediate details.
3. Use `get_exception_chain` to walk through all inner exceptions with their individual stack traces.
4. Use `get_call_stack` to see the context that led to the exception.
5. Use `list_exception_settings` to review which exceptions are configured to break.
6. After investigation, set the exception back to `"user-unhandled"` or `"never"` to avoid excessive breaking.

### Attaching to a Running Process

1. Use `list_processes` with a filter to find the target process: `filter: "MyApp"`.
2. Use `attach_to_process` with the PID or process name.
3. Set breakpoints in the relevant code. The debugger will break when those lines are reached.
4. When finished, use `detach_from_process` to release the process without stopping it.

### Comparing Expected vs Actual Values

Use `evaluate_multiple` to check several conditions at once:

```
Tool: evaluate_multiple
expressions: [
  "expected == actual",
  "actual.GetType().Name",
  "expected.ToString() + \" vs \" + actual.ToString()"
]
```

### Tracking Down State Mutation

1. Use `add_data_breakpoint` with the expression for the property you want to monitor.
2. Use `continue_execution`. The debugger will break whenever the value changes.
3. Use `get_call_stack` to see what code is modifying the value.
4. Use `get_variables_values` to inspect the new state.

## Tips for Effective Debugging

1. **Start with breakpoints, not stepping.** Set breakpoints at key locations rather than stepping line-by-line from the start.
2. **Use conditional breakpoints** to skip irrelevant iterations in loops.
3. **Use tracepoints for logging.** Add tracepoints to observe values over time without modifying source code or stopping execution.
4. **Prefer evaluate_multiple over evaluate_expression.** It evaluates all expressions in a single call, avoiding errors from parallel calls and reducing round-trips.
5. **Prefer add_breakpoints_batch for multiple breakpoints.** Setting several breakpoints in one call is faster and more reliable.
6. **Check state before acting.** Call `get_debug_state` before operations to verify the debugger is in the correct mode (Design/Run/Break).
7. **Use watches for persistent tracking.** Add watches for key values you want to monitor across multiple steps.
8. **Use search_variables to find specific values.** Instead of manually inspecting large objects, use regex patterns to search through variable names and values.
9. **Check return values after stepping.** After stepping over or out of a function, use `get_return_value` to see what it returned.
10. **Use edit_and_continue for quick fixes.** Modify code and apply changes without restarting, preserving program state.
11. **Remove breakpoints when done.** Use `clear_all_breakpoints` after resolving an issue to keep the session clean.
12. **Restart after major code changes.** If you modify source code beyond what Edit and Continue supports, restart debugging to pick up the changes.
13. **Use data breakpoints to track mutations.** When you cannot figure out where a value is being changed, set a data breakpoint on it.
14. **Detach instead of stopping** when debugging a running service you do not want to terminate.

## Important Notes

- The debugger controls Visual Studio via COM automation (EnvDTE). All operations happen in the actual Visual Studio instance running on the machine.
- Stepping and breakpoint commands only work when the debugger is in **Break mode** (paused at a breakpoint or step).
- Session commands (`start_debugging`, `stop_debugging`) only work when the debugger is in the appropriate state (Design mode for start, debugging for stop).
- Step operations (step_over, step_into, step_out) return source context and local variable values automatically, giving you immediate visibility into the new state.
- When multiple Visual Studio instances are open, use `list_vs_instances` and `switch_vs_instance` to target the correct one.
- Expression evaluation can have side effects. Use `evaluate_expression` or `evaluate_multiple` for read-only inspection. Use `execute_immediate_command` when you intentionally want to modify state.
- Data breakpoints are supported in C++ native code and .NET Core 3.0+ for certain scenarios.
- The Debug Output window (`get_output`) is limited to the last 100 lines.
