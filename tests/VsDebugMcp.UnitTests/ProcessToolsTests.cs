using FluentAssertions;
using Moq;
using VsDebugMcp.Debugger;
using VsDebugMcp.Tools;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class ProcessToolsTests
{
    private readonly Mock<IProcessDebugService> _mockProcess = new();

    [Fact]
    public async Task AttachToProcess_WithPid_ShouldCallService()
    {
        _mockProcess.Setup(d => d.AttachToProcessAsync(1234, null))
            .ReturnsAsync("Attached to process 'MyApp.exe' (PID 1234).");

        var result = await ProcessTools.AttachToProcess(null!, _mockProcess.Object, pid: 1234);

        result.Should().Contain("Attached");
        result.Should().Contain("1234");
    }

    [Fact]
    public async Task AttachToProcess_WithName_ShouldCallService()
    {
        _mockProcess.Setup(d => d.AttachToProcessAsync(null, "MyApp"))
            .ReturnsAsync("Attached to process 'MyApp.exe' (PID 5678).");

        var result = await ProcessTools.AttachToProcess(null!, _mockProcess.Object, processName: "MyApp");

        result.Should().Contain("Attached");
    }

    [Fact]
    public async Task DetachFromProcess_ShouldCallService()
    {
        _mockProcess.Setup(d => d.DetachFromProcessAsync())
            .ReturnsAsync("Detached from all processes.");

        var result = await ProcessTools.DetachFromProcess(null!, _mockProcess.Object);

        result.Should().Contain("Detached");
    }

    [Fact]
    public async Task ListProcesses_ShouldCallService()
    {
        _mockProcess.Setup(d => d.ListProcessesAsync(null))
            .ReturnsAsync("3 process(es):\n  PID 1234: MyApp.exe\n  PID 5678: dotnet.exe");

        var result = await ProcessTools.ListProcesses(_mockProcess.Object);

        result.Should().Contain("3 process(es)");
    }

    [Fact]
    public async Task ListProcesses_WithFilter_ShouldPassFilter()
    {
        _mockProcess.Setup(d => d.ListProcessesAsync("dotnet"))
            .ReturnsAsync("1 process(es) matching 'dotnet':\n  PID 5678: dotnet.exe");

        var result = await ProcessTools.ListProcesses(_mockProcess.Object, "dotnet");

        result.Should().Contain("dotnet");
    }
}
