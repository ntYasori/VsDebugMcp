using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class SessionToolsTests
{
    private readonly Mock<IVsDebuggerService> _mockDebugger = new();

    [Fact]
    public async Task StartDebugging_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.StartDebuggingAsync(null))
            .ReturnsAsync("Debugging started.");

        var result = await SessionTools.StartDebugging(_mockDebugger.Object);

        result.Should().Be("Debugging started.");
        _mockDebugger.Verify(d => d.StartDebuggingAsync(null), Times.Once);
    }

    [Fact]
    public async Task StartDebugging_WithConfiguration_ShouldPassConfiguration()
    {
        _mockDebugger.Setup(d => d.StartDebuggingAsync("Release"))
            .ReturnsAsync("Debugging started in Release.");

        var result = await SessionTools.StartDebugging(_mockDebugger.Object, "Release");

        result.Should().Be("Debugging started in Release.");
        _mockDebugger.Verify(d => d.StartDebuggingAsync("Release"), Times.Once);
    }

    [Fact]
    public async Task StopDebugging_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.StopDebuggingAsync())
            .ReturnsAsync("Debugging stopped.");

        var result = await SessionTools.StopDebugging(_mockDebugger.Object);

        result.Should().Be("Debugging stopped.");
    }

    [Fact]
    public async Task RestartDebugging_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.RestartDebuggingAsync())
            .ReturnsAsync("Debugging restarted.");

        var result = await SessionTools.RestartDebugging(_mockDebugger.Object);

        result.Should().Be("Debugging restarted.");
    }
}
