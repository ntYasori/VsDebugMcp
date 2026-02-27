using FluentAssertions;
using VsDebugMcp.Debugger;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class DebugStateTests
{
    [Fact]
    public void NotDebugging_ShouldReturnDesignModeState()
    {
        var state = DebugState.NotDebugging;

        state.IsDebugging.Should().BeFalse();
        state.Mode.Should().Be("Design");
        state.CurrentFile.Should().BeNull();
        state.CurrentLine.Should().BeNull();
    }

    [Fact]
    public void With_ShouldCreateModifiedCopy()
    {
        var original = DebugState.NotDebugging;
        var modified = original with { IsDebugging = true, Mode = "Break" };

        original.IsDebugging.Should().BeFalse();
        modified.IsDebugging.Should().BeTrue();
        modified.Mode.Should().Be("Break");
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        var a = new DebugState { IsDebugging = true, Mode = "Break", BreakpointCount = 3 };
        var b = new DebugState { IsDebugging = true, Mode = "Break", BreakpointCount = 3 };

        a.Should().Be(b);
    }

    [Fact]
    public void Inequality_ShouldDetectDifferences()
    {
        var a = new DebugState { IsDebugging = true, Mode = "Break" };
        var b = new DebugState { IsDebugging = true, Mode = "Run" };

        a.Should().NotBe(b);
    }
}
