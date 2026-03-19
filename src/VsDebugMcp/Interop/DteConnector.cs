using System.Runtime.InteropServices;

namespace VsDebugMcp.Interop;

public sealed class DteConnector
{
    private readonly ComThread _comThread;
    private readonly int? _targetPid;
    private object? _dte;
    private bool _isConnected;
    private int? _connectedPid;

    public DteConnector(ComThread comThread, int? targetPid = null)
    {
        _comThread = comThread;
        _targetPid = targetPid;
    }

    public bool IsConnected => _isConnected;
    public int? ConnectedProcessId => _connectedPid;

    public async Task ConnectAsync()
    {
        (object dte, int pid) = await _comThread.RunAsync(() =>
        {
            if (_targetPid.HasValue)
            {
                var obj = RotHelper.GetDteByPid(_targetPid.Value);
                if (obj is null)
                    throw new InvalidOperationException(
                        $"Could not find Visual Studio instance with PID {_targetPid.Value}. " +
                        "Make sure Visual Studio is running and a solution is open.");
                var dteCast = (EnvDTE.DTE)obj;
                _ = dteCast.Version;
                return (obj, _targetPid.Value);
            }
            else
            {
                var instance = RotHelper.GetFirstDteInstance();
                if (instance is null)
                    throw new InvalidOperationException(
                        "Could not find any running Visual Studio instance. " +
                        "Make sure Visual Studio is running and a solution is open.");
                var dteCast = (EnvDTE.DTE)instance.DteObject;
                _ = dteCast.Version;
                return (instance.DteObject, instance.ProcessId);
            }
        });

        _dte = dte;
        _connectedPid = pid;
        _isConnected = true;
    }

    public async Task SwitchAsync(int pid)
    {
        _dte = await _comThread.RunAsync(() =>
        {
            var dte = RotHelper.GetDteByPid(pid);
            if (dte is null)
                throw new InvalidOperationException(
                    $"Could not find Visual Studio instance with PID {pid}. " +
                    "Make sure Visual Studio is running and a solution is open.");
            var dteCast = (EnvDTE.DTE)dte;
            _ = dteCast.Version;
            return dte;
        });
        _connectedPid = pid;
        _isConnected = true;
    }

    public async Task<List<VsInstanceInfo>> ListInstancesAsync()
    {
        return await _comThread.RunAsync(() =>
        {
            var instances = RotHelper.GetRunningDteInstances();
            return instances.Select(i =>
            {
                string? solutionName = null;
                string? windowTitle = null;
                try
                {
                    var dte = (EnvDTE.DTE)i.DteObject;
                    var slnPath = dte.Solution?.FullName;
                    solutionName = string.IsNullOrEmpty(slnPath)
                        ? null
                        : Path.GetFileNameWithoutExtension(slnPath);
                    windowTitle = dte.MainWindow?.Caption;
                }
                catch { /* VS may be busy or showing a modal dialog */ }

                return new VsInstanceInfo(
                    i.ProcessId, i.Version,
                    solutionName, windowTitle,
                    i.ProcessId == _connectedPid);
            }).ToList();
        });
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
            catch (Exception)
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
