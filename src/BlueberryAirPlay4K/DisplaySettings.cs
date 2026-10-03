using System.Globalization;
using System.IO;
using System.Text;

namespace BlueberryAirPlay4K;

public enum PictureMode { OriginalPixels, AspectWindow, FitWindow }
public enum PictureWindowState { Normal, MaximizeWithTaskbar, Fullscreen }

/// <summary>Package-local display commands, independent of AirPlay encoding and other copies.</summary>
public sealed class DisplaySettings(string runtime)
{
    public bool Supported
    {
        get
        {
            try { return File.ReadAllText(Path.Combine(runtime, "display-control.version")).Trim() == "1"; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
    public (PictureMode Mode, PictureWindowState State) Read()
    {
        try
        {
            string[] fields = File.ReadAllText(Path.Combine(runtime, "display-mode.txt")).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 3 && int.TryParse(fields[0], out int mode) && int.TryParse(fields[1], out int state) &&
                mode is >= 0 and <= 2 && state is >= 0 and <= 2 && ulong.TryParse(fields[2].Trim(), out ulong revision) && revision > 0)
                return ((PictureMode)mode, (PictureWindowState)state);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return (PictureMode.FitWindow, PictureWindowState.Normal);
    }

    public void Apply(PictureMode mode, PictureWindowState state)
    {
        if (!Enum.IsDefined(mode) || !Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (!Supported) throw new InvalidOperationException("当前接收核心不支持显示控制，请使用包含新版接收核心的完整程序包。");
        string destination = Path.Combine(runtime, "display-mode.txt");
        string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, $"{(int)mode} {(int)state} {DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}\n", new UTF8Encoding(false));
            File.Move(temp, destination, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
