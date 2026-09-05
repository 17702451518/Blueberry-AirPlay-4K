using BlueberryAirPlay4K;
using Microsoft.Win32;

string testKey = @"Software\BlueberryAirPlay4K\Tests\" + Guid.NewGuid().ToString("N");
var settings = new StartupSettings(testKey);
int assertions = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    assertions++;
    Console.WriteLine("PASS: " + description);
}

try
{
    Check(!settings.Read().Exists, "Default is off");
    settings.Disable();
    using (var absent = Registry.CurrentUser.OpenSubKey(testKey))
        Check(absent is null, "Reading/disabling absent settings creates no key");

    const string oldEngine = @"E:\Old package\uxplay-windows.exe";
    settings.Enable(oldEngine);
    Check(settings.Read().Exists, "Old or missing package registration is still visible");
    Check(settings.Read().Command == $"\"{oldEngine}\"", "Paths with spaces are quoted exactly");
    Check(settings.Read().Command == $"\"{oldEngine}\"", "Loading never changes registration");

    using (var key = Registry.CurrentUser.OpenSubKey(testKey, true))
        key!.SetValue("UnrelatedApplication", "preserve-me");
    settings.Disable();
    settings.Disable();
    Check(!settings.Read().Exists, "Disable removes legacy entries and is idempotent");
    using (var key = Registry.CurrentUser.OpenSubKey(testKey))
        Check((string?)key!.GetValue("UnrelatedApplication") == "preserve-me", "Other startup entries are preserved");

    foreach (object badValue in new object[] { "", 123 })
    {
        using (var key = Registry.CurrentUser.OpenSubKey(testKey, true))
            key!.SetValue(StartupSettings.ValueName, badValue);
        Check(settings.Read().Exists, "Malformed registration is not hidden as disabled");
        settings.Disable();
        Check(!settings.Read().Exists, "Malformed registration can be removed");
    }

    foreach (string invalid in new[] { "", "relative.exe", "E:\\bad\"path.exe", "E:\\bad\npath.exe", "E:\\" + new string('x', 260) })
    {
        bool rejected = false;
        try { settings.Enable(invalid); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && !settings.Read().Exists, "Invalid command does not create startup registration");
    }
    Console.WriteLine($"All {assertions} startup regression assertions passed.");
}
finally
{
    // Only the unique test key is removed, never the real Run key.
    Registry.CurrentUser.DeleteSubKeyTree(testKey, false);
}
