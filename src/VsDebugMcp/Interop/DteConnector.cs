using System.Runtime.InteropServices;

namespace VsDebugMcp.Interop;

public sealed class DteConnector
{
    private readonly ComThread _comThread;
    private readonly int? _targetPid;
    private object? _dte;
    private bool _isConnected;

    public DteConnector(ComThread comThread, int? targetPid = null)
    {
        _comThread = comThread;
        _targetPid = targetPid;
    }

    public bool IsConnected => _isConnected;

    public object Dte => _dte ?? throw new InvalidOperationException(
        "Not connected to Visual Studio. Call ConnectAsync() first.");

    public async Task ConnectAsync()
    {
        _dte = await _comThread.RunAsync(() =>
        {
            object? dte = _targetPid.HasValue
                ? RotHelper.GetDteByPid(_targetPid.Value)
                : RotHelper.GetFirstDte();

            if (dte is null)
            {
                var message = _targetPid.HasValue
                    ? $"Could not find Visual Studio instance with PID {_targetPid.Value}. " +
                      "Make sure Visual Studio is running and a solution is open."
                    : "Could not find any running Visual Studio instance. " +
                      "Make sure Visual Studio is running and a solution is open.";
                throw new InvalidOperationException(message);
            }

            // Verify the connection works by accessing a property
            var dteCast = (EnvDTE.DTE)dte;
            _ = dteCast.Version;

            return dte;
        });

        _isConnected = true;
    }

    public async Task EnsureConnectedAsync()
    {
        if (_isConnected)
        {
            // Verify connection is still alive
            try
            {
                await _comThread.RunAsync(() =>
                {
                    var dteCast = (EnvDTE.DTE)_dte!;
                    _ = dteCast.Version;
                    return true;
                });
                return;
            }
            catch (COMException)
            {
                _isConnected = false;
            }
        }

        await ConnectAsync();
    }

    public async Task<T> ExecuteOnDteAsync<T>(Func<EnvDTE.DTE, T> action)
    {
        await EnsureConnectedAsync();
        return await _comThread.RunAsync(() => action((EnvDTE.DTE)_dte!));
    }

    public async Task ExecuteOnDteAsync(Action<EnvDTE.DTE> action)
    {
        await EnsureConnectedAsync();
        await _comThread.RunAsync(() =>
        {
            action((EnvDTE.DTE)_dte!);
            return true;
        });
    }
}
