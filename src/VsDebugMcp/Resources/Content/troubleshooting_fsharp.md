# F# Debugging Tips and Troubleshooting

## Pattern Matching and Discriminated Unions

### Inspecting Discriminated Unions

Discriminated unions compile to a class hierarchy. In the debugger, the display might differ from F# syntax:

```
expression: "myUnion"                  // shows the actual case and value
expression: "myUnion.Tag"              // numeric tag for the active case
expression: "myUnion.IsCase1"          // boolean check for a specific case
expression: "myUnion.Item"             // value for single-field cases
expression: "myUnion.Item1"            // first field for multi-field cases
```

For example, given `type Shape = Circle of float | Rectangle of float * float`:
```
expression: "shape.IsCircle"           // true/false
expression: "shape.IsRectangle"        // true/false
expression: "shape.Item"               // radius if Circle
expression: "shape.Item1"              // width if Rectangle
expression: "shape.Item2"              // height if Rectangle
```

### Debugging Match Expressions

When stepping through a `match` expression:
1. The debugger evaluates the expression being matched.
2. It then jumps to the matching case branch.
3. If a `when` guard is present, it evaluates that condition too.

Set a breakpoint on the `match` line, then step through to see which branch is taken. If the wrong branch is matched, evaluate the match expression to see its actual value.

### Option and Result Types

```
expression: "myOption"                 // Some(value) or None
expression: "myOption.Value"           // the inner value (throws if None)
expression: "myOption.IsSome"          // boolean
expression: "myOption.IsNone"          // boolean
```

For `Result<'T,'TError>`:
```
expression: "myResult.IsOk"
expression: "myResult.IsError"
expression: "myResult.ResultValue"     // the Ok value
expression: "myResult.ErrorValue"      // the Error value
```

## Computation Expressions

### Async Computation Expressions

F# `async { }` blocks compile to a state machine similar to C# `async`/`await`. When debugging:
- The call stack shows `MoveNext` methods from the generated state machine.
- Local variables captured in the async block are visible as fields on the state machine.
- Use `get_variables_values` to see `builder`, `awaiter`, and captured locals.

Step through `let!` and `do!` bindings to see when async operations complete.

### Custom Computation Expressions

For custom CEs (like `result { }`, `task { }`, or domain-specific builders):
- The builder's `Bind`, `Return`, `Zero`, etc. methods are called at runtime.
- Step Into on a `let!` will enter the builder's `Bind` method.
- If the CE short-circuits (e.g., returns early on Error), stepping will show the path through the builder, which can be confusing.

**Tip:** Set breakpoints inside the builder's methods to understand the flow.

## Pipeline Debugging

### Breaking Long Pipes

F# pipelines (`|>`) compile to nested function calls. The debugger steps through them one operator at a time, but inspecting intermediate values is tricky.

For a pipeline like:
```fsharp
data
|> List.filter isValid
|> List.map transform
|> List.sortBy key
|> List.take 10
```

**Strategy 1: Breakpoint on the pipeline and evaluate stages**
Set a breakpoint on the first line and evaluate intermediate results:
```
expression: "data |> List.filter isValid |> Seq.toList"
expression: "data |> List.filter isValid |> List.map transform |> Seq.toList"
```

**Strategy 2: Bind intermediates**
Temporarily rewrite the pipeline with `let` bindings:
```fsharp
let filtered = data |> List.filter isValid
let mapped = filtered |> List.map transform
let sorted = mapped |> List.sortBy key
let result = sorted |> List.take 10
```

Now each variable is inspectable with `get_variables_values`.

### Partial Application Debugging

Partially applied functions appear as `FSharpFunc` objects in the debugger. They are opaque -- you cannot see the captured arguments directly. If you need to inspect them, evaluate the full application:
```
expression: "myPartiallyAppliedFn(testArg)"
```

## Async Workflows

### F# Async vs Task

F# `Async<'T>` is different from `Task<'T>`. When debugging:
- `Async.RunSynchronously` blocks the current thread -- watch for deadlocks.
- `Async.StartAsTask` converts to a `Task` that you can inspect with `.Status`, `.Result`, etc.
- Inside an `async { }` block, use `get_variables_values` to see the `CancellationToken` and other captured state.

### Cancellation

```
expression: "cancellationToken.IsCancellationRequested"
```

If a workflow is not responding to cancellation, check that `let!` is used (cooperative cancellation) rather than `let` (no cancellation point).

## Type Provider Debugging

### Erased Type Providers

Erased type providers (JSON, CSV, SQL, etc.) generate types that exist only at compile time. At runtime:
- The objects are typically `obj` or dynamic dispatch behind the scenes.
- Property access in the debugger may not work with the provided type names.
- Use `evaluate_expression` with the actual runtime type:
  ```
  expression: "myProvidedObj.GetType().FullName"
  expression: "myProvidedObj.GetType().GetProperties().Select(fun p -> p.Name).ToArray()"
  ```

### Generated Type Providers

Generated type providers (e.g., some database providers) produce real .NET types. These debug normally -- you can inspect properties and call methods as expected.

## Tail Call Optimization Effects

### Missing Stack Frames

When F# performs tail call optimization (TCO), recursive calls do not create new stack frames. This means:
- The call stack shows only the current invocation, not the recursion history.
- You cannot step "back" through prior recursive calls.
- Local variables from previous iterations are gone.

**Workaround:** To debug a tail-recursive function:
1. Set a conditional breakpoint to break on a specific iteration/value.
2. Use `evaluate_expression` to inspect the accumulator and parameters.
3. If you need the full call stack, temporarily disable TCO by adding a non-tail operation after the recursive call (e.g., wrap in `id`). Remember to revert this change afterward.

### Debug vs Release Behavior

TCO may only be active in Release builds. In Debug builds, the JIT typically preserves stack frames for easier debugging. If you see different behavior between Debug and Release, TCO is a likely cause.
