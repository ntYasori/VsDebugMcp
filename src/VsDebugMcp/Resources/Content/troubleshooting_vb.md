# Visual Basic .NET Debugging Tips and Troubleshooting

## Late Binding and Option Strict

### Debugging Late-Bound Calls

When `Option Strict Off` is used, VB.NET allows late binding -- calling methods and properties on `Object` references without compile-time type checking. At runtime, these use reflection and can fail with `MissingMemberException` or `InvalidCastException`.

To debug late-bound calls:
1. Set a breakpoint on the line with the late-bound call.
2. Inspect the actual runtime type:
   ```
   expression: "myObj.GetType().FullName"
   expression: "myObj.GetType().GetMembers().Select(Function(m) m.Name).ToArray()"
   ```
3. Verify the member exists and has the expected signature:
   ```
   expression: "myObj.GetType().GetMethod(\"MethodName\") IsNot Nothing"
   ```

### Option Strict Issues

If enabling `Option Strict On` breaks existing code, debug by checking implicit conversions:
```
expression: "myValue.GetType().Name"
expression: "TypeOf myValue Is TargetType"
expression: "CType(myValue, TargetType)"       ' test the conversion
```

Common problem areas:
- `Object` variables used where specific types are expected.
- Implicit narrowing conversions (e.g., `Double` to `Integer`).
- String-to-number comparisons.

## COM Interop Debugging

### Working with COM Objects

VB.NET is frequently used with COM interop (Office automation, legacy ActiveX components). When debugging COM calls:

1. **Check the object is valid:**
   ```
   expression: "myComObj Is Nothing"
   expression: "System.Runtime.InteropServices.Marshal.IsComObject(myComObj)"
   ```

2. **RCW (Runtime Callable Wrapper) issues:**
   If you see `InvalidComObjectException`, the COM object's RCW has been released:
   ```
   expression: "System.Runtime.InteropServices.Marshal.IsComObject(myComObj)"
   ```

3. **HRESULT errors:**
   COM methods return HRESULT values. When a `COMException` is thrown:
   ```
   expression: "ex.ErrorCode"            ' the HRESULT as Integer
   expression: "ex.ErrorCode.ToString(\"X8\")"   ' hex format for lookup
   expression: "ex.Message"
   ```

### Office Automation Debugging

When automating Excel, Word, or other Office applications:
- Each property access is a COM call -- expression evaluation can be slow.
- Office objects must be released in the correct order (child before parent).
- Watch for `Nothing` returns when accessing collections with non-existent keys.

```
expression: "xlApp.Workbooks.Count"
expression: "xlSheet.Range(\"A1\").Value"
expression: "xlSheet.Name"
```

## Legacy Code Patterns

### On Error Resume Next

`On Error Resume Next` suppresses all errors and continues execution on the next line. This makes debugging extremely difficult because:
- Errors are silently swallowed.
- `Err.Number` is the only indicator something went wrong.
- The code path continues as if nothing happened, but variables may have unexpected values.

**Debugging strategy:**
1. Set breakpoints at key points in the `On Error Resume Next` block.
2. After each suspicious line, check:
   ```
   expression: "Err.Number"              ' 0 = no error
   expression: "Err.Description"
   expression: "Err.Source"
   ```
3. If an error occurred, inspect the variables that should have been set:
   ```
   expression: "result"                  ' may still be its previous value
   ```

### On Error GoTo

For structured `On Error GoTo Label` patterns:
1. Set a breakpoint at the error handler label.
2. When it triggers, inspect `Err.Number` and `Err.Description`.
3. Check the `Resume` target to understand where execution continues.

### Converting Legacy Error Handling

When modernizing to `Try`/`Catch`:
- Add a `Try`/`Catch` block around the `On Error Resume Next` section.
- Set breakpoints in the `Catch` block to verify all errors are now caught.
- Compare `Err.Number` values with the caught exception types.

## WithEvents and Event Handling

### Debugging Event Handlers

VB.NET's `WithEvents` and `Handles` clause wires up events at compile time. To debug:

1. **Verify the event source is set:**
   ```
   expression: "_myObj"                  ' WithEvents backing field (underscore prefix)
   expression: "_myObj Is Nothing"       ' if True, events won't fire
   ```

2. **Set breakpoints in event handlers:**
   Set a breakpoint in the `Sub` that has the `Handles` clause. If it never hits:
   - The `WithEvents` variable may be `Nothing`.
   - The variable may have been reassigned (old handler detached, new handler attached).
   - The event may not be raised by the source.

3. **AddHandler/RemoveHandler:**
   For dynamically wired events, ensure handlers are added before the event fires:
   ```
   expression: "myObj"                   ' verify the object exists
   ```
   Set a breakpoint on the `AddHandler` line to confirm it runs before the event.

### Event Order Issues

When multiple handlers exist for the same event:
- `Handles` clause handlers run before `AddHandler` handlers.
- Multiple `AddHandler` calls run in the order they were added.
- Set breakpoints in each handler to verify execution order.

## Module-Level State Debugging

### Shared/Static State

VB.NET `Module` members and `Shared` members are global state. Debugging issues:

1. **Finding who modified a value:**
   - Search the codebase for all assignments to the module variable.
   - Set breakpoints on each assignment.
   - Run the program and see which breakpoint hits first/unexpectedly.

2. **Thread safety:**
   Module-level variables are shared across threads. If values change unexpectedly:
   ```
   expression: "System.Threading.Thread.CurrentThread.ManagedThreadId"
   expression: "MyModule.SharedValue"
   ```
   Check if concurrent threads are writing to the same variable.

3. **Initialization order:**
   Module-level variables are initialized when the module is first accessed. If a variable is unexpectedly `Nothing`:
   ```
   expression: "myModuleVar"
   expression: "myModuleVar Is Nothing"
   ```
   The initialization may not have run yet, or it may have thrown an exception silently.

### Static Local Variables

VB.NET supports `Static` local variables that persist across method calls:
```vb
Sub MyMethod()
    Static callCount As Integer = 0
    callCount += 1
End Sub
```

These are compiled to hidden class-level fields. In the debugger:
```
expression: "callCount"                  ' visible as a local variable
```

If the value seems wrong, remember it persists from previous calls -- it may have accumulated state from earlier in the session.
