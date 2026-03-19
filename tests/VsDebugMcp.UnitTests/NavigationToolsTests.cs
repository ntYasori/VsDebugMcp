using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class NavigationToolsTests
{
    private readonly Mock<INavigationDebugService> _mockNavigation = new();

    [Fact]
    public async Task ExecuteImmediateCommand_ShouldCallService()
    {
        _mockNavigation.Setup(d => d.ExecuteImmediateCommandAsync("myVar = 42"))
            .ReturnsAsync("Executed: myVar = 42");

        var result = await NavigationTools.ExecuteImmediateCommand(_mockNavigation.Object, "myVar = 42");

        result.Should().Contain("Executed");
        result.Should().Contain("myVar = 42");
    }

    [Fact]
    public async Task NavigateToSource_ShouldCallService()
    {
        _mockNavigation.Setup(d => d.NavigateToSourceAsync("C:\\src\\Program.cs", 42))
            .ReturnsAsync("Navigated to C:\\src\\Program.cs:42");

        var result = await NavigationTools.NavigateToSource(_mockNavigation.Object, "C:\\src\\Program.cs", 42);

        result.Should().Contain("Navigated");
        result.Should().Contain("Program.cs");
    }
}
