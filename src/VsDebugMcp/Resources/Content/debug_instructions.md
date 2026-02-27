# Visual Studio Debugger - Instructions for Claude Code

This guide covers how to use the VsDebugMcp tools to control Visual Studio's debugger through Claude Code. The server communicates with Visual Studio via COM automation (EnvDTE), giving you full control over debugging sessions.

## Available Tools

| Tool | Description |
|---|---|
| `start_debugging` | Start debugging (F5) with optional build configuration |
| `stop_debugging` | Stop the current debug session (Shift+F5) |
| `restart_debugging` | Restart debugging (Ctrl+Shift+F5) |
| `step_over` | Step over the current line (F10) |
| `step_into` | Step into a function call (F11) |
| `step_out` | Step out of the current function (Shift+F11) |
| `continue_execution` | Continue running until next breakpoint (F5) |
| `add_breakpoint` | Set a breakpoint at a file and line, with optional condition |
| `remove_breakpoint` | Remove a breakpoint at a file and line |
| `clear_all_breakpoints` | Remove all breakpoints |
| `list_breakpoints` | List all active breakpoints |
| `get_variables_values` | Inspect local variables in the current stack frame |
| `evaluate_expression` | Evaluate an expression in the debugger context |

## Starting and Stopping Debug Sessions

### Start Debugging

```
Tool: start_debugging
configuration: "Debug"   (optional, defaults to active configuration)
```

This builds the project (if needed) and launches it under the debugger. Equivalent to pressing F5 in Visual Studio. The debugger must be in Design mode (not already debugging).

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

### Managing Breakpoints

```
Tool: list_breakpoints        -- see all breakpoints with file, line, and conditions
Tool: remove_breakpoint       -- remove a specific breakpoint by file and line
Tool: clear_all_breakpoints   -- remove all breakpoints at once
```

## Stepping Through Code

When the debugger is paused (Break mode), you can step through code:

- **Step Over** (`step_over`): Execute the current line and move to the next one. If the line contains a function call, the function runs to completion without pausing inside it.
- **Step Into** (`step_into`): If the current line calls a function, enter that function and pause at its first line. Otherwise, behaves like Step Over.
- **Step Out** (`step_out`): Run the rest of the current function and pause at the line after the call site in the caller.
- **Continue** (`continue_execution`): Resume execution until the next breakpoint is hit or the program ends.

### Typical Stepping Workflow

1. Set a breakpoint at a suspicious line.
2. Start debugging.
3. When the breakpoint hits, inspect variables.
4. Step over lines to watch values change.
5. Step into a function if you suspect the bug is inside it.
6. Step out when you've seen enough of a function.

## Inspecting Variables and Expressions

### Get Local Variables

```
Tool: get_variables_values
depth: 2    (optional, default: 1)
```

Returns all local variables and method arguments in the current stack frame. The `depth` parameter controls how deep to expand nested objects:
- `depth: 1` -- shows property names and values for the top level only
- `depth: 2` -- expands one level of nested objects
- Higher values show deeper nesting but produce more output

### Evaluate an Expression

```
Tool: evaluate_expression
expression: "myList.Count"
```

Evaluates any valid expression in the current scope. Examples:
- `myObject.Property` -- access a property
- `x + y * 2` -- arithmetic
- `string.Join(", ", items)` -- call static methods
- `(MyType)baseObject` -- cast expressions
- `$"Name: {person.Name}"` -- interpolated strings

**Note:** Expression evaluation can have side effects. Calling methods that modify state (e.g., `list.Add(item)`) will actually modify the running program.

## Common Debugging Workflows

### Finding a Null Reference Exception

1. Read the exception message to identify the file and approximate location.
2. Set a breakpoint a few lines before the crash site.
3. Start debugging and wait for the breakpoint.
4. Use `get_variables_values` to check which variables are null.
5. Step over each line, checking variables after each step.
6. When you find the null value, trace back where it should have been assigned.

### Debugging a Loop

1. Set a conditional breakpoint inside the loop: `add_breakpoint` with `condition: "i == 50"`.
2. Start debugging. The loop runs freely until the condition matches.
3. Inspect variables at that iteration.
4. Step over a few iterations to observe how state changes.

### Investigating Unexpected Return Values

1. Set a breakpoint at the first line of the method.
2. Step through the method, evaluating key expressions at each decision point.
3. Before the return statement, use `evaluate_expression` to check the return value.

### Comparing Expected vs Actual Values

Use `evaluate_expression` to check multiple conditions:
```
expression: "expected == actual"
expression: "actual.GetType().Name"
expression: "expected.ToString() + \" vs \" + actual.ToString()"
```

## Tips for Effective Debugging

1. **Start with breakpoints, not stepping.** Set breakpoints at key locations rather than stepping line-by-line from the start.
2. **Use conditional breakpoints** to skip irrelevant iterations in loops.
3. **Check state before acting.** Call `get_variables_values` before stepping to understand the current context.
4. **Evaluate expressions liberally.** It is cheaper to evaluate an expression than to step through code to find a value.
5. **Remove breakpoints when done.** Use `clear_all_breakpoints` after resolving an issue to keep the session clean.
6. **Restart after code changes.** If you modify source code, restart debugging to pick up the changes.

## Important Notes

- The debugger controls Visual Studio via COM automation (EnvDTE). This means all operations happen in the actual Visual Studio instance running on the machine.
- Stepping and breakpoint commands only work when the debugger is in **Break mode** (paused at a breakpoint or step).
- Session commands (`start_debugging`, `stop_debugging`) only work when the debugger is in the appropriate state (Design mode for start, debugging for stop).
- If Visual Studio has multiple instances open, use the `--vs-pid` argument when starting VsDebugMcp to target a specific instance.
