using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;

namespace VsDebugMcp.Interop;

internal sealed record DteInstance(object DteObject, string Version, int ProcessId);

internal static partial class RotHelper
{
    [GeneratedRegex(@"!VisualStudio\.DTE\.(\d+\.\d+):(\d+)")]
    private static partial Regex DteMonikerPattern();

    internal static List<DteInstance> GetRunningDteInstances()
    {
        var instances = new List<DteInstance>();

        int hr = NativeMethods.GetRunningObjectTable(0, out var rot);
        if (hr != 0) return instances;

        rot.EnumRunning(out var enumMoniker);
        var monikers = new IMoniker[1];

        NativeMethods.CreateBindCtx(0, out var bindCtx);

        while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
        {
            monikers[0].GetDisplayName(bindCtx, null!, out var displayName);

            var match = DteMonikerPattern().Match(displayName);
            if (!match.Success) continue;

            var version = match.Groups[1].Value;
            var pid = int.Parse(match.Groups[2].Value);

            try
            {
                rot.GetObject(monikers[0], out var obj);
                instances.Add(new DteInstance(obj, version, pid));
            }
            catch (COMException)
            {
                // Instance may have been closed between enumeration and retrieval
            }
        }

        return instances;
    }

    internal static object? GetDteByPid(int pid)
    {
        return GetRunningDteInstances()
            .FirstOrDefault(i => i.ProcessId == pid)
            ?.DteObject;
    }

    internal static object? GetFirstDte()
    {
        return GetRunningDteInstances()
            .OrderByDescending(i => i.Version) // Prefer newest VS
            .FirstOrDefault()
            ?.DteObject;
    }
}
