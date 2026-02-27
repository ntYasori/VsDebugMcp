# VsDebugMcp — Agent Instructions

## Build Commands

- **Build:** `dotnet build`
- **Test:** `dotnet test`
- **Publish:** `dotnet publish src/VsDebugMcp -c Release`

## Publish Output

The publish produces a **single self-contained exe** at:
```
src/VsDebugMcp/bin/Release/net8.0-windows/win-x64/publish/VsDebugMcp.exe
```

The exe is self-contained (no .NET runtime needed) and single-file (no DLLs alongside). Only `VsDebugMcp.exe` (and optionally `.pdb`) should be in the publish folder.
