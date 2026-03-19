using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VsDebugMcp.Debugger;
using VsDebugMcp.Interop;
using Xunit;

namespace VsDebugMcp.IntegrationTests;

[Trait("Category", "Integration")]
public class VsIntegrationTests : IDisposable
{
    private readonly ComThread _comThread;
    private readonly DteConnector _connector;
    private readonly SessionDebugService _sessionService;
    private readonly BreakpointDebugService _breakpointService;

    public VsIntegrationTests()
    {
        var options = Options.Create(new DebuggerOptions());
        _comThread = new ComThread(NullLogger<ComThread>.Instance, options);
        var rotHelper = new RotHelper();
        _connector = new DteConnector(_comThread, rotHelper, NullLogger<DteConnector>.Instance);
        _sessionService = new SessionDebugService(_connector, NullLogger<SessionDebugService>.Instance, options);
        _breakpointService = new BreakpointDebugService(_connector, NullLogger<BreakpointDebugService>.Instance);
    }

    [Fact]
    public async Task ConnectToVs_ShouldSucceed()
    {
        await _connector.ConnectAsync();
        _connector.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task GetDebugState_WhenNotDebugging_ShouldReturnDesignMode()
    {
        await _connector.ConnectAsync();
        var state = await _sessionService.GetDebugStateAsync();

        state.Mode.Should().Be("Design");
        state.IsDebugging.Should().BeFalse();
    }

    [Fact]
    public async Task ListBreakpoints_ShouldReturnString()
    {
        await _connector.ConnectAsync();
        var result = await _breakpointService.ListBreakpointsAsync();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FullDebugWorkflow_ShouldWork()
    {
        await _connector.ConnectAsync();

        // Clear existing breakpoints
        await _breakpointService.ClearAllBreakpointsAsync();

        // Get initial state
        var state = await _sessionService.GetDebugStateAsync();
        state.Mode.Should().Be("Design");

        // Note: Full workflow test requires a solution to be open
        // and the SimpleConsoleApp project set as startup project
    }

    public void Dispose()
    {
        _comThread.Dispose();
    }
}
