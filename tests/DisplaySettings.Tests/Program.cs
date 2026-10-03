using BlueberryAirPlay4K;

string temporary = Path.Combine(Path.GetTempPath(), "Blueberry-Display-Test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporary);
int assertions = 0;
void Check(bool condition) { assertions++; if (!condition) throw new Exception("Display assertion " + assertions); }
try
{
    var settings = new DisplaySettings(temporary);
    Check(!settings.Supported);
    Check(settings.Read() == (PictureMode.FitWindow, PictureWindowState.Normal));
    try { settings.Apply(PictureMode.FitWindow, PictureWindowState.Normal); throw new Exception("Old core accepted"); }
    catch (InvalidOperationException) { assertions++; }
    File.WriteAllText(Path.Combine(temporary, "display-control.version"), "1\n");
    foreach (PictureMode mode in Enum.GetValues<PictureMode>())
    foreach (PictureWindowState state in Enum.GetValues<PictureWindowState>())
    {
        settings.Apply(mode, state);
        Check(settings.Read() == (mode, state));
        Check(Directory.GetFiles(temporary, "*.tmp").Length == 0);
    }
    File.WriteAllText(Path.Combine(temporary, "display-mode.txt"), "broken");
    Check(settings.Read() == (PictureMode.FitWindow, PictureWindowState.Normal));
    try { settings.Apply((PictureMode)9, PictureWindowState.Normal); throw new Exception("Invalid enum accepted"); }
    catch (ArgumentOutOfRangeException) { assertions++; }
    Console.WriteLine($"PASS: {assertions} display configuration assertions.");
}
finally { Directory.Delete(temporary, true); }
