using FluentAssertions;
using VsDebugMcp.Debugger;
using VsDebugMcp.Interop;
using Xunit;

namespace VsDebugMcp.IntegrationTests;

[Trait("Category", "Integration")]
public class VsIntegrationTests : IDisposable
{
    private readonly ComThread _comThread;
    private readonly DteConnector _connector;
    private readonly VsDebuggerService _debugger;

    public VsIntegrationTests()
    {
        _comThread = new ComThread();
        _connector = new DteConnector(_comThread);
        _debugger = new VsDebuggerService(_connector, _comThread);
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
        var state = await _debugger.GetDebugStateAsync();

        state.Mode.Should().Be("Design");
        state.IsDebugging.Should().BeFalse();
    }

    [Fact]
    public async Task ListBreakpoints_ShouldReturnString()
    {
        await _connector.ConnectAsync();
        var result = await _debugger.ListBreakpointsAsync();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FullDebugWorkflow_ShouldWork()
    {
        await _connector.ConnectAsync();

        // Clear existing breakpoints
        await _debugger.ClearAllBreakpointsAsync();

        // Get initial state
        var state = await _debugger.GetDebugStateAsync();
        state.Mode.Should().Be("Design");

        // Note: Full workflow test requires a solution to be open
        // and the SimpleConsoleApp project set as startup project
    }

    public void Dispose()
    {
        _comThread.Dispose();
    }
}
