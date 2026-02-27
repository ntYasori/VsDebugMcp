namespace VsDebugMcp.Debugger;

public sealed class DebugStatePoller : IDisposable
{
    private readonly IVsDebuggerService _debugger;
    private readonly CancellationTokenSource _cts = new();
    private readonly int _minIntervalMs;
    private readonly int _maxIntervalMs;
    private DebugState _lastState = DebugState.NotDebugging;
    private Task? _pollingTask;

    public event Action<DebugState, DebugState>? StateChanged;

    public DebugState CurrentState => _lastState;

    public DebugStatePoller(
        IVsDebuggerService debugger,
        int minIntervalMs = 100,
        int maxIntervalMs = 2000)
    {
        _debugger = debugger;
        _minIntervalMs = minIntervalMs;
        _maxIntervalMs = maxIntervalMs;
    }

    public void Start()
    {
        _pollingTask = Task.Run(PollLoopAsync);
    }

    private async Task PollLoopAsync()
    {
        var interval = _minIntervalMs;

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var newState = await _debugger.GetDebugStateAsync();

                if (newState != _lastState)
                {
                    var old = _lastState;
                    _lastState = newState;
                    interval = _minIntervalMs; // Reset backoff on change
                    StateChanged?.Invoke(old, newState);
                }
                else
                {
                    // Exponential backoff when no changes
                    interval = Math.Min(interval * 2, _maxIntervalMs);
                }
            }
            catch (Exception)
            {
                interval = _maxIntervalMs; // Back off on errors
            }

            try
            {
                await Task.Delay(interval, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _pollingTask?.Wait(TimeSpan.FromSeconds(2));
        _cts.Dispose();
    }
}
