using System.Collections.Concurrent;

namespace VsDebugMcp;

/// <summary>
/// Provides a dedicated STA thread for COM interop calls.
/// All EnvDTE interactions must be marshaled through this thread.
/// </summary>
public sealed class ComThread : IDisposable
{
    private readonly Thread _staThread;
    private readonly BlockingCollection<Action> _workQueue = new();
    private bool _disposed;

    public ComThread()
    {
        _staThread = new Thread(StaWorker)
        {
            Name = "VsDebugMcp-STA",
            IsBackground = true
        };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
    }

    private void StaWorker()
    {
        foreach (var action in _workQueue.GetConsumingEnumerable())
        {
            action();
        }
    }

    public async Task<T> RunAsync<T>(Func<T> work)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ComThread));

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        _workQueue.Add(() =>
        {
            try
            {
                tcs.SetResult(work());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _workQueue.CompleteAdding();
        _staThread.Join(TimeSpan.FromSeconds(5));
        _workQueue.Dispose();
    }
}
