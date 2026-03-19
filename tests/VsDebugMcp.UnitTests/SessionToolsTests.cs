using FluentAssertions;
using ModelContextProtocol.Server;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class SessionToolsTests
{
    private readonly Mock<ISessionDebugService> _mockSession = new();

    [Fact]
    public async Task StartDebugging_ShouldCallService()
    {
        _mockSession.Setup(d => d.StartDebuggingAsync(null))
            .ReturnsAsync("Debugging started.");

        var result = await SessionTools.StartDebugging(null!, _mockSession.Object);

        result.Should().Be("Debugging started.");
        _mockSession.Verify(d => d.StartDebuggingAsync(null), Times.Once);
    }

    [Fact]
    public async Task StartDebugging_WithConfiguration_ShouldPassConfiguration()
    {
        _mockSession.Setup(d => d.StartDebuggingAsync("Release"))
            .ReturnsAsync("Debugging started in Release.");

        var result = await SessionTools.StartDebugging(null!, _mockSession.Object, "Release");

        result.Should().Be("Debugging started in Release.");
        _mockSession.Verify(d => d.StartDebuggingAsync("Release"), Times.Once);
    }

    [Fact]
    public async Task StopDebugging_ShouldCallService()
    {
        _mockSession.Setup(d => d.StopDebuggingAsync())
            .ReturnsAsync("Debugging stopped.");

        var result = await SessionTools.StopDebugging(_mockSession.Object);

        result.Should().Be("Debugging stopped.");
    }

    [Fact]
    public async Task RestartDebugging_ShouldCallService()
    {
        _mockSession.Setup(d => d.RestartDebuggingAsync())
            .ReturnsAsync("Debugging restarted.");

        var result = await SessionTools.RestartDebugging(_mockSession.Object);

        result.Should().Be("Debugging restarted.");
    }

    [Fact]
    public async Task EditAndContinue_ShouldCallService()
    {
        _mockSession.Setup(d => d.ApplyCodeChangesAsync())
            .ReturnsAsync("Edit and Continue: code changes applied successfully.");

        var result = await SessionTools.EditAndContinue(null!, _mockSession.Object);

        result.Should().Contain("code changes applied");
    }

    [Fact]
    public async Task GetDebugState_ShouldReturnFormattedState()
    {
        _mockSession.Setup(d => d.GetDebugStateAsync())
            .ReturnsAsync(new DebugState
            {
                IsDebugging = true,
                Mode = "Break",
                SolutionName = "TestSolution",
                BreakpointCount = 3,
                CurrentFunction = "Main",
                CurrentFile = "Program.cs",
                CurrentLine = 10
            });

        var result = await SessionTools.GetDebugState(_mockSession.Object);

        result.Should().Contain("Mode: Break");
        result.Should().Contain("Breakpoints: 3");
        result.Should().Contain("Function: Main");
    }
}
