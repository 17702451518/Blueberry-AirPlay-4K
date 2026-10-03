using System.Windows;
using System.Windows.Interop;
using BlueberryAirPlay4K;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var layout = new PackageLayout(@"C:\Test Package\app\");
        if (layout.Engine != @"C:\Test Package\runtime\uxplay-windows.exe") throw new Exception("Structured engine path");
        if (!layout.OwnsProcess(layout.Engine) || layout.OwnsProcess(@"C:\Test Package\runtime-other\uxplay-windows.exe")) throw new Exception("Process isolation");
        var legacy = new PackageLayout(@"C:\Legacy Package\");
        if (legacy.Engine != @"C:\Legacy Package\uxplay-windows.exe") throw new Exception("Legacy path compatibility");
        if (ReceiverProfiles.All.Count != 7 || !ReceiverProfiles.All["4k60-sync"].Arguments.Contains("-fps 60")) throw new Exception("Profile preservation");
        Console.WriteLine("PASS: structured paths, legacy paths, process isolation, profile preservation");
        var window = new Window();
        try
        {
            // A hidden test window: never touches the user's receiver or taskbar settings.
            var handle = new WindowInteropHelper(window).EnsureHandle();
            if (!TaskbarGrouping.TryApply(handle)) throw new Exception("Set window application ID failed");
            if (!TaskbarGrouping.TryApply(handle)) throw new Exception("Repeated application failed");
            TaskbarGrouping.ApplyToProcesses(new HashSet<int>());
            Console.WriteLine("PASS: application ID write, repeated read, empty process isolation");
        }
        finally { window.Close(); }
    }
}
