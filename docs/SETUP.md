# VsDebugMcp Setup Guide

## Prerequisites

- **Visual Studio 2022** (17.x) or **Visual Studio 2026** (18.x) -- Community, Professional, or Enterprise
- **.NET 8 SDK** or later -- [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows 10/11** -- COM automation requires Windows
- A solution open in Visual Studio (the debugger attaches to a running VS instance)

## Building from Source

Clone the repository and build:

```bash
git clone <repository-url>
cd VsDebugMcp
dotnet build -c Release
```

The output binary is located at:
```
src/VsDebugMcp/bin/Release/net8.0-windows/VsDebugMcp.exe
```

## Registering with Claude Code

### Option 1: CLI Registration

```bash
claude mcp add vs-debugger \
  --transport stdio \
  -- "C:\path\to\VsDebugMcp.exe"
```

To target a specific Visual Studio instance by process ID:

```bash
claude mcp add vs-debugger \
  --transport stdio \
  -- "C:\path\to\VsDebugMcp.exe" --vs-pid 12345
```

You can find the Visual Studio PID in Task Manager or by running:
```powershell
Get-Process devenv | Select-Object Id, MainWindowTitle
```

### Option 2: Project-Level Configuration (.mcp.json)

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

With a specific Visual Studio PID:

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

### Option 3: User-Level Configuration

Add to your Claude Code settings (`~/.claude/settings.json` or equivalent):

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

## Configuration Options

| Option | Description |
|---|---|
| `--vs-pid <pid>` | Target a specific Visual Studio instance by process ID. If omitted, VsDebugMcp connects to the most recent VS instance found via the Running Object Table. |

## Verifying the Setup

After registering, verify the server is available:

```bash
claude mcp list
```

You should see `vs-debugger` in the list. Then start a conversation with Claude Code and ask it to check the debugger state -- it should be able to connect to Visual Studio.

You can also verify by asking Claude Code:
> "List the available debugging tools"

It should describe the session, execution, breakpoint, and inspection tools.

## Troubleshooting

### Visual Studio Not Detected

**Symptom:** Error message "Could not find any running Visual Studio instance."

**Causes and fixes:**
1. **Visual Studio is not running.** Start Visual Studio and open a solution.
2. **No solution is open.** The COM automation interface requires a solution to be loaded.
3. **Running as different user.** VsDebugMcp and Visual Studio must run under the same Windows user account. COM objects in the Running Object Table (ROT) are per-user.
4. **Elevation mismatch.** If Visual Studio is running as Administrator, VsDebugMcp must also run elevated (or vice versa). COM ROT entries are separated by integrity level.

### COM Errors (COMException, InvalidCastException)

**Symptom:** Operations fail with COM-related exceptions.

**Causes and fixes:**
1. **Visual Studio was closed.** If VS is closed while VsDebugMcp is running, all COM calls fail. Restart VsDebugMcp after reopening VS.
2. **VS is busy (modal dialog).** If Visual Studio is showing a modal dialog (save prompt, error dialog), COM calls may hang or fail. Dismiss the dialog and retry.
3. **Build in progress.** Some debug operations are unavailable while a build is running. Wait for the build to complete.

### Permission Issues

**Symptom:** Access denied errors or inability to enumerate COM objects.

**Causes and fixes:**
1. **Antivirus/EDR blocking.** Some security software blocks COM automation. Add VsDebugMcp.exe to the allow list.
2. **Group Policy restrictions.** Enterprise environments may restrict COM automation. Check with your IT administrator.
3. **DCOM configuration.** In rare cases, DCOM settings may need adjustment. Run `dcomcnfg` and verify permissions.

### Debugger State Mismatch

**Symptom:** Tools report the debugger is in the wrong state (e.g., "not debugging" when you think it is).

**Causes and fixes:**
1. **Wrong VS instance.** If multiple Visual Studio instances are running, VsDebugMcp may be connected to a different one. Use `--vs-pid` to target the correct instance.
2. **Stale connection.** Restart VsDebugMcp to force a fresh connection.

### Performance

**Symptom:** Commands are slow to respond.

**Notes:**
- COM calls to Visual Studio are inherently synchronous and run on a dedicated STA thread. Each command round-trips through COM.
- Expression evaluation speed depends on the complexity of the expression and the state of the debuggee process.
- `get_variables_values` with high depth values on objects with many properties can produce large results and take longer.
