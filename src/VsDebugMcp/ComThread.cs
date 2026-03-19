using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VsDebugMcp;

/// <summary>
/// Provides a dedicated STA thread for COM interop calls.
/// All EnvDTE interactions must be marshaled through this thread.
/// </summary>
public sealed class ComThread : IDisposable
{
    private readonly Thread _staThread;
    private readonly BlockingCollection<Action> _workQueue = new();
    private readonly ILogger<ComThread> _logger;
    private readonly int _defaultTimeoutMs;
    private bool _disposed;

    public ComThread(ILogger<ComThread> logger, IOptions<DebuggerOptions>? options = null)
    {
        _logger = logger;
        _defaultTimeoutMs = options?.Value.ComOperationTimeoutMs ?? 10_000;

        _staThread = new Thread(StaWorker)
        {
            Name = "VsDebugMcp-STA",
            IsBackground = true
        };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
        _logger.LogDebug("STA thread started (timeout: {TimeoutMs}ms)", _defaultTimeoutMs);
    }

    private void StaWorker()
    {
        try
        {
            foreach (var action in _workQueue.GetConsumingEnumerable())
            {
                action();
            }
        }
        catch (ObjectDisposedException)
        {
            // Expected during shutdown when Dispose races with GetConsumingEnumerable
        }
    }

    /// <summary>
    /// Executes work on the STA thread with a timeout.
    /// Note: A timeout means the caller gave up waiting, but the COM operation may still
    /// be executing on the STA thread (e.g., VS is showing a modal dialog). Subsequent
    /// operations will queue behind it until the STA thread is free.
    /// </summary>
    public async Task<T> RunAsync<T>(Func<T> work, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ComThread));

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_defaultTimeoutMs);

        var registration = timeoutCts.Token.Register(() =>
        {
            tcs.TrySetException(new TimeoutException(
                $"COM operation timed out after {_defaultTimeoutMs}ms. " +
                "Visual Studio may be busy or showing a modal dialog."));
        });

        _workQueue.Add(() =>
        {
            try
            {
                var result = work();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        try
        {
            return await tcs.Task;
        }
        finally
        {
            await registration.DisposeAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogDebug("STA thread shutting down");
        _workQueue.CompleteAdding();
        _staThread.Join(TimeSpan.FromSeconds(5));
        _workQueue.Dispose();
    }
}
