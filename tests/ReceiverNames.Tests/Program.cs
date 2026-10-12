using System.Text;
using BlueberryAirPlay4K;

int assertions = 0;
var parserFixtures = new List<object>();
void Check(bool condition, string description)
{
    assertions++;
    if (!condition) throw new Exception(description);
}
void Invalid(string value)
{
    Check(!ReceiverNames.TryNormalizeCustom(value, out _, out _), $"Invalid name accepted: {value}");
    try { ReceiverNames.BuildArguments(ReceiverProfiles.All["4k60-sync"], value, "PC-A"); }
    catch (ArgumentException) { assertions++; return; }
    throw new Exception("Invalid name generated arguments");
}

foreach (string value in new[] { "", "   ", "客厅电脑", "Meeting Room 2", "PC_01.2 (A)", "电脑（客厅）", "Café", "Cafe\u0301", "电脑𠮷" })
{
    Check(ReceiverNames.TryNormalizeCustom(value, out string normalized, out string error) && error.Length == 0, "Valid name rejected");
    foreach (var profile in ReceiverProfiles.All.Values)
    {
        string name = ReceiverNames.BuildName(profile, value, "PC-A");
        Check(Encoding.UTF8.GetByteCount(name) + 13 <= 63, "RAOP service name exceeds DNS label limit");
        Check(name.EndsWith("-" + profile.NameSuffix, StringComparison.Ordinal), "Mode suffix missing");
        var parsed = ReceiverNames.ParseConfiguration(ReceiverNames.BuildArguments(profile, value, "PC-A"));
        Check(parsed.Profile == profile && parsed.Name == name, "New quoted configuration does not round-trip");
        parserFixtures.Add(new { Arguments = ReceiverNames.BuildArguments(profile, value, "PC-A"), Name = name, Options = profile.Arguments });
        var legacy = ReceiverNames.ParseConfiguration($"-n 蓝莓投屏-{profile.NameSuffix} {profile.Arguments}");
        Check(legacy.Profile == profile, "Old profile not recognized after migration");
    }
}
foreach (string value in new[] { "-n Evil", "a\" -fps 999", "a\nb", "a\r", "a\t", "a\0b", "a%USERNAME%", "a\\b", "a/b", "a;quit", "a💻", "a\u200B", "a\u202E", "a\uD800", new string('a',33), new string('电',12) })
    Invalid(value);

Check(ReceiverNames.TryNormalizeCustom(new string('a',32), out _, out _), "ASCII length boundary rejected");
Check(ReceiverNames.TryNormalizeCustom(new string('电',11), out _, out _), "Chinese byte boundary rejected");
var standard = ReceiverProfiles.All["4k60-sync"];
Check(ReceiverNames.BuildName(standard, "", "PC-A") != ReceiverNames.BuildName(standard, "", "PC-B"), "Different computers share default name");
Check(ReceiverNames.BuildName(standard, "", "PC-A") != ReceiverNames.BuildName(ReceiverProfiles.All["4k60-low"], "", "PC-A"), "Different modes share name");
Check(ReceiverNames.BuildName(standard, "", "PC-A") == "PC-A-4K60", "Default not based on current computer");
Check(ReceiverNames.BuildName(standard, "Cafe\u0301", "PC-A") == "Café-4K60", "Name not normalized to NFC");
foreach (string machine in new[] { "电脑A", new string('电',100), "PC\"%name", "--???", "" })
    foreach (var profile in ReceiverProfiles.All.Values)
        Check(Encoding.UTF8.GetByteCount(ReceiverNames.BuildName(profile, "", machine)) <= 50, "Default not safely bounded");
Check(ReceiverNames.ParseConfiguration("-n \"Room 2\" -h265 -s broken").Profile is null, "Unknown configuration treated as known profile");

string directory = Path.Combine(AppContext.BaseDirectory, "Blueberry-Names-Test-" + Guid.NewGuid().ToString("N"));
try
{
    string path = Path.Combine(directory, "receiver-name.txt");
    var settings = new ReceiverNameSettings(path);
    Check(settings.Read() == "", "Missing settings not default");
    settings.Save("  客厅电脑  ");
    Check(new ReceiverNameSettings(path).Read() == "客厅电脑", "Name not persisted across launches");
    try { settings.Save("-bad"); throw new Exception("Saved invalid name"); }
    catch (ArgumentException) { assertions++; }
    Check(settings.Read() == "客厅电脑", "Invalid input overwrote valid settings");
    settings.Save("");
    Check(settings.Read() == "", "Clearing name does not restore default");
    Check(ReceiverNames.BuildName(standard, settings.Read(), "PC-B") == "PC-B-4K60", "Default stuck to old machine");
    File.WriteAllText(path, "injected\n-n evil");
    Check(settings.Read() == "", "Corrupted file not rejected");
    Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "Temporary settings file left behind");
}
finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
Console.WriteLine($"PASS: {assertions} receiver naming, migration, injection and persistence assertions.");
if (args.Length > 0)
    File.WriteAllText(args[0], System.Text.Json.JsonSerializer.Serialize(parserFixtures), new UTF8Encoding(false));
