using System.Runtime.InteropServices;

namespace BlueberryAirPlay4K;

// Group windows without changing ownership, visibility or the rendering pipeline.
internal static class TaskbarGrouping
{
    internal const string AppId = "Blueberry.AirPlay4K.Desktop";
    internal static void ApplyToProcesses(IReadOnlySet<int> processIds)
    {
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint pid);
            if (processIds.Contains((int)pid) && IsWindowVisible(window)) TryApply(window);
            return true;
        }, IntPtr.Zero);
    }

    internal static bool TryApply(IntPtr window)
    {
        IPropertyStore? store = null;
        var value = new PropVariant();
        var key = new PropertyKey { Format = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), Id = 5 };
        try
        {
            var iid = typeof(IPropertyStore).GUID;
            if (SHGetPropertyStoreForWindow(window, ref iid, out store) < 0) return false;
            if (store.GetValue(ref key, out value) >= 0 && value.Type == 31 &&
                Marshal.PtrToStringUni(value.Pointer) == AppId) return true;
            PropVariantClear(ref value);
            value = new PropVariant { Type = 31, Pointer = Marshal.StringToCoTaskMemUni(AppId) };
            return store.SetValue(ref key, ref value) >= 0;
        }
        catch (COMException) { return false; }
        finally
        {
            PropVariantClear(ref value);
            if (store is not null) Marshal.ReleaseComObject(store);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey { public Guid Format; public uint Id; }
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort Type;
        [FieldOffset(8)] public IntPtr Pointer;
    }
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
        [PreserveSig] int Commit();
    }
    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("shell32.dll")] private static extern int SHGetPropertyStoreForWindow(IntPtr window, ref Guid iid, out IPropertyStore store);
    [DllImport("ole32.dll")] private static extern int PropVariantClear(ref PropVariant value);
}
