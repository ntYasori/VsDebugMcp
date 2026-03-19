using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class InspectionToolsTests
{
    private readonly Mock<IInspectionDebugService> _mockInspection = new();

    [Fact]
    public async Task GetVariablesValues_ShouldCallService()
    {
        _mockInspection.Setup(d => d.GetVariablesAsync(null))
            .ReturnsAsync("**Locals:**\n- x = 42 (int)");

        var result = await InspectionTools.GetVariablesValues(_mockInspection.Object);

        result.Should().Contain("x = 42");
    }

    [Fact]
    public async Task GetVariablesValues_WithDepth_ShouldPassDepth()
    {
        _mockInspection.Setup(d => d.GetVariablesAsync(2))
            .ReturnsAsync("**Locals:**\n- obj = {...} (MyClass)\n  - Name = \"test\" (string)");

        var result = await InspectionTools.GetVariablesValues(_mockInspection.Object, 2);

        result.Should().Contain("Name = \"test\"");
    }

    [Fact]
    public async Task EvaluateExpression_ShouldCallService()
    {
        _mockInspection.Setup(d => d.EvaluateExpressionAsync("x + y"))
            .ReturnsAsync("x + y = 15 (int)");

        var result = await InspectionTools.EvaluateExpression(_mockInspection.Object, "x + y");

        result.Should().Contain("x + y = 15");
    }

    [Fact]
    public async Task GetCallStack_ShouldCallService()
    {
        _mockInspection.Setup(d => d.GetCallStackAsync())
            .ReturnsAsync("Call Stack (2 frames):\n  #0  MyClass.MyMethod() - MyApp.dll, line 42\n  #1  Program.Main(string[]) - MyApp.dll, line 10");

        var result = await InspectionTools.GetCallStack(_mockInspection.Object);

        result.Should().Contain("Call Stack (2 frames)");
        result.Should().Contain("MyClass.MyMethod()");
    }

    [Fact]
    public async Task GetLoadedModules_ShouldCallService()
    {
        _mockInspection.Setup(d => d.GetLoadedModulesAsync(null))
            .ReturnsAsync("3 module(s):\n  MyApp.dll\n    Path: C:\\bin\\MyApp.dll");

        var result = await InspectionTools.GetLoadedModules(_mockInspection.Object);

        result.Should().Contain("module(s)");
    }

    [Fact]
    public async Task SearchVariables_ShouldCallService()
    {
        _mockInspection.Setup(d => d.SearchVariablesAsync("count", null, 3))
            .ReturnsAsync("1 match(es):\n  itemCount = 42 (int)");

        var result = await InspectionTools.SearchVariables(_mockInspection.Object, namePattern: "count");

        result.Should().Contain("match(es)");
    }

    [Fact]
    public async Task GetAutos_ShouldCallService()
    {
        _mockInspection.Setup(d => d.GetAutosAsync())
            .ReturnsAsync("**Locals (current frame):**\n  x = 42 (int)");

        var result = await InspectionTools.GetAutos(_mockInspection.Object);

        result.Should().Contain("Locals");
    }

    [Fact]
    public async Task GetReturnValue_ShouldCallService()
    {
        _mockInspection.Setup(d => d.GetReturnValueAsync())
            .ReturnsAsync("$ReturnValue = 42 (int)");

        var result = await InspectionTools.GetReturnValue(_mockInspection.Object);

        result.Should().Contain("$ReturnValue");
    }
}
