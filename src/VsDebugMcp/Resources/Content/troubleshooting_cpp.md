# C++ Debugging Tips and Troubleshooting

## Memory Issues

### Access Violations

When the debugger breaks on an access violation (0xC0000005):

1. Check the faulting pointer:
   ```
   expression: "ptr"             // address value (0x00000000 means null dereference)
   expression: "&variable"       // verify address is in valid range
   ```
2. Common causes:
   - **Null pointer dereference:** The pointer is `0x00000000` or `0xcccccccc` (uninitialized in debug builds).
   - **Use-after-free:** The pointer was valid but the memory was freed. Look for `0xdddddddd` (freed heap memory in debug builds) or `0xfeeefeee` (freed heap in Windows debug heap).
   - **Buffer overrun:** The pointer has gone past the allocated region. Compare with the original allocation size.

### Debug Memory Patterns (MSVC)

In Debug builds, MSVC fills memory with recognizable patterns:

| Pattern | Meaning |
|---|---|
| `0xCCCCCCCC` | Uninitialized stack memory |
| `0xCDCDCDCD` | Uninitialized heap memory (`new`/`malloc`) |
| `0xDDDDDDDD` | Freed heap memory (`delete`/`free`) |
| `0xFEEEFEEE` | Windows heap freed memory |
| `0xFDFDFDFD` | Heap guard bytes (buffer boundaries) |

If a pointer or value shows these patterns, you have a memory lifecycle bug.

### Buffer Overflow Detection

When you suspect a buffer overflow:
1. Set a breakpoint before the suspected write operation.
2. Inspect the buffer bounds:
   ```
   expression: "sizeof(buffer)"
   expression: "strlen(inputStr)"
   ```
3. Set a conditional breakpoint to catch the overwrite: `condition: "index >= bufferSize"`
4. Step through the loop watching the index and buffer contents.

## Pointer Inspection

### Viewing Pointer Contents

```
expression: "*ptr"              // dereference pointer
expression: "ptr[0]"            // first element
expression: "ptr[5]"            // sixth element
expression: "*(ptr + offset)"   // offset dereference
```

### Viewing Arrays Through Pointers

The debugger cannot determine array size from a raw pointer. Use format specifiers:
```
expression: "ptr,10"            // show 10 elements (Visual Studio specific)
```

Or inspect element by element:
```
expression: "ptr[0]"
expression: "ptr[1]"
expression: "ptr[9]"
```

### Struct/Class Through Pointer

```
expression: "ptr->memberName"
expression: "(*ptr).memberName"
expression: "ptr->nestedPtr->value"
```

## Native/Managed Interop (C++/CLI)

### Crossing the Native/Managed Boundary

When debugging C++/CLI code that calls between native and managed:
- The call stack shows both native and managed frames. Managed frames are prefixed with the assembly name.
- Local variable inspection works in both contexts, but you may need different expression syntax.
- Stepping from managed into native (or vice versa) works, but there may be extra thunk frames in the call stack.

### Marshaling Issues

Common interop debugging points:
1. Check string marshaling:
   ```
   expression: "managedString"                     // System::String^
   expression: "nativeCharPtr"                     // char* or wchar_t*
   ```
2. Verify struct layout matches between native and managed definitions.
3. Watch for lifetime issues: a pinned managed object can be collected if the pin goes out of scope.

## Symbol Loading and PDB Files

### Missing Symbols

If variable values show as "optimized away" or stepping behaves unexpectedly:
1. Verify you are building in **Debug** configuration, not Release.
2. Check that PDB files exist alongside the binary.
3. In Visual Studio: Debug > Windows > Modules -- verify the "Symbol Status" column shows "Symbols loaded."

### Optimized Code

In Release builds, the compiler may:
- Eliminate variables entirely (inlined or register-only).
- Reorder statements.
- Remove "dead" code branches.

If you must debug Release code, add `[MethodImpl(MethodImplOptions.NoOptimization)]` to the specific method temporarily, or use `#pragma optimize("", off)` / `#pragma optimize("", on)` to disable optimization for a code section.

## Conditional Breakpoints for Loops

### Iteration-Based Breaking

```
Tool: add_breakpoint
filePath: "C:\Projects\MyApp\processor.cpp"
line: 87
condition: "i == 500"
```

### Value-Based Breaking

```
condition: "strcmp(name, \"targetValue\") == 0"
condition: "ptr != nullptr && ptr->id == 42"
condition: "result > 1000.0 || result < -1000.0"
```

### Performance Note

Conditional breakpoints evaluate the condition every time the breakpoint is hit. In tight loops (millions of iterations), this can significantly slow down execution. Consider:
- Using a hit count condition instead (set in VS UI).
- Adding a temporary `if` statement in the code with a simpler breakpoint.

## Watch Window Expressions for Native Types

### STL Containers (with Natvis Visualizers)

Visual Studio includes Natvis visualizers for STL types. When inspecting:
```
expression: "myVector.size()"
expression: "myVector[0]"
expression: "myMap.size()"
```

For `std::map`/`std::unordered_map`, direct key lookup in the evaluator may not work. Inspect the size and iterate manually or use the Variables window in VS.

### Common Format Specifiers

In `evaluate_expression`, some format specifiers work:
- `variable,x` -- hexadecimal display
- `variable,d` -- decimal display
- `variable,o` -- octal display
- `variable,s` -- treat as null-terminated string
- `variable,su` -- treat as Unicode string
- `variable,hr` -- HRESULT description
- `variable,na` -- suppress Natvis expansion (show raw members)

### Raw Memory Interpretation

```
expression: "*(int*)&myFloat"           // reinterpret float bits as int
expression: "(char*)&myStruct"          // view struct as raw bytes
expression: "((MyStruct*)rawPtr)->field" // cast raw pointer to struct
```
