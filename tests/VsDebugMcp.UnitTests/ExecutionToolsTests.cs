using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class ExecutionToolsTests
{
    private readonly Mock<IExecutionDebugService> _mockExecution = new();

    [Fact]
    public async Task StepOver_ShouldCallService()
    {
        _mockExecution.Setup(d => d.StepOverAsync())
            .ReturnsAsync("Stepped to: file.cs:10 in Main\n\n→ 10| var x = 1;\n\nLocals:\n  x = 1 (int)");

        var result = await ExecutionTools.StepOver(_mockExecution.Object);

        result.Should().Contain("Stepped to:");
        result.Should().Contain("file.cs:10");
    }

    [Fact]
    public async Task StepInto_ShouldCallService()
    {
        _mockExecution.Setup(d => d.StepIntoAsync())
            .ReturnsAsync("Stepped to: file.cs:20 in Process\n\n→ 20| var y = 2;\n\nLocals:\n  y = 2 (int)");

        var result = await ExecutionTools.StepInto(_mockExecution.Object);

        result.Should().Contain("Stepped to:");
        result.Should().Contain("file.cs:20");
    }

    [Fact]
    public async Task StepOut_ShouldCallService()
    {
        _mockExecution.Setup(d => d.StepOutAsync())
            .ReturnsAsync("Step completed.");

        var result = await ExecutionTools.StepOut(_mockExecution.Object);

        result.Should().Be("Step completed.");
    }

    [Fact]
    public async Task ContinueExecution_ShouldCallService()
    {
        _mockExecution.Setup(d => d.ContinueExecutionAsync())
            .ReturnsAsync("Continuing execution...");

        var result = await ExecutionTools.ContinueExecution(_mockExecution.Object);

        result.Should().Be("Continuing execution...");
    }
}
