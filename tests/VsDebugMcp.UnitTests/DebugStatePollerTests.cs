using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class DebugStatePollerTests : IDisposable
{
    private readonly Mock<IVsDebuggerService> _mockDebugger = new();
    private readonly DebugStatePoller _sut;

    public DebugStatePollerTests()
    {
        _mockDebugger.Setup(d => d.GetDebugStateAsync())
            .ReturnsAsync(DebugState.NotDebugging);
        _sut = new DebugStatePoller(_mockDebugger.Object, minIntervalMs: 50, maxIntervalMs: 200);
    }

    [Fact]
    public void CurrentState_Initially_ShouldBeNotDebugging()
    {
        _sut.CurrentState.Should().Be(DebugState.NotDebugging);
    }

    [Fact]
    public async Task StateChanged_ShouldFireOnStateChange()
    {
        var breakState = new DebugState { IsDebugging = true, Mode = "Break" };
        var stateChangedFired = new TaskCompletionSource<(DebugState, DebugState)>();

        _mockDebugger.SetupSequence(d => d.GetDebugStateAsync())
            .ReturnsAsync(DebugState.NotDebugging)
            .ReturnsAsync(breakState);

        _sut.StateChanged += (old, @new) => stateChangedFired.TrySetResult((old, @new));
        _sut.Start();

        var (oldState, newState) = await stateChangedFired.Task.WaitAsync(TimeSpan.FromSeconds(5));

        oldState.Should().Be(DebugState.NotDebugging);
        newState.Mode.Should().Be("Break");
    }

    public void Dispose() => _sut.Dispose();
}
