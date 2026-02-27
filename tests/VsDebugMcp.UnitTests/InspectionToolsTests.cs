using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class InspectionToolsTests
{
    private readonly Mock<IVsDebuggerService> _mockDebugger = new();

    [Fact]
    public async Task GetVariablesValues_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.GetVariablesAsync(null))
            .ReturnsAsync("**Locals:**\n- x = 42 (int)");

        var result = await InspectionTools.GetVariablesValues(_mockDebugger.Object);

        result.Should().Contain("x = 42");
    }

    [Fact]
    public async Task GetVariablesValues_WithDepth_ShouldPassDepth()
    {
        _mockDebugger.Setup(d => d.GetVariablesAsync(2))
            .ReturnsAsync("**Locals:**\n- obj = {...} (MyClass)\n  - Name = \"test\" (string)");

        var result = await InspectionTools.GetVariablesValues(_mockDebugger.Object, 2);

        result.Should().Contain("Name = \"test\"");
    }

    [Fact]
    public async Task EvaluateExpression_ShouldCallService()
    {
        _mockDebugger.Setup(d => d.EvaluateExpressionAsync("x + y"))
            .ReturnsAsync("x + y = 15 (int)");

        var result = await InspectionTools.EvaluateExpression(_mockDebugger.Object, "x + y");

        result.Should().Contain("x + y = 15");
    }
}
