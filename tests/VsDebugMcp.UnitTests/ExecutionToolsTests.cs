using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class ExecutionToolsTests
{
    private readonly Mock<IVsDebuggerService> _mockDebugger = new();

    [Fact]
    public async Task StepOver_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.StepOverAsync())
            .ReturnsAsync("Now at: file.cs:10");

        var result = await ExecutionTools.StepOver(_mockDebugger.Object);

        result.Should().Contain("file.cs:10");
    }

    [Fact]
    public async Task StepInto_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.StepIntoAsync())
            .ReturnsAsync("Now at: file.cs:20");

        var result = await ExecutionTools.StepInto(_mockDebugger.Object);

        result.Should().Contain("file.cs:20");
    }

    [Fact]
    public async Task StepOut_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.StepOutAsync())
            .ReturnsAsync("Step completed.");

        var result = await ExecutionTools.StepOut(_mockDebugger.Object);

        result.Should().Be("Step completed.");
    }

    [Fact]
    public async Task ContinueExecution_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.ContinueExecutionAsync())
            .ReturnsAsync("Continuing execution...");

        var result = await ExecutionTools.ContinueExecution(_mockDebugger.Object);

        result.Should().Be("Continuing execution...");
    }
}
