using Microsoft.Extensions.Logging;

namespace VsDebugMcp.Interop;

public sealed class DteConnector
{
    private readonly ComThread _comThread;
    private readonly IRotHelper _rotHelper;
    private readonly ILogger<DteConnector> _logger;
    private readonly int? _targetPid;
    private object? _dte;
    private bool _isConnected;
    private int? _connectedPid;

    public DteConnector(ComThread comThread, IRotHelper rotHelper, ILogger<DteConnector> logger, int? targetPid = null)
    {
        _comThread = comThread;
        _rotHelper = rotHelper;
        _logger = logger;
        _targetPid = targetPid;
    }

    public bool IsConnected => _isConnected;
    public int? ConnectedProcessId => _connectedPid;

    public async Task ConnectAsync()
    {
        _logger.LogDebug("Connecting to Visual Studio{Pid}", _targetPid.HasValue ? $" (PID {_targetPid})" : "");

        (object dte, int pid) = await _comThread.RunAsync(() =>
        {
            if (_targetPid.HasValue)
            {
                var obj = _rotHelper.GetDteByPid(_targetPid.Value);
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
                var instance = _rotHelper.GetFirstDteInstance();
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
        _logger.LogInformation("Connected to Visual Studio (PID {Pid})", pid);
    }

    public async Task SwitchAsync(int pid)
    {
        _logger.LogDebug("Switching to Visual Studio PID {Pid}", pid);

        _dte = await _comThread.RunAsync(() =>
        {
            var dte = _rotHelper.GetDteByPid(pid);
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
        _logger.LogInformation("Switched to Visual Studio (PID {Pid})", pid);
    }

    public async Task<List<VsInstanceInfo>> ListInstancesAsync()
    {
        return await _comThread.RunAsync(() =>
        {
            var instances = _rotHelper.GetRunningDteInstances();
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
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read details for VS instance PID {Pid}", i.ProcessId);
                }

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lost connection to Visual Studio, attempting reconnection");
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
