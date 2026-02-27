using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace VsDebugMcp.Interop;

internal static class NativeMethods
{
    [DllImport("ole32.dll")]
    internal static extern int CreateBindCtx(int reserved, out IBindCtx bindCtx);

    [DllImport("ole32.dll")]
    internal static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable rot);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    internal static extern void GetActiveObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
        IntPtr reserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object obj);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    internal static extern void CLSIDFromProgID(string progId, out Guid clsid);
}
