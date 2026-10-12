using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BlueberryAirPlay4K;

internal static class ReceiverNames
{
    // DNS-SD labels allow 63 UTF-8 bytes. RAOP adds twelve hex digits and '@'.
    internal const int MaxReceiverBytes = 50;
    internal const int MaxCustomCharacters = 32;
    internal static readonly int MaxBaseBytes = MaxReceiverBytes - 1 -
        ReceiverProfiles.All.Values.Max(p => Encoding.UTF8.GetByteCount(p.NameSuffix));

    internal static bool TryNormalizeCustom(string? value, out string normalized, out string error)
    {
        normalized = "";
        error = "";
        try { normalized = (value ?? "").Trim(' ').Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { error = "名称包含无效字符。"; return false; }
        if (normalized.Length == 0) return true;
        var characters = normalized.EnumerateRunes().ToArray();
        if (!Rune.IsLetterOrDigit(characters[0]) || characters.Any(r => !Allowed(r)))
        {
            error = "请以文字或数字开头，仅使用文字、数字、空格和 - _ . ( )（）。";
            return false;
        }
        if (characters.Length > MaxCustomCharacters || Encoding.UTF8.GetByteCount(normalized) > MaxBaseBytes)
        {
            error = $"名称过长：最多 {MaxCustomCharacters} 个字符，中文通常不超过 {MaxBaseBytes / 3} 字；请缩短名称。";
            return false;
        }
        return true;
    }

    private static bool Allowed(Rune r) => Rune.IsLetterOrDigit(r) ||
        Rune.GetUnicodeCategory(r) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark ||
        r.Value is ' ' or '-' or '_' or '.' or '(' or ')' or '（' or '）';

    internal static string DefaultBase(string machineName)
    {
        var result = new StringBuilder();
        int bytes = 0;
        foreach (Rune r in machineName.Normalize(NormalizationForm.FormC).EnumerateRunes())
        {
            if (!Allowed(r) || result.Length == 0 && !Rune.IsLetterOrDigit(r)) continue;
            if (bytes + r.Utf8SequenceLength > MaxBaseBytes) break;
            result.Append(r.ToString());
            bytes += r.Utf8SequenceLength;
        }
        return result.Length == 0 ? "Windows" : result.ToString().TrimEnd(' ');
    }

    internal static string BuildName(ReceiverProfile profile, string? customName, string machineName)
    {
        if (!TryNormalizeCustom(customName, out string normalized, out string error))
            throw new ArgumentException(error, nameof(customName));
        return $"{(normalized.Length == 0 ? DefaultBase(machineName) : normalized)}-{profile.NameSuffix}";
    }

    internal static string BuildArguments(ReceiverProfile profile, string? customName, string machineName) =>
        $"-n \"{BuildName(profile, customName, machineName)}\" {profile.Arguments}";

    // Accept both old unquoted fixed names and new quoted names. The remaining
    // options must match a known profile, so naming never changes codec selection.
    internal static (ReceiverProfile? Profile, string Name) ParseConfiguration(string content)
    {
        Match match = Regex.Match(content.Trim(),
            "\\A-n\\s+(?:\"(?<name>[^\"\\r\\n]+)\"|(?<name>[^\\s\"]+))\\s+(?<options>[^\\r\\n]+)\\z");
        if (!match.Success) return (null, "");
        string options = match.Groups["options"].Value;
        return (ReceiverProfiles.All.Values.FirstOrDefault(p => p.Arguments == options), match.Groups["name"].Value);
    }
}

internal sealed class ReceiverNameSettings(string path)
{
    internal string Read()
    {
        try
        {
            return ReceiverNames.TryNormalizeCustom(File.ReadAllText(path), out string normalized, out _) ? normalized : "";
        }
        catch (IOException) { return ""; }
        catch (UnauthorizedAccessException) { return ""; }
    }

    internal void Save(string value)
    {
        if (!ReceiverNames.TryNormalizeCustom(value, out string normalized, out string error))
            throw new ArgumentException(error, nameof(value));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, normalized, new UTF8Encoding(false));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
