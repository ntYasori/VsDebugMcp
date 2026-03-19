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

## Release Workflow

Every commit MUST include a version bump, git tag, publish, and GitHub Release:

1. **Bump version** in `src/VsDebugMcp/VsDebugMcp.csproj` (`<Version>X.Y.Z</Version>`)
   - **patch** (0.4.0 → 0.4.1): bug fixes
   - **minor** (0.4.0 → 0.5.0): new features
   - **major** (0.4.0 → 1.0.0): breaking changes
2. **Commit** all changes (include version bump in the same commit)
3. **Tag**: `git tag vX.Y.Z`
4. **Publish**: `dotnet publish src/VsDebugMcp -c Release`
5. **GitHub Release** with detailed notes:
   ```bash
   gh release create vX.Y.Z src/VsDebugMcp/bin/Release/net8.0-windows/win-x64/publish/VsDebugMcp.exe \
     --title "vX.Y.Z — Short Description" \
     --notes "<release notes body>"
   ```
   - **Title format:** `vX.Y.Z — Short Description` (e.g. `v0.5.0 — Multi-VS Instance Support`)
   - **Body MUST** list all changes grouped by category (New Tools, Fixes, Improvements, etc.)
   - **Body MUST** end with: `## Download\nDownload \`VsDebugMcp.exe\` below — self-contained, no .NET runtime needed.`
   - See v0.4.0 release as reference for the format
