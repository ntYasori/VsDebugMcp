# C# Debugging Tips and Troubleshooting

## Async/Await Debugging

### Task State Inspection

When paused at an `await`, use `evaluate_expression` to check the task state:

```
expression: "myTask.Status"           // RanToCompletion, Faulted, WaitingForActivation, etc.
expression: "myTask.Exception"        // null if no error, AggregateException if faulted
expression: "myTask.Result"           // WARNING: blocks if task isn't completed
```

**Caution:** Evaluating `.Result` on an incomplete task will deadlock the debugger. Always check `.Status` or `.IsCompleted` first.

### ConfigureAwait Behavior

When debugging code with `ConfigureAwait(false)`, the continuation may run on a different thread. After stepping over an `await`:
- Local variables are still visible (the compiler captures them in the state machine).
- `Thread.CurrentThread.ManagedThreadId` may differ from before the `await`.
- The call stack shows the state machine `MoveNext()` method, not the original method signature.

### Async Call Stacks

The Visual Studio debugger shows synthesized async call stacks. If you see `[Resuming Async Method]` frames, these are reconstructed from the state machine -- not real stack frames. Use `get_variables_values` to see the captured local variables.

### Deadlock Diagnosis

If the application hangs at an `await`:
1. Stop debugging and set a breakpoint on the line with `await`.
2. Restart and examine the task: `evaluate_expression("myTask.Status")`.
3. If the task is `WaitingForActivation`, the work hasn't started yet -- check whether the producing code is blocked on the same synchronization context.
4. Look for `.Result` or `.Wait()` calls higher in the call stack -- these are the most common cause of async deadlocks.

## LINQ Debugging

### Breaking Down Complex Queries

LINQ chains are hard to debug because they execute lazily as a single expression. Break them apart:

Instead of debugging:
```csharp
var result = items.Where(x => x.IsActive).Select(x => x.Name).OrderBy(n => n).ToList();
```

Set breakpoints and inspect intermediate results:
```
expression: "items.Where(x => x.IsActive).ToList()"
expression: "items.Where(x => x.IsActive).Select(x => x.Name).ToList()"
```

**Note:** Calling `.ToList()` in the evaluator forces materialization, which consumes the enumerator. This is safe for `ICollection`-backed sources but be cautious with one-shot streams or database queries (EF will execute the SQL).

### Lambda Breakpoints

You cannot set a breakpoint inside a lambda in the `add_breakpoint` tool. Instead:
1. Extract the lambda into a named method.
2. Set a breakpoint in that method.
3. Or use a conditional breakpoint on the line containing the LINQ expression.

## Null Reference Debugging

### Strategies for Finding Null Sources

1. **Check the full chain:** For `a.B.C.D`, evaluate each segment:
   ```
   expression: "a"
   expression: "a.B"
   expression: "a.B.C"
   ```
   The first one returning null is your culprit.

2. **Check method parameters:** Use `get_variables_values` at the start of a method to see if any parameter arrived as null.

3. **Check collection items:** A non-null collection can contain null elements:
   ```
   expression: "myList.Any(x => x == null)"
   expression: "myList.FindIndex(x => x == null)"
   ```

### Nullable Reference Types

When debugging with nullable reference types enabled (`<Nullable>enable</Nullable>`):
- Compiler warnings do not prevent nulls at runtime. External data, deserialization, and reflection can still produce nulls.
- Use `evaluate_expression` to check `object?.Property` patterns and see the actual runtime values.

## Exception Handling

### Inspecting Caught Exceptions

When paused inside a `catch` block, the exception variable is a local:
```
expression: "ex.Message"
expression: "ex.StackTrace"
expression: "ex.InnerException?.Message"
expression: "ex.GetType().FullName"
```

### First-Chance vs. Handled Exceptions

By default, the debugger only breaks on unhandled exceptions. If you need to catch an exception before it's handled:
1. In Visual Studio, use Debug > Windows > Exception Settings.
2. Enable "Break when thrown" for the exception type.
3. The debugger will pause at the `throw` statement, before any `catch` block runs.

VsDebugMcp does not currently control exception settings directly, so configure these in the Visual Studio UI.

## Generic Type Inspection

When debugging generic types, the evaluator uses concrete types:
```
expression: "myList.GetType().GenericTypeArguments[0].Name"   // "String", "Int32", etc.
```

For generic methods, check type parameters:
```
expression: "typeof(T).Name"    // only works if T is in scope (it usually is in the state machine)
```

## Property Getters with Side Effects

**Warning:** The debugger's expression evaluator calls property getters. If a getter has side effects (logging, incrementing counters, lazy initialization), evaluating the property will trigger those effects.

Symptoms:
- A counter increases each time you inspect a variable.
- Log output appears during variable inspection.
- An object is unexpectedly initialized.

Mitigation:
- Use `evaluate_expression` on the backing field directly if you know its name (often `_fieldName` or `<PropertyName>k__BackingField`).
- Be aware that `get_variables_values` calls getters for every visible property.

## Dynamic and Reflection Debugging

### Dynamic Objects

`dynamic` variables show as `object` in the debugger. To inspect them:
```
expression: "((dynamic)myObj).SomeProperty"
expression: "myObj.GetType().GetProperties().Select(p => p.Name).ToArray()"
```

### ExpandoObject

```
expression: "((IDictionary<string, object>)myExpando).Keys.ToArray()"
expression: "((IDictionary<string, object>)myExpando)[\"PropertyName\"]"
```

### Reflection-Created Objects

When inspecting objects created via reflection (e.g., `Activator.CreateInstance`):
```
expression: "myObj.GetType().FullName"
expression: "myObj.GetType().GetProperties().Length"
```
Cast to the actual type if known: `expression: "((MyClass)myObj).MyProperty"`.
