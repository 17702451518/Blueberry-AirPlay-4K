using System.IO;

namespace BlueberryAirPlay4K;

internal sealed class PackageLayout
{
    internal string Root { get; }
    internal string Runtime { get; }
    internal string Engine => Path.Combine(Runtime, "uxplay-windows.exe");

    internal PackageLayout(string applicationDirectory)
    {
        string app = Path.TrimEndingDirectorySeparator(Path.GetFullPath(applicationDirectory));
        // Legacy flat packages remain usable. New packages always keep dependencies together.
        Root = string.Equals(Path.GetFileName(app), "app", StringComparison.OrdinalIgnoreCase)
            ? Directory.GetParent(app)!.FullName : app;
        Runtime = string.Equals(Path.GetFileName(app), "app", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(Root, "runtime") : Root;
    }

    internal bool OwnsProcess(string path) =>
        string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), Runtime, StringComparison.OrdinalIgnoreCase);
}
