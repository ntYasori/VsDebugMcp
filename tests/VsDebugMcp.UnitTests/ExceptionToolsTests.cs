using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class ExceptionToolsTests
{
    private readonly Mock<IExceptionDebugService> _mockException = new();

    [Fact]
    public async Task ManageExceptionSettings_ShouldCallService()
    {
        _mockException.Setup(d => d.ManageExceptionSettingsAsync("System.NullReferenceException", "always"))
            .ReturnsAsync("Exception 'System.NullReferenceException' set to 'always'.");

        var result = await ExceptionTools.ManageExceptionSettings(
            _mockException.Object, "System.NullReferenceException", "always");

        result.Should().Contain("System.NullReferenceException");
        result.Should().Contain("always");
    }

    [Fact]
    public async Task ListExceptionSettings_ShouldCallService()
    {
        _mockException.Setup(d => d.ListExceptionSettingsAsync(null))
            .ReturnsAsync("Exception settings (5):\n  CLR Exceptions (5):\n    System.Exception [user-unhandled]");

        var result = await ExceptionTools.ListExceptionSettings(_mockException.Object);

        result.Should().Contain("Exception settings");
    }

    [Fact]
    public async Task ListExceptionSettings_WithFilter_ShouldPassFilter()
    {
        _mockException.Setup(d => d.ListExceptionSettingsAsync("NullReference"))
            .ReturnsAsync("Exception settings matching 'NullReference' (1):\n  CLR Exceptions (1):\n    System.NullReferenceException [always]");

        var result = await ExceptionTools.ListExceptionSettings(_mockException.Object, "NullReference");

        result.Should().Contain("NullReference");
    }

    [Fact]
    public async Task GetExceptionChain_ShouldCallService()
    {
        _mockException.Setup(d => d.GetExceptionChainAsync())
            .ReturnsAsync("Exception chain (2 exceptions):\n[0] System.InvalidOperationException\n    Message: \"Something went wrong\"\n  [1] System.NullReferenceException\n    Message: \"Object reference not set\"");

        var result = await ExceptionTools.GetExceptionChain(_mockException.Object);

        result.Should().Contain("Exception chain");
        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("NullReferenceException");
    }

    [Fact]
    public async Task GetExceptionChain_NoException_ShouldReturnMessage()
    {
        _mockException.Setup(d => d.GetExceptionChainAsync())
            .ReturnsAsync("No exception in current context.");

        var result = await ExceptionTools.GetExceptionChain(_mockException.Object);

        result.Should().Contain("No exception");
    }
}
