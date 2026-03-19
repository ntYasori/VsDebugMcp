using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace VsDebugMcp.UnitTests;

public class ComThreadTests : IDisposable
{
    private readonly ComThread _sut = new(NullLogger<ComThread>.Instance);

    [Fact]
    public async Task RunAsync_ShouldExecuteWorkAndReturnResult()
    {
        var result = await _sut.RunAsync(() => "hello");
        result.Should().Be("hello");
    }

    [Fact]
    public async Task RunAsync_ShouldExecuteOnStaThread()
    {
        var apartmentState = await _sut.RunAsync(() => Thread.CurrentThread.GetApartmentState());
        apartmentState.Should().Be(ApartmentState.STA);
    }

    [Fact]
    public async Task RunAsync_ShouldPropagateExceptions()
    {
        var act = () => _sut.RunAsync<int>(() => throw new InvalidOperationException("test error"));
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("test error");
    }

    [Fact]
    public async Task RunAsync_AfterDispose_ShouldThrow()
    {
        _sut.Dispose();
        var act = () => _sut.RunAsync(() => 1);
        await act.Should().ThrowAsync<ObjectDisposedException>();
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

    [Fact]
    public async Task RunAsync_WithCancellation_ShouldRespectToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _sut.RunAsync(() =>
        {
            Thread.Sleep(5000);
            return 1;
        }, cts.Token);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    public void Dispose() => _sut.Dispose();
}
