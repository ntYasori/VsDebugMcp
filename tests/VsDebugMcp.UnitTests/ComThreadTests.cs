using FluentAssertions;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class ComThreadTests : IDisposable
{
    private readonly ComThread _sut = new();

    [Fact]
    public void Run_ShouldExecuteWorkAndReturnResult()
    {
        var result = _sut.Run(() => 42);
        result.Should().Be(42);
    }

    [Fact]
    public async Task RunAsync_ShouldExecuteWorkAndReturnResult()
    {
        var result = await _sut.RunAsync(() => "hello");
        result.Should().Be("hello");
    }

    [Fact]
    public void Run_ShouldExecuteOnStaThread()
    {
        var apartmentState = _sut.Run(() => Thread.CurrentThread.GetApartmentState());
        apartmentState.Should().Be(ApartmentState.STA);
    }

    [Fact]
    public void Run_ShouldPropagateExceptions()
    {
        var act = () => _sut.Run<int>(() => throw new InvalidOperationException("test error"));
        act.Should().Throw<InvalidOperationException>().WithMessage("test error");
    }

    [Fact]
    public void Run_AfterDispose_ShouldThrow()
    {
        _sut.Dispose();
        var act = () => _sut.Run(() => 1);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task RunAsync_MultipleConcurrentCalls_ShouldAllComplete()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _sut.RunAsync(() => i * 2))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        results.Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(i => i * 2));
    }

    public void Dispose() => _sut.Dispose();
}
