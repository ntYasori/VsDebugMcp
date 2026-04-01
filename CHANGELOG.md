# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-04-01

### Added

- Enhanced step responses: step_over, step_into, step_out now return source code context (surrounding lines with arrow marker) and local variable values after each step.
- Configurable step context: `VSDEBUGMCP_STEP_CONTEXT_LINES` (default: 3) and `VSDEBUGMCP_STEP_MAX_LOCALS` (default: 15) environment variables.
- Source code snippet in `get_current_location` response.
- README.md with full documentation, quick start guide, and tool reference.
- CHANGELOG.md covering all versions.

### Changed

- Step responses format changed from `Now at: file:line in function` to rich format with source context and locals.

## [0.9.1] - 2026-04-01

### Fixed

- Stepping tools (step_over, step_into, step_out) now return correct position. Previously used `WaitForBreakOrEnd=false` (async), causing stale line numbers and "unknown" function names. Changed to synchronous stepping.

## [0.9.0] - 2026-03-19

### Added

- get_loaded_modules, search_variables, get_autos, get_return_value tools.
- execute_immediate_command, navigate_to_source tools.
- INavigationDebugService and NavigationDebugService.

## [0.8.0] - 2026-03-19

### Added

- add_tracepoint tool with message placeholders ({variableName}, $CALLER, $CALLSTACK, $FUNCTION).
- set_hit_count_breakpoint tool (equal, greaterOrEqual, multiple modes).
- add_data_breakpoint tool (C++ native and .NET Core 3.0+).

## [0.7.0] - 2026-03-19

### Added

- attach_to_process, detach_from_process, list_processes tools.
- manage_exception_settings, list_exception_settings, get_exception_chain tools.
- IProcessDebugService and IExceptionDebugService.

## [0.6.0] - 2026-03-19

### Changed

- Split monolithic VsDebuggerService into focused services (ISP): ISessionDebugService, IBreakpointDebugService, IExecutionDebugService, IInspectionDebugService.
- Extract DebuggerHelpers and IRotHelper.

### Added

- Configurable options via environment variables (DebuggerOptions).
- CancellationToken support in ComThread.
- ILogger integration.

## [0.5.1] - 2026-03-19

### Changed

- Updated release workflow in CLAUDE.md.

## [0.5.0] - 2026-03-19

### Added

- list_vs_instances and switch_vs_instance tools.
- Resilient startup (starts without VS running).
- VsInstanceInfo record.

## [0.4.0] - 2026-03-16

### Added

- MCP elicitation support for start_debugging, edit_and_continue, clear_all_breakpoints.
- get_exception_info, get_threads, switch_stack_frame, toggle_breakpoint, get_output tools.
- add_breakpoints_batch, list_configurations tools.
- add_watch, remove_watch, list_watches tools.

### Changed

- Upgrade ModelContextProtocol from 0.2.0-preview.1 to 1.1.0.

### Removed

- DebugStatePoller (dead code).
- Synchronous ComThread.Run methods.

## [0.3.1] - 2026-03-09

### Fixed

- evaluate_expression returns friendly error messages instead of generic MCP SDK exceptions.

## [0.3.0] - 2026-03-06

### Added

- edit_and_continue, set_next_statement, run_to_cursor tools.
- evaluate_multiple, get_current_location tools.

## [0.2.0] - 2026-02-27

### Added

- get_call_stack tool.

## [0.1.0] - 2026-02-27

### Added

- Initial release: MCP server for Visual Studio debugging via COM automation.
- 14 core tools: start_debugging, stop_debugging, restart_debugging, step_over, step_into, step_out, continue_execution, add_breakpoint, remove_breakpoint, clear_all_breakpoints, list_breakpoints, get_variables_values, evaluate_expression, get_debug_state.
- 5 MCP resources: debug instructions + C#, C++, F#, VB.NET troubleshooting.
- COM interop via Running Object Table.
- Dedicated STA thread (ComThread).
- Multi-instance targeting via --vs-pid.
- stdio transport (JSON-RPC).

[1.0.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.9.1...v1.0.0
[0.9.1]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.9.0...v0.9.1
[0.9.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.8.0...v0.9.0
[0.8.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.5.1...v0.6.0
[0.5.1]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.5.0...v0.5.1
[0.5.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.3.1...v0.4.0
[0.3.1]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/ViniciusGomes99/VsDebugMcp/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/ViniciusGomes99/VsDebugMcp/releases/tag/v0.1.0
