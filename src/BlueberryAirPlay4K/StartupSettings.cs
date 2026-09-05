using Microsoft.Win32;
using System.IO;

namespace BlueberryAirPlay4K;

internal sealed record StartupRegistration(bool Exists, string? Command);

// This class owns one named value, never the entire Windows Run key.
internal sealed class StartupSettings
{
    internal const string ValueName = "BlueberryAirPlay4K";
    internal const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _keyPath;

    internal StartupSettings(string keyPath = RunKeyPath) => _keyPath = keyPath;

    internal StartupRegistration Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_keyPath, false);
        bool exists = key?.GetValueNames().Contains(ValueName, StringComparer.OrdinalIgnoreCase) == true;
        return new(exists, key?.GetValue(ValueName, null,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string);
    }

    internal static string BuildCommand(string enginePath)
    {
        if (string.IsNullOrWhiteSpace(enginePath) || !Path.IsPathFullyQualified(enginePath) ||
            enginePath.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0)
            throw new ArgumentException("开机启动路径无效。", nameof(enginePath));

        string command = $"\"{enginePath}\"";
        if (command.Length > 260)
            throw new ArgumentException("目录过长，无法登记 Windows 开机启动；手动启动不受影响。", nameof(enginePath));
        return command;
    }

    // Call only after explicit user confirmation, never on load or normal start.
    internal void Enable(string enginePath)
    {
        string command = BuildCommand(enginePath);
        using var key = Registry.CurrentUser.CreateSubKey(_keyPath, true);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    internal void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_keyPath, true);
        // A moved/deleted package must not prevent removing its old registration.
        key?.DeleteValue(ValueName, false);
    }
}
