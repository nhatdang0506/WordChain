
using System.Globalization;
using System.Text;

namespace WordChain.Common;

public static class WordRules
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Trim().ToLowerInvariant();
        var d = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var ch in d)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string? LastWordNormalized(string phrase)
    {
        var s = Normalize(phrase);
        if (string.IsNullOrWhiteSpace(s)) return null;
        var parts = s.Split(new char[]{' ', '\t', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            var token = FilterLettersDigitDash(parts[i]);
            if (!string.IsNullOrWhiteSpace(token)) return token;
        }
        return null;
    }

    private static string FilterLettersDigitDash(string token)
    {
        var sb = new StringBuilder(token.Length);
        foreach (var ch in token)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
        }
        return sb.ToString();
    }

    public static bool StartsWithWordNormalized(string current, string required)
    {
        var s = Normalize(current);
        if (string.IsNullOrEmpty(required)) return false;
        if (s == required) return true;
        if (s.StartsWith(required + " ")) return true;
        if (s.StartsWith(required + "-")) return true;
        if (s.StartsWith(required + "_")) return true;
        return false;
    }

    public static bool IsValidChainByWord(string? previous, string current, out string? nextRequired)
    {
        nextRequired = LastWordNormalized(current);
        if (string.IsNullOrWhiteSpace(current)) return false;
        if (string.IsNullOrWhiteSpace(previous)) return true;
        var required = LastWordNormalized(previous!);
        if (required is null) return false;
        return StartsWithWordNormalized(current, required);
    }
}
