using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class BreakpointToolsTests
{
    private readonly Mock<IBreakpointDebugService> _mockBreakpoints = new();

    [Fact]
    public async Task AddBreakpoint_ShouldCallServiceWithParameters()
    {
        _mockBreakpoints.Setup(d => d.AddBreakpointAsync("test.cs", 10, null))
            .ReturnsAsync("Breakpoint added at test.cs:10");

        var result = await BreakpointTools.AddBreakpoint(_mockBreakpoints.Object, "test.cs", 10);

        result.Should().Contain("test.cs:10");
    }

    [Fact]
    public async Task AddBreakpoint_WithCondition_ShouldPassCondition()
    {
        _mockBreakpoints.Setup(d => d.AddBreakpointAsync("test.cs", 15, "x > 5"))
            .ReturnsAsync("Breakpoint added at test.cs:15 (condition: x > 5)");

        var result = await BreakpointTools.AddBreakpoint(_mockBreakpoints.Object, "test.cs", 15, "x > 5");

        result.Should().Contain("condition: x > 5");
    }

    [Fact]
    public async Task RemoveBreakpoint_ShouldCallService()
    {
        _mockBreakpoints.Setup(d => d.RemoveBreakpointAsync("test.cs", 10))
            .ReturnsAsync("Breakpoint removed at test.cs:10.");

        var result = await BreakpointTools.RemoveBreakpoint(_mockBreakpoints.Object, "test.cs", 10);

        result.Should().Contain("removed");
    }

    [Fact]
    public async Task ClearAllBreakpoints_ShouldCallService()
    {
        _mockBreakpoints.Setup(d => d.ClearAllBreakpointsAsync())
            .ReturnsAsync("Cleared 5 breakpoint(s).");

        var result = await BreakpointTools.ClearAllBreakpoints(null!, _mockBreakpoints.Object);

        result.Should().Contain("Cleared");
    }

    [Fact]
    public async Task ListBreakpoints_ShouldCallService()
    {
        _mockBreakpoints.Setup(d => d.ListBreakpointsAsync())
            .ReturnsAsync("2 breakpoint(s):\n- file.cs:10\n- file.cs:20");

        var result = await BreakpointTools.ListBreakpoints(_mockBreakpoints.Object);

        result.Should().Contain("2 breakpoint(s)");
    }

    [Fact]
    public async Task ToggleBreakpoint_ShouldCallService()
    {
        _mockBreakpoints.Setup(d => d.ToggleBreakpointAsync("test.cs", 10))
            .ReturnsAsync("Breakpoint at test.cs:10 is now disabled.");

        var result = await BreakpointTools.ToggleBreakpoint(_mockBreakpoints.Object, "test.cs", 10);

        result.Should().Contain("disabled");
    }
}
